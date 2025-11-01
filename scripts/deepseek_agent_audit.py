#!/usr/bin/env python3
"""
🔍 MOD SQUAD: DeepSeek Agent Model Audit
Maximum thoroughness investigation of why DeepSeek model isn't appearing in agents
"""

import json
import os
import sys
import subprocess
import time
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Any, Optional

class DeepSeekAgentAuditor:
    def __init__(self):
        self.root_dir = Path(__file__).parent.parent
        self.results = {
            "timestamp": datetime.now().isoformat(),
            "audit_name": "DeepSeek Agent Model Investigation",
            "phases": {},
            "issues": [],
            "recommendations": []
        }
        self.issue_count = 0

    def log(self, message: str, level: str = "INFO"):
        """Log messages with timestamp"""
        timestamp = datetime.now().strftime("%H:%M:%S")
        prefix = "[OK]" if level == "SUCCESS" else "[ERROR]" if level == "ERROR" else "[INFO]" if level == "INFO" else "[WARN]"
        print(f"[{timestamp}] {prefix} {message}", flush=True)

    def add_issue(self, phase: str, severity: str, description: str, details: Any = None):
        """Track issues found"""
        self.issue_count += 1
        self.results.setdefault("issues", []).append({
            "phase": phase,
            "severity": severity,
            "description": description,
            "details": details
        })
        self.log(f"{severity}: {description}", "ERROR" if severity == "CRITICAL" else "WARNING")

    def add_recommendation(self, phase: str, recommendation: str, action: str = None):
        """Track recommendations"""
        self.results["recommendations"].append({
            "phase": phase,
            "recommendation": recommendation,
            "action": action
        })
        self.log(f"💡 RECOMMENDATION: {recommendation}", "INFO")

    # PHASE 1: Configuration Verification
    def phase1_config_check(self):
        """Verify all configuration files for DeepSeek model"""
        self.log("=" * 80)
        self.log("PHASE 1: Configuration File Deep Scan")
        self.log("=" * 80)

        phase_results = {
            "config_files_checked": 0,
            "deepseek_references": [],
            "litellm_config": None,
            "appsettings_config": None
        }

        # Check appsettings.json
        appsettings_path = self.root_dir / "WolfPackAI.AppHost" / "appsettings.json"
        if appsettings_path.exists():
            self.log(f"Checking {appsettings_path}")
            with open(appsettings_path, 'r', encoding='utf-8') as f:
                appsettings = json.load(f)
                phase_results["appsettings_config"] = appsettings.get("LiteLLM", {})

                # Extract model configuration
                model_list = appsettings.get("LiteLLM", {}).get("ModelList", [])
                for model in model_list:
                    model_name = model.get("ModelName", "")
                    if "deepseek" in model_name.lower():
                        phase_results["deepseek_references"].append({
                            "file": "appsettings.json",
                            "model_name": model_name,
                            "full_config": model
                        })
                        self.log(f"Found DeepSeek in appsettings.json: {model_name}", "SUCCESS")

                phase_results["config_files_checked"] += 1

        # Check generated litellm-config.yaml
        litellm_yaml = self.root_dir / "WolfPackAI.AppHost" / "litellm-config.yaml"
        if litellm_yaml.exists():
            self.log(f"Checking {litellm_yaml}")
            with open(litellm_yaml, 'r', encoding='utf-8') as f:
                content = f.read()
                if "deepseek" in content.lower():
                    phase_results["deepseek_references"].append({
                        "file": "litellm-config.yaml",
                        "content_snippet": content[:500]
                    })
                    self.log(f"Found DeepSeek in litellm-config.yaml", "SUCCESS")
                phase_results["litellm_config"] = content
            phase_results["config_files_checked"] += 1

        if not phase_results["deepseek_references"]:
            self.add_issue(
                "Phase 1",
                "CRITICAL",
                "DeepSeek model not found in any configuration files",
                phase_results
            )
        else:
            self.log(f"Found {len(phase_results['deepseek_references'])} DeepSeek references", "SUCCESS")

        self.results["phases"]["phase1_config"] = phase_results
        return phase_results

    # PHASE 2: Ollama Container Check
    def phase2_ollama_check(self):
        """Check if Ollama container is running and has the model"""
        self.log("=" * 80)
        self.log("PHASE 2: Ollama Container and Model Verification")
        self.log("=" * 80)

        phase_results = {
            "container_running": False,
            "container_name": None,
            "models_list": [],
            "deepseek_found": False
        }

        try:
            # Find Ollama container
            result = subprocess.run(
                ["docker", "ps", "--filter", "ancestor=ollama/ollama", "--format", "{{.Names}}"],
                capture_output=True,
                text=True,
                timeout=10
            )

            if result.returncode == 0 and result.stdout.strip():
                container_name = result.stdout.strip().split('\n')[0]
                phase_results["container_running"] = True
                phase_results["container_name"] = container_name
                self.log(f"Found Ollama container: {container_name}", "SUCCESS")

                # List models in Ollama
                self.log("Listing models in Ollama...")
                model_result = subprocess.run(
                    ["docker", "exec", container_name, "ollama", "list"],
                    capture_output=True,
                    text=True,
                    timeout=30
                )

                if model_result.returncode == 0:
                    models = model_result.stdout
                    phase_results["models_list"] = models.split('\n')
                    self.log(f"Ollama models:\n{models}")

                    if "deepseek" in models.lower():
                        phase_results["deepseek_found"] = True
                        self.log("DeepSeek model found in Ollama!", "SUCCESS")
                    else:
                        self.add_issue(
                            "Phase 2",
                            "CRITICAL",
                            "DeepSeek model NOT found in Ollama container",
                            {"models": phase_results["models_list"]}
                        )
                        self.add_recommendation(
                            "Phase 2",
                            "Pull DeepSeek model in Ollama",
                            f"docker exec {container_name} ollama pull deepseek-coder-v2:16b"
                        )
                else:
                    self.add_issue(
                        "Phase 2",
                        "ERROR",
                        "Failed to list Ollama models",
                        model_result.stderr
                    )
            else:
                self.add_issue(
                    "Phase 2",
                    "CRITICAL",
                    "Ollama container is not running",
                    None
                )
                self.add_recommendation(
                    "Phase 2",
                    "Start the application to launch Ollama",
                    "dotnet run --project WolfPackAI.AppHost"
                )

        except subprocess.TimeoutExpired:
            self.add_issue("Phase 2", "ERROR", "Docker command timeout", None)
        except FileNotFoundError:
            self.add_issue("Phase 2", "ERROR", "Docker not found in PATH", None)
        except Exception as e:
            self.add_issue("Phase 2", "ERROR", f"Unexpected error: {str(e)}", None)

        self.results["phases"]["phase2_ollama"] = phase_results
        return phase_results

    # PHASE 3: LiteLLM API Check
    def phase3_litellm_api_check(self):
        """Check LiteLLM API for available models"""
        self.log("=" * 80)
        self.log("PHASE 3: LiteLLM API Model List Verification")
        self.log("=" * 80)

        phase_results = {
            "api_accessible": False,
            "models_endpoint": None,
            "available_models": [],
            "deepseek_in_api": False
        }

        try:
            import requests

            # Try different possible endpoints
            endpoints = [
                "http://localhost:4000/v1/models",
                "http://localhost:4000/models",
                "http://localhost/litellm/v1/models",
                "http://localhost/litellm/models"
            ]

            for endpoint in endpoints:
                try:
                    self.log(f"Trying endpoint: {endpoint}")
                    response = requests.get(endpoint, timeout=5, headers={"Authorization": "Bearer sk-1234"})

                    if response.status_code == 200:
                        phase_results["api_accessible"] = True
                        phase_results["models_endpoint"] = endpoint
                        models_data = response.json()

                        if "data" in models_data:
                            phase_results["available_models"] = [m.get("id", "") for m in models_data["data"]]
                        elif "models" in models_data:
                            phase_results["available_models"] = models_data["models"]
                        else:
                            phase_results["available_models"] = list(models_data.keys())

                        self.log(f"Found {len(phase_results['available_models'])} models via API", "SUCCESS")
                        self.log(f"Models: {', '.join(phase_results['available_models'][:10])}")

                        # Check for DeepSeek
                        for model in phase_results["available_models"]:
                            if "deepseek" in model.lower():
                                phase_results["deepseek_in_api"] = True
                                self.log(f"DeepSeek found in API: {model}", "SUCCESS")

                        if not phase_results["deepseek_in_api"]:
                            self.add_issue(
                                "Phase 3",
                                "CRITICAL",
                                "DeepSeek not exposed by LiteLLM API",
                                {"available_models": phase_results["available_models"]}
                            )

                        break

                except requests.exceptions.RequestException:
                    continue

            if not phase_results["api_accessible"]:
                self.add_issue(
                    "Phase 3",
                    "ERROR",
                    "Cannot access LiteLLM API on any known endpoint",
                    {"tried_endpoints": endpoints}
                )

        except ImportError:
            self.log("requests library not available, skipping API check", "WARNING")
            phase_results["skip_reason"] = "requests library not installed"

        self.results["phases"]["phase3_litellm_api"] = phase_results
        return phase_results

    # PHASE 4: OpenWebUI Configuration Check
    def phase4_openwebui_check(self):
        """Check OpenWebUI configuration and model visibility"""
        self.log("=" * 80)
        self.log("PHASE 4: OpenWebUI Model Visibility Check")
        self.log("=" * 80)

        phase_results = {
            "container_running": False,
            "env_vars": {},
            "api_base_url": None,
            "agents_accessible": False
        }

        try:
            # Find OpenWebUI container
            result = subprocess.run(
                ["docker", "ps", "--filter", "ancestor=ghcr.io/open-webui/open-webui", "--format", "{{.Names}}"],
                capture_output=True,
                text=True,
                timeout=10
            )

            if result.returncode == 0 and result.stdout.strip():
                container_name = result.stdout.strip().split('\n')[0]
                phase_results["container_running"] = True
                self.log(f"Found OpenWebUI container: {container_name}", "SUCCESS")

                # Check environment variables
                env_result = subprocess.run(
                    ["docker", "inspect", container_name, "--format", "{{json .Config.Env}}"],
                    capture_output=True,
                    text=True,
                    timeout=10
                )

                if env_result.returncode == 0:
                    env_vars = json.loads(env_result.stdout)
                    for env in env_vars:
                        if "=" in env:
                            key, value = env.split("=", 1)
                            phase_results["env_vars"][key] = value
                            if "API" in key.upper() or "URL" in key.upper():
                                self.log(f"  {key} = {value}")

                    # Check OPENAI_API_BASE_URL
                    api_base = phase_results["env_vars"].get("OPENAI_API_BASE_URL")
                    if api_base:
                        phase_results["api_base_url"] = api_base
                        self.log(f"OpenWebUI API Base: {api_base}", "SUCCESS")

                        if "litellm" not in api_base.lower():
                            self.add_issue(
                                "Phase 4",
                                "WARNING",
                                "OpenWebUI may not be pointing to LiteLLM",
                                {"api_base_url": api_base}
                            )
                    else:
                        self.add_issue(
                            "Phase 4",
                            "ERROR",
                            "OPENAI_API_BASE_URL not set in OpenWebUI",
                            None
                        )

                # Try to access OpenWebUI agents API
                try:
                    import requests

                    endpoints = [
                        "http://localhost:8080/api/models",
                        "http://localhost/chat/api/models"
                    ]

                    for endpoint in endpoints:
                        try:
                            self.log(f"Trying OpenWebUI endpoint: {endpoint}")
                            response = requests.get(endpoint, timeout=5)
                            if response.status_code == 200:
                                phase_results["agents_accessible"] = True
                                models = response.json()
                                self.log(f"OpenWebUI returned {len(models.get('data', []))} models")

                                # Check for DeepSeek in OpenWebUI
                                model_ids = [m.get("id", "") for m in models.get("data", [])]
                                deepseek_models = [m for m in model_ids if "deepseek" in m.lower()]

                                if deepseek_models:
                                    self.log(f"DeepSeek found in OpenWebUI: {deepseek_models}", "SUCCESS")
                                else:
                                    self.add_issue(
                                        "Phase 4",
                                        "CRITICAL",
                                        "DeepSeek NOT visible in OpenWebUI models",
                                        {"available_models": model_ids}
                                    )

                                break
                        except requests.exceptions.RequestException:
                            continue

                except ImportError:
                    self.log("requests library not available for OpenWebUI check", "WARNING")

            else:
                self.add_issue(
                    "Phase 4",
                    "ERROR",
                    "OpenWebUI container not running",
                    None
                )

        except Exception as e:
            self.add_issue("Phase 4", "ERROR", f"Unexpected error: {str(e)}", None)

        self.results["phases"]["phase4_openwebui"] = phase_results
        return phase_results

    # PHASE 5: Service Connectivity Test
    def phase5_connectivity_check(self):
        """Test connectivity between services"""
        self.log("=" * 80)
        self.log("PHASE 5: Service Connectivity Matrix")
        self.log("=" * 80)

        phase_results = {
            "tests": []
        }

        try:
            import requests

            # Test 1: OpenWebUI -> LiteLLM
            test = {
                "name": "OpenWebUI to LiteLLM",
                "success": False,
                "details": None
            }

            try:
                # Get OpenWebUI container
                result = subprocess.run(
                    ["docker", "ps", "--filter", "ancestor=ghcr.io/open-webui/open-webui", "--format", "{{.Names}}"],
                    capture_output=True,
                    text=True,
                    timeout=10
                )

                if result.returncode == 0 and result.stdout.strip():
                    container_name = result.stdout.strip().split('\n')[0]

                    # Try to curl LiteLLM from within OpenWebUI container
                    curl_result = subprocess.run(
                        ["docker", "exec", container_name, "curl", "-s", "http://litellm:4000/health"],
                        capture_output=True,
                        text=True,
                        timeout=10
                    )

                    if curl_result.returncode == 0:
                        test["success"] = True
                        test["details"] = "Successfully reached LiteLLM from OpenWebUI"
                        self.log("✅ OpenWebUI can reach LiteLLM", "SUCCESS")
                    else:
                        test["details"] = f"Failed to reach LiteLLM: {curl_result.stderr}"
                        self.add_issue(
                            "Phase 5",
                            "CRITICAL",
                            "OpenWebUI cannot reach LiteLLM service",
                            curl_result.stderr
                        )
            except Exception as e:
                test["details"] = str(e)

            phase_results["tests"].append(test)

            # Test 2: LiteLLM -> Ollama
            test = {
                "name": "LiteLLM to Ollama",
                "success": False,
                "details": None
            }

            try:
                result = subprocess.run(
                    ["docker", "ps", "--filter", "name=litellm", "--format", "{{.Names}}"],
                    capture_output=True,
                    text=True,
                    timeout=10
                )

                if result.returncode == 0 and result.stdout.strip():
                    container_name = result.stdout.strip().split('\n')[0]

                    curl_result = subprocess.run(
                        ["docker", "exec", container_name, "curl", "-s", "http://ollama:11434/api/tags"],
                        capture_output=True,
                        text=True,
                        timeout=10
                    )

                    if curl_result.returncode == 0:
                        test["success"] = True
                        test["details"] = "Successfully reached Ollama from LiteLLM"
                        self.log("✅ LiteLLM can reach Ollama", "SUCCESS")

                        # Parse response for models
                        try:
                            models_data = json.loads(curl_result.stdout)
                            model_names = [m.get("name", "") for m in models_data.get("models", [])]
                            self.log(f"Ollama models accessible to LiteLLM: {model_names}")

                            if not any("deepseek" in m.lower() for m in model_names):
                                self.add_issue(
                                    "Phase 5",
                                    "CRITICAL",
                                    "DeepSeek model not in Ollama's model list",
                                    {"available_models": model_names}
                                )
                        except json.JSONDecodeError:
                            pass
                    else:
                        test["details"] = f"Failed to reach Ollama: {curl_result.stderr}"
                        self.add_issue(
                            "Phase 5",
                            "CRITICAL",
                            "LiteLLM cannot reach Ollama service",
                            curl_result.stderr
                        )
            except Exception as e:
                test["details"] = str(e)

            phase_results["tests"].append(test)

        except ImportError:
            self.log("Skipping connectivity tests (requests not available)", "WARNING")

        self.results["phases"]["phase5_connectivity"] = phase_results
        return phase_results

    # PHASE 6: Generate Action Plan
    def phase6_action_plan(self):
        """Generate prioritized action plan"""
        self.log("=" * 80)
        self.log("PHASE 6: Root Cause Analysis & Action Plan")
        self.log("=" * 80)

        action_plan = {
            "critical_issues": [],
            "root_cause": None,
            "immediate_actions": [],
            "verification_steps": []
        }

        # Analyze results
        critical_issues = [i for i in self.results.get("issues", []) if i["severity"] == "CRITICAL"]
        action_plan["critical_issues"] = critical_issues

        # Determine root cause
        if not self.results["phases"].get("phase2_ollama", {}).get("deepseek_found", False):
            action_plan["root_cause"] = "DeepSeek model not downloaded in Ollama container"
            action_plan["immediate_actions"].append({
                "priority": 1,
                "action": "Pull DeepSeek model in Ollama",
                "command": "docker exec <ollama-container> ollama pull deepseek-coder-v2:16b",
                "expected_time": "5-15 minutes (depending on network)"
            })
        elif not self.results["phases"].get("phase3_litellm_api", {}).get("deepseek_in_api", False):
            action_plan["root_cause"] = "LiteLLM not exposing DeepSeek model via API"
            action_plan["immediate_actions"].append({
                "priority": 1,
                "action": "Restart LiteLLM container to reload configuration",
                "command": "docker restart <litellm-container>",
                "expected_time": "30 seconds"
            })
        elif not self.results["phases"].get("phase4_openwebui", {}).get("agents_accessible", False):
            action_plan["root_cause"] = "OpenWebUI not connected to LiteLLM properly"
            action_plan["immediate_actions"].append({
                "priority": 1,
                "action": "Verify OpenWebUI OPENAI_API_BASE_URL configuration",
                "command": "Check environment variables in OpenWebUI container",
                "expected_time": "2 minutes"
            })

        # Add verification steps
        action_plan["verification_steps"] = [
            "1. Check Ollama has model: docker exec <ollama-container> ollama list",
            "2. Verify LiteLLM API: curl http://localhost:4000/v1/models",
            "3. Check OpenWebUI models: curl http://localhost:8080/api/models",
            "4. Restart all services if needed: dotnet run --project WolfPackAI.AppHost"
        ]

        self.results["phases"]["phase6_action_plan"] = action_plan

        # Print action plan
        self.log("ROOT CAUSE:", "INFO")
        if action_plan["root_cause"]:
            self.log(f"   {action_plan['root_cause']}", "ERROR")

        self.log("\nIMMEDIATE ACTIONS:", "INFO")
        for action in action_plan["immediate_actions"]:
            self.log(f"   [{action['priority']}] {action['action']}", "INFO")
            self.log(f"       Command: {action['command']}", "INFO")
            self.log(f"       ETA: {action['expected_time']}", "INFO")

        return action_plan

    def generate_report(self):
        """Generate final audit report"""
        self.log("=" * 80)
        self.log("AUDIT COMPLETE - Generating Report")
        self.log("=" * 80)

        # Save detailed JSON report
        report_path = self.root_dir / "reports" / "deepseek_agent_audit.json"
        report_path.parent.mkdir(exist_ok=True)

        with open(report_path, 'w', encoding='utf-8') as f:
            json.dump(self.results, f, indent=2)

        self.log(f"Detailed report saved: {report_path}", "SUCCESS")

        # Print summary
        self.log("\n" + "=" * 80)
        self.log("MOD SQUAD DEEPSEEK AGENT AUDIT SUMMARY")
        self.log("=" * 80)
        self.log(f"Total Issues Found: {self.issue_count}")
        self.log(f"Critical Issues: {len([i for i in self.results.get('issues', []) if i['severity'] == 'CRITICAL'])}")
        self.log(f"Recommendations: {len(self.results.get('recommendations', []))}")

        return report_path

    def run(self):
        """Execute all audit phases"""
        try:
            self.log("Starting MOD SQUAD DeepSeek Agent Investigation")
            self.log("Maximum Thoroughness Mode Activated")

            self.phase1_config_check()
            time.sleep(1)

            self.phase2_ollama_check()
            time.sleep(1)

            self.phase3_litellm_api_check()
            time.sleep(1)

            self.phase4_openwebui_check()
            time.sleep(1)

            self.phase5_connectivity_check()
            time.sleep(1)

            self.phase6_action_plan()

            report_path = self.generate_report()

            self.log(f"\nAudit completed successfully!", "SUCCESS")
            return 0 if self.issue_count == 0 else 1

        except Exception as e:
            self.log(f"Audit failed with error: {str(e)}", "ERROR")
            import traceback
            traceback.print_exc()
            return 2

if __name__ == "__main__":
    auditor = DeepSeekAgentAuditor()
    sys.exit(auditor.run())
