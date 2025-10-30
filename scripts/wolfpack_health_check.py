#!/usr/bin/env python3
"""
WolfPackAI Health Check Script
Validates all services are running and responsive.
Part of MOD SQUAD validation suite.
"""

import json
import sys
import time
import argparse
from typing import Dict, List, Tuple
from datetime import datetime, timezone
import subprocess

try:
    import requests
except ImportError:
    print("ERROR: requests library not installed. Run: pip install requests")
    sys.exit(1)


class WolfPackHealthChecker:
    """Health checker for WolfPackAI services."""
    
    def __init__(self, config_path: str = "mod_squad.config.json", wait: bool = False, timeout: int = 120):
        self.config_path = config_path
        self.wait_mode = wait
        self.timeout = timeout
        self.config = self._load_config()
        self.results = {
            "status": "unknown",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "services": {},
            "timings": {},
            "p95_ms": 0,
            "p99_ms": 0,
            "errors": [],
            "details": {}
        }
    
    def _load_config(self) -> dict:
        """Load MOD SQUAD configuration."""
        try:
            with open(self.config_path, 'r') as f:
                return json.load(f)
        except FileNotFoundError:
            print(f"ERROR: Config file not found: {self.config_path}")
            sys.exit(1)
        except json.JSONDecodeError as e:
            print(f"ERROR: Invalid JSON in config: {e}")
            sys.exit(1)
    
    def check_postgres(self, service_config: dict) -> Tuple[bool, float, str]:
        """Check PostgreSQL using pg_isready."""
        start = time.time()
        try:
            cmd = service_config.get("health_command", "pg_isready -h localhost -p 5432 -U postgres")
            result = subprocess.run(
                cmd.split(),
                capture_output=True,
                text=True,
                timeout=service_config.get("timeout_ms", 5000) / 1000
            )
            elapsed = (time.time() - start) * 1000
            
            if result.returncode == 0:
                return True, elapsed, "PostgreSQL is ready"
            else:
                return False, elapsed, f"PostgreSQL not ready: {result.stderr}"
        except subprocess.TimeoutExpired:
            elapsed = (time.time() - start) * 1000
            return False, elapsed, "PostgreSQL check timed out"
        except FileNotFoundError:
            elapsed = (time.time() - start) * 1000
            return False, elapsed, "pg_isready command not found (PostgreSQL client not installed)"
        except Exception as e:
            elapsed = (time.time() - start) * 1000
            return False, elapsed, f"PostgreSQL check error: {str(e)}"
    
    def check_http_service(self, service_name: str, service_config: dict) -> Tuple[bool, float, str]:
        """Check HTTP-based service health endpoint."""
        url = service_config["url"] + service_config.get("health_endpoint", "/health")
        timeout_s = service_config.get("timeout_ms", 5000) / 1000
        method = service_config.get("method", "GET")
        expected_status = service_config.get("expected_status", 200)
        auth_type = service_config.get("auth", "none")
        
        headers = {}
        if auth_type == "bearer":
            token_env = service_config.get("auth_token_env", "LITELLM_MASTER_KEY")
            # For now, skip auth checks if not in environment
            # In production, would read from env or config
            pass
        
        start = time.time()
        try:
            if method == "GET":
                response = requests.get(url, headers=headers, timeout=timeout_s)
            else:
                response = requests.request(method, url, headers=headers, timeout=timeout_s)
            
            elapsed = (time.time() - start) * 1000
            
            if response.status_code == expected_status:
                return True, elapsed, f"{service_name} is healthy (HTTP {response.status_code})"
            else:
                return False, elapsed, f"{service_name} returned HTTP {response.status_code}, expected {expected_status}"
        
        except requests.exceptions.ConnectionError:
            elapsed = (time.time() - start) * 1000
            return False, elapsed, f"{service_name} connection refused (service not started or wrong port)"
        except requests.exceptions.Timeout:
            elapsed = (time.time() - start) * 1000
            return False, elapsed, f"{service_name} health check timed out after {timeout_s}s"
        except Exception as e:
            elapsed = (time.time() - start) * 1000
            return False, elapsed, f"{service_name} check error: {str(e)}"
    
    def check_service(self, service_name: str, service_config: dict) -> Tuple[bool, float, str]:
        """Check a single service based on its type."""
        if not service_config.get("enabled", True):
            return True, 0, f"{service_name} is disabled (skipped)"
        
        health_type = service_config.get("health_type", "http")
        
        if health_type == "cli":
            return self.check_postgres(service_config)
        elif health_type == "http":
            return self.check_http_service(service_name, service_config)
        else:
            return False, 0, f"Unknown health_type: {health_type}"
    
    def check_all_services(self) -> bool:
        """Check all configured services."""
        services = self.config.get("services", {})
        all_healthy = True
        timings = []
        
        print("WolfPackAI Health Check")
        print("=" * 60)
        
        for service_name, service_config in services.items():
            if not service_config.get("enabled", True):
                print(f"⏭️  {service_name:15} SKIPPED (disabled)")
                continue
            
            is_critical = service_config.get("critical", True)
            healthy, elapsed_ms, message = self.check_service(service_name, service_config)
            
            self.results["services"][service_name] = {
                "healthy": healthy,
                "elapsed_ms": round(elapsed_ms, 2),
                "message": message,
                "critical": is_critical,
                "url": service_config.get("url", "N/A")
            }
            self.results["timings"][service_name] = round(elapsed_ms, 2)
            timings.append(elapsed_ms)
            
            status_icon = "[OK]" if healthy else ("[FAIL]" if is_critical else "[WARN]")
            print(f"{status_icon:8} {service_name:15} {elapsed_ms:7.1f}ms  {message}")
            
            if not healthy:
                self.results["errors"].append({
                    "service": service_name,
                    "message": message,
                    "critical": is_critical
                })
                if is_critical:
                    all_healthy = False
        
        # Calculate percentiles
        if timings:
            timings.sort()
            p95_idx = int(len(timings) * 0.95)
            p99_idx = int(len(timings) * 0.99)
            self.results["p95_ms"] = round(timings[p95_idx], 2) if p95_idx < len(timings) else round(timings[-1], 2)
            self.results["p99_ms"] = round(timings[p99_idx], 2) if p99_idx < len(timings) else round(timings[-1], 2)
        
        print("=" * 60)
        print(f"P95 Response Time: {self.results['p95_ms']:.1f}ms")
        print(f"P99 Response Time: {self.results['p99_ms']:.1f}ms")
        
        self.results["status"] = "pass" if all_healthy else "fail"
        return all_healthy
    
    def wait_for_services(self) -> bool:
        """Wait for services to become healthy (with timeout)."""
        print(f"Waiting for services to become healthy (timeout: {self.timeout}s)...")
        start_time = time.time()
        attempt = 0
        interval = self.config.get("thresholds", {}).get("health_check_interval_s", 5)
        
        while (time.time() - start_time) < self.timeout:
            attempt += 1
            print(f"\n[ATTEMPT {attempt}] ({int(time.time() - start_time)}s elapsed)")
            
            if self.check_all_services():
                elapsed = int(time.time() - start_time)
                print(f"\n[OK] All services healthy after {elapsed}s!")
                return True
            
            remaining = self.timeout - int(time.time() - start_time)
            if remaining > 0:
                print(f"Retrying in {interval}s... ({remaining}s remaining)")
                time.sleep(interval)
        
        print(f"\n[FAIL] Timeout reached after {self.timeout}s. Some services are not healthy.")
        return False
    
    def save_report(self, output_path: str):
        """Save health check report to JSON file."""
        try:
            with open(output_path, 'w') as f:
                json.dump(self.results, f, indent=2)
            print(f"\n[OK] Report saved: {output_path}")
        except Exception as e:
            print(f"\n[WARN] Failed to save report: {e}")
    
    def run(self, output_path: str = None, quick: bool = False) -> int:
        """Run health check and return exit code."""
        if quick:
            # Quick mode: check only critical services, no retries
            print("⚡ Quick health check mode")
        
        if self.wait_mode:
            success = self.wait_for_services()
        else:
            success = self.check_all_services()
        
        if output_path:
            self.save_report(output_path)
        
        if success:
            print("\n[PASS] Health check PASSED")
            return 0
        else:
            print("\n[FAIL] Health check FAILED")
            print("\nFailed services:")
            for error in self.results["errors"]:
                icon = "[CRITICAL]" if error["critical"] else "[WARN]"
                print(f"  {icon} {error['service']}: {error['message']}")
            return 1


def main():
    parser = argparse.ArgumentParser(description="WolfPackAI Health Check")
    parser.add_argument("--config", default="mod_squad.config.json", help="Path to MOD SQUAD config")
    parser.add_argument("--output", help="Output JSON report path")
    parser.add_argument("--wait", action="store_true", help="Wait for services to become healthy")
    parser.add_argument("--timeout", type=int, default=120, help="Wait timeout in seconds (default: 120)")
    parser.add_argument("--quick", action="store_true", help="Quick check (critical services only)")
    
    args = parser.parse_args()
    
    checker = WolfPackHealthChecker(
        config_path=args.config,
        wait=args.wait,
        timeout=args.timeout
    )
    
    exit_code = checker.run(output_path=args.output, quick=args.quick)
    sys.exit(exit_code)


if __name__ == "__main__":
    main()

