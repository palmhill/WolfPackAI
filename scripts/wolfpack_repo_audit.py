#!/usr/bin/env python3
"""
WolfPackAI Repository Audit Script
Scans for config issues, secrets, and deprecated patterns.
Part of MOD SQUAD validation suite.
"""

import argparse
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, List


class WolfPackRepoAuditor:
    """Repository auditor for WolfPackAI project."""

    def __init__(self, config_path: str = "mod_squad.config.json", root_dir: str = "."):
        self.config_path = config_path
        self.root_dir = Path(root_dir)
        self.config = self._load_config()
        self.results = {
            "status": "unknown",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "errors": [],
            "warnings": [],
            "info": [],
            "files_scanned": 0,
            "issues_found": 0,
            "details": {},
        }

    def _load_config(self) -> dict:
        """Load MOD SQUAD configuration."""
        try:
            with open(self.config_path, "r") as f:
                return json.load(f)
        except FileNotFoundError:
            print(f"ERROR: Config file not found: {self.config_path}")
            sys.exit(1)
        except json.JSONDecodeError as e:
            print(f"ERROR: Invalid JSON in config: {e}")
            sys.exit(1)

    def should_exclude(self, file_path: Path) -> bool:
        """Check if file should be excluded from scan."""
        audit_config = self.config.get("repo_audit", {})
        exclude_patterns = audit_config.get("exclude_paths", [])

        path_str = str(file_path)
        for pattern in exclude_patterns:
            # Simple pattern matching (could be enhanced with glob)
            pattern_clean = pattern.replace("**/", "").replace("/**", "")
            if pattern_clean in path_str:
                return True
        return False

    def scan_for_secrets(self, file_path: Path) -> List[Dict]:
        """Scan file for potential secrets."""
        issues = []
        audit_config = self.config.get("repo_audit", {})
        secret_patterns = audit_config.get("scan_patterns", {}).get("secrets", [])

        try:
            with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
                for line_num, line in enumerate(f, 1):
                    for pattern in secret_patterns:
                        if re.search(pattern, line, re.IGNORECASE):
                            issues.append(
                                {
                                    "file": str(file_path),
                                    "line": line_num,
                                    "pattern": pattern,
                                    "severity": "high",
                                    "message": "Potential secret detected",
                                }
                            )
        except Exception:
            pass  # Skip files that can't be read

        return issues

    def check_appsettings_files(self) -> List[Dict]:
        """Check appsettings.json files for common issues."""
        issues = []

        # Find all appsettings files
        appsettings_files = list(self.root_dir.rglob("appsettings*.json"))

        for file_path in appsettings_files:
            if self.should_exclude(file_path):
                continue

            try:
                with open(file_path, "r", encoding="utf-8") as f:
                    config_data = json.load(f)

                # Check for missing required sections (WolfPackAI specific)
                required_sections = [
                    "LiteLLM",
                    "Postgres",
                    "OpenWebUI",
                    "n8n",
                    "Dashboard",
                ]
                for section in required_sections:
                    if section not in config_data:
                        issues.append(
                            {
                                "file": str(file_path),
                                "severity": "medium",
                                "message": f"Missing required config section: {section}",
                            }
                        )

                # Check for default/placeholder values
                config_str = json.dumps(config_data)
                if "password" in config_str.lower() and (
                    "password123" in config_str.lower()
                    or "changeme" in config_str.lower()
                ):
                    issues.append(
                        {
                            "file": str(file_path),
                            "severity": "high",
                            "message": "Default password detected in config",
                        }
                    )

                # Check for hardcoded localhost in production configs
                if "Production" in str(file_path) or "production" in str(file_path):
                    if "localhost" in config_str:
                        issues.append(
                            {
                                "file": str(file_path),
                                "severity": "medium",
                                "message": "localhost reference in production config",
                            }
                        )

            except json.JSONDecodeError as e:
                issues.append(
                    {
                        "file": str(file_path),
                        "severity": "high",
                        "message": f"Invalid JSON: {str(e)}",
                    }
                )
            except Exception:
                pass  # Skip files with read errors

        return issues

    def check_docker_configs(self) -> List[Dict]:
        """Check Docker-related configuration files."""
        issues = []

        # Check for docker-compose files
        docker_files = list(self.root_dir.glob("docker-compose*.yml"))
        docker_files.extend(list(self.root_dir.glob("Dockerfile*")))

        for file_path in docker_files:
            if self.should_exclude(file_path):
                continue

            try:
                with open(file_path, "r", encoding="utf-8") as f:
                    content = f.read()

                # Check for hardcoded secrets
                if re.search(
                    r'password\s*[:=]\s*["\'](?!\\$)[^"\']{3,}["\']',
                    content,
                    re.IGNORECASE,
                ):
                    issues.append(
                        {
                            "file": str(file_path),
                            "severity": "high",
                            "message": "Potential hardcoded password in Docker config",
                        }
                    )

                # Check for :latest tag (best practice to pin versions)
                if ":latest" in content:
                    issues.append(
                        {
                            "file": str(file_path),
                            "severity": "low",
                            "message": "Docker image using :latest tag (consider pinning version)",
                        }
                    )

            except Exception:
                pass

        return issues

    def check_git_config(self) -> List[Dict]:
        """Check for sensitive files in git."""
        issues = []
 
        gitignore_path = self.root_dir / ".gitignore"
        if not gitignore_path.exists():
            issues.append(
                {
                    "file": ".gitignore",
                    "severity": "medium",
                    "message": ".gitignore file not found",
                }
            )
            return issues
 
        try:
            with open(gitignore_path, "r") as f:
                gitignore_content = f.read()
 
            # Check for important patterns
            important_patterns = [
                ("*.env", "Environment files"),
                ("appsettings*.json", "App settings"),
                ("*.key", "Key files"),
                ("*.pem", "Certificate files"),
            ]
 
            for pattern, description in important_patterns:
                if pattern not in gitignore_content:
                    issues.append(
                        {
                            "file": ".gitignore",
                            "severity": "low",
                            "message": f"Consider adding {description} pattern: {pattern}",
                        }
                    )
 
        except Exception:
            pass
 
        return issues
    
    def check_port_url_consistency(self) -> List[Dict]:
        """Check for inconsistent port/URL references across all config files."""
        issues = []
        port_references = {}  # {port_number: [(file, line, context)]}
        
        # Scan all JSON config files
        for config_file in self.root_dir.rglob("*.json"):
            if self.should_exclude(config_file):
                continue
            
            try:
                with open(config_file, "r", encoding="utf-8") as f:
                    content = f.read()
                    
                # Find all port references (localhost:XXXX, "port": XXXX, HttpPort: XXXX)
                import re
                
                # Pattern 1: localhost:PORT
                for match in re.finditer(r'localhost:(\d+)', content):
                    port = match.group(1)
                    if port not in port_references:
                        port_references[port] = []
                    port_references[port].append({
                        "file": str(config_file),
                        "context": f"localhost:{port}",
                        "type": "url"
                    })
                
                # Pattern 2: "port": PORT or "Port": PORT
                for match in re.finditer(r'"[Pp]ort"\s*:\s*(\d+)', content):
                    port = match.group(1)
                    if port not in port_references:
                        port_references[port] = []
                    port_references[port].append({
                        "file": str(config_file),
                        "context": f'"port": {port}',
                        "type": "config"
                    })
                
                # Pattern 3: HttpPort or HttpsPort
                for match in re.finditer(r'Http[s]?Port"\s*:\s*(\d+)', content):
                    port = match.group(1)
                    if port not in port_references:
                        port_references[port] = []
                    port_references[port].append({
                        "file": str(config_file),
                        "context": f'HttpPort: {port}',
                        "type": "http_config"
                    })
            
            except Exception:
                pass
        
        # Check for dangerous legacy ports that should be updated
        dangerous_ports = {
            "80": "Port 80 requires admin on Windows; use 8000+ instead",
            "443": "Port 443 requires admin on Windows; use 8443+ instead"
        }
        
        for port, message in dangerous_ports.items():
            if port in port_references:
                refs = port_references[port]
                # Only flag if used in multiple places or in critical configs
                if len(refs) > 0:
                    files = ", ".join(set(r["file"] for r in refs))
                    issues.append({
                        "files": files,
                        "severity": "high",
                        "message": f"Port {port} used in {len(refs)} location(s). {message}",
                        "details": refs
                    })
        
        # Check for inconsistent service port references
        # Group by service name and check if ports differ
        service_ports = {}  # {service_name: [ports_used]}
        
        for config_file in self.root_dir.rglob("mod_squad.config.json"):
            try:
                with open(config_file, "r") as f:
                    mod_config = json.load(f)
                
                # Check services section
                services = mod_config.get("services", {})
                for service_name, service_config in services.items():
                    url = service_config.get("url", "")
                    port_match = re.search(r':(\d+)', url)
                    if port_match:
                        port = port_match.group(1)
                        if service_name not in service_ports:
                            service_ports[service_name] = set()
                        service_ports[service_name].add(port)
                
                # Check browser_tests.scenarios section
                browser_tests = mod_config.get("browser_tests", {})
                scenarios = browser_tests.get("scenarios", [])
                for scenario in scenarios:
                    scenario_url = scenario.get("url", "")
                    scenario_name = scenario.get("name", "unknown")
                    
                    # Try to match scenario to service by name
                    for service_name in services.keys():
                        if service_name.lower() in scenario_name.lower():
                            port_match = re.search(r':(\d+)', scenario_url)
                            if port_match:
                                port = port_match.group(1)
                                if service_name not in service_ports:
                                    service_ports[service_name] = set()
                                service_ports[service_name].add(port)
            
            except Exception:
                pass
        
        # Flag services with multiple different ports
        for service_name, ports in service_ports.items():
            if len(ports) > 1:
                issues.append({
                    "file": "mod_squad.config.json",
                    "severity": "high",
                    "message": f"INCONSISTENT PORTS for {service_name}: found {', '.join(sorted(ports))}. All references to same service must use same port!",
                    "service": service_name,
                    "ports_found": list(ports)
                })
        
        return issues

    def scan_repository(self) -> bool:
        """Run all audit checks."""
        print("WolfPackAI Repository Audit")
        print("=" * 60)

        all_issues = []

        # 1. Scan for secrets in config files
        print("Scanning configuration files for secrets...")
        audit_config = self.config.get("repo_audit", {})
        config_patterns = audit_config.get("scan_patterns", {}).get("config_files", [])

        for pattern in config_patterns:
            for file_path in self.root_dir.rglob(pattern.replace("**/", "")):
                if self.should_exclude(file_path):
                    continue

                self.results["files_scanned"] += 1
                issues = self.scan_for_secrets(file_path)
                all_issues.extend(issues)

        # 2. Check appsettings files
        print("Checking appsettings.json files...")
        appsettings_issues = self.check_appsettings_files()
        all_issues.extend(appsettings_issues)

        # 3. Check Docker configs
        print("Checking Docker configurations...")
        docker_issues = self.check_docker_configs()
        all_issues.extend(docker_issues)

        # 4. Check git configuration
        print("Checking .gitignore...")
        git_issues = self.check_git_config()
        all_issues.extend(git_issues)
        
        # 5. Check port/URL consistency
        print("Checking port/URL consistency...")
        port_issues = self.check_port_url_consistency()
        all_issues.extend(port_issues)

        # Categorize issues by severity
        for issue in all_issues:
            severity = issue.get("severity", "info")
            if severity == "high":
                self.results["errors"].append(issue)
            elif severity == "medium":
                self.results["warnings"].append(issue)
            else:
                self.results["info"].append(issue)

        self.results["issues_found"] = len(all_issues)

        # Print summary
        print("=" * 60)
        print(f"Files Scanned: {self.results['files_scanned']}")
        print(f"Issues Found: {self.results['issues_found']}")
        print(f"  [ERROR] High severity:    {len(self.results['errors'])}")
        print(f"  [WARN]  Medium severity: {len(self.results['warnings'])}")
        print(f"  [INFO]  Low severity:    {len(self.results['info'])}")

        # Print details
        if self.results["errors"]:
            print("\n[ERRORS]:")
            for error in self.results["errors"][:10]:  # Limit to first 10
                file_ref = error.get('file') or error.get('files', 'unknown')
                print(f"  {file_ref}: {error['message']}")
 
        if self.results["warnings"]:
            print("\n[WARNINGS]:")
            for warning in self.results["warnings"][:5]:
                file_ref = warning.get('file') or warning.get('files', 'unknown')
                print(f"  {file_ref}: {warning['message']}")

        # Determine pass/fail
        has_critical_issues = len(self.results["errors"]) > 0
        self.results["status"] = "fail" if has_critical_issues else "pass"

        return not has_critical_issues

    def save_report(self, output_path: str):
        """Save audit report to JSON file."""
        try:
            with open(output_path, "w") as f:
                json.dump(self.results, f, indent=2)
            print(f"\n[OK] Report saved: {output_path}")
        except Exception as e:
            print(f"\n[WARN] Failed to save report: {e}")

    def run(self, output_path: str = None) -> int:
        """Run repository audit and return exit code."""
        success = self.scan_repository()

        if output_path:
            self.save_report(output_path)

        if success:
            print("\n[PASS] Repository audit PASSED")
            return 0
        else:
            print("\n[FAIL] Repository audit FAILED")
            print(
                f"\nFound {len(self.results['errors'])} critical issues that must be fixed."
            )
            return 1


def main():
    parser = argparse.ArgumentParser(description="WolfPackAI Repository Audit")
    parser.add_argument(
        "--config", default="mod_squad.config.json", help="Path to MOD SQUAD config"
    )
    parser.add_argument("--output", help="Output JSON report path")
    parser.add_argument("--root", default=".", help="Repository root directory")

    args = parser.parse_args()

    auditor = WolfPackRepoAuditor(config_path=args.config, root_dir=args.root)

    exit_code = auditor.run(output_path=args.output)
    sys.exit(exit_code)


if __name__ == "__main__":
    main()
