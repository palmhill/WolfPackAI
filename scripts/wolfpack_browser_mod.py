#!/usr/bin/env python3
"""
WolfPackAI Browser MOD Script
Validates Dashboard and OpenWebUI interfaces using Playwright.
Part of MOD SQUAD validation suite.
"""

import json
import sys
import argparse
from datetime import datetime
from typing import Dict, List

try:
    from playwright.sync_api import sync_playwright, TimeoutError as PlaywrightTimeout
except ImportError:
    print("ERROR: playwright library not installed.")
    print("Run: pip install playwright && python -m playwright install")
    sys.exit(1)


class WolfPackBrowserMod:
    """Browser-based validation for WolfPackAI services."""
    
    def __init__(self, config_path: str = "mod_squad.config.json", headless: bool = True):
        self.config_path = config_path
        self.headless = headless
        self.config = self._load_config()
        self.results = {
            "status": "unknown",
            "timestamp": datetime.utcnow().isoformat() + "Z",
            "scenarios": {},
            "errors": [],
            "timings": {},
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
    
    def test_scenario(self, browser, scenario: dict) -> bool:
        """Run a single browser test scenario."""
        scenario_name = scenario["name"]
        url = scenario["url"]
        checks = scenario.get("checks", [])
        
        print(f"\n🌐 Testing: {scenario_name}")
        print(f"   URL: {url}")
        
        try:
            page = browser.new_page()
            console_errors = []
            failed_requests = []
            
            # Capture console errors
            page.on("console", lambda msg: console_errors.append(msg.text) if msg.type == "error" else None)
            
            # Capture failed network requests
            page.on("response", lambda response: 
                failed_requests.append({
                    "url": response.url,
                    "status": response.status
                }) if response.status >= 400 else None
            )
            
            # Navigate to page
            response = page.goto(url, wait_until="networkidle", timeout=10000)
            load_time = page.evaluate("() => performance.timing.loadEventEnd - performance.timing.navigationStart")
            
            self.results["timings"][scenario_name] = load_time
            
            scenario_result = {
                "success": True,
                "load_time_ms": load_time,
                "status_code": response.status if response else 0,
                "console_errors": [],
                "failed_requests": [],
                "checks": {}
            }
            
            # Run checks
            for check in checks:
                if check == "no_console_errors":
                    if console_errors:
                        scenario_result["success"] = False
                        scenario_result["console_errors"] = console_errors[:5]  # Limit to first 5
                        scenario_result["checks"][check] = f"FAIL: {len(console_errors)} console errors"
                        print(f"   ❌ Console errors detected ({len(console_errors)})")
                    else:
                        scenario_result["checks"][check] = "PASS"
                        print(f"   ✅ No console errors")
                
                elif check == "page_loads":
                    if response and response.ok:
                        scenario_result["checks"][check] = "PASS"
                        print(f"   ✅ Page loaded successfully ({load_time}ms)")
                    else:
                        scenario_result["success"] = False
                        scenario_result["checks"][check] = f"FAIL: HTTP {response.status if response else 'N/A'}"
                        print(f"   ❌ Page load failed")
                
                elif check == "status_200":
                    if response and response.status == 200:
                        scenario_result["checks"][check] = "PASS"
                        print(f"   ✅ HTTP 200 OK")
                    else:
                        scenario_result["success"] = False
                        scenario_result["checks"][check] = f"FAIL: HTTP {response.status if response else 'N/A'}"
                        print(f"   ❌ Expected HTTP 200, got {response.status if response else 'N/A'}")
                
                elif check == "chat_interface_visible":
                    # Check for OpenWebUI-specific elements
                    try:
                        # Look for common chat UI elements
                        has_input = page.query_selector("textarea, input[type='text']") is not None
                        if has_input:
                            scenario_result["checks"][check] = "PASS"
                            print(f"   ✅ Chat interface detected")
                        else:
                            scenario_result["success"] = False
                            scenario_result["checks"][check] = "FAIL: No input field found"
                            print(f"   ❌ Chat interface not found")
                    except Exception as e:
                        scenario_result["success"] = False
                        scenario_result["checks"][check] = f"FAIL: {str(e)}"
                        print(f"   ❌ Chat interface check error: {e}")
            
            # Save screenshot on failure
            if not scenario_result["success"]:
                screenshot_path = f"reports/{scenario_name.replace(' ', '_')}_failure.png"
                page.screenshot(path=screenshot_path)
                scenario_result["screenshot"] = screenshot_path
                print(f"   📸 Screenshot saved: {screenshot_path}")
            
            page.close()
            self.results["scenarios"][scenario_name] = scenario_result
            
            return scenario_result["success"]
        
        except PlaywrightTimeout as e:
            print(f"   ❌ Timeout: {str(e)}")
            self.results["scenarios"][scenario_name] = {
                "success": False,
                "error": f"Timeout: {str(e)}",
                "checks": {}
            }
            self.results["errors"].append({
                "scenario": scenario_name,
                "error": f"Timeout: {str(e)}"
            })
            return False
        
        except Exception as e:
            print(f"   ❌ Error: {str(e)}")
            self.results["scenarios"][scenario_name] = {
                "success": False,
                "error": str(e),
                "checks": {}
            }
            self.results["errors"].append({
                "scenario": scenario_name,
                "error": str(e)
            })
            return False
    
    def run_tests(self) -> bool:
        """Run all browser test scenarios."""
        print("🌐 WolfPackAI Browser MOD")
        print("=" * 60)
        
        browser_config = self.config.get("browser_tests", {})
        if not browser_config.get("enabled", True):
            print("⏭️  Browser tests disabled in config")
            self.results["status"] = "skipped"
            return True
        
        scenarios = browser_config.get("scenarios", [])
        if not scenarios:
            print("⚠️  No scenarios configured")
            self.results["status"] = "skipped"
            return True
        
        all_passed = True
        
        with sync_playwright() as p:
            browser = p.chromium.launch(headless=self.headless)
            
            for scenario in scenarios:
                if not self.test_scenario(browser, scenario):
                    all_passed = False
            
            browser.close()
        
        print("=" * 60)
        self.results["status"] = "pass" if all_passed else "fail"
        
        return all_passed
    
    def save_report(self, output_path: str):
        """Save browser test report to JSON file."""
        try:
            with open(output_path, 'w') as f:
                json.dump(self.results, f, indent=2)
            print(f"\n📄 Report saved: {output_path}")
        except Exception as e:
            print(f"\n⚠️  Failed to save report: {e}")
    
    def run(self, output_path: str = None) -> int:
        """Run browser tests and return exit code."""
        success = self.run_tests()
        
        if output_path:
            self.save_report(output_path)
        
        if success:
            print("\n✅ Browser tests PASSED")
            return 0
        else:
            print("\n❌ Browser tests FAILED")
            print("\nFailed scenarios:")
            for error in self.results["errors"]:
                print(f"  🔴 {error['scenario']}: {error['error']}")
            return 1


def main():
    parser = argparse.ArgumentParser(description="WolfPackAI Browser MOD")
    parser.add_argument("--config", default="mod_squad.config.json", help="Path to MOD SQUAD config")
    parser.add_argument("--output", help="Output JSON report path")
    parser.add_argument("--check-render", action="store_true", help="Check page rendering only")
    parser.add_argument("--live-data", action="store_true", help="Validate live data flows")
    parser.add_argument("--headed", action="store_true", help="Run with visible browser (not headless)")
    
    args = parser.parse_args()
    
    mod = WolfPackBrowserMod(
        config_path=args.config,
        headless=not args.headed
    )
    
    exit_code = mod.run(output_path=args.output)
    sys.exit(exit_code)


if __name__ == "__main__":
    main()

