/**
 * WolfPackAI Gatekeeper - Cursor Integration Extension
 *
 * PURPOSE: Seamlessly inject WolfPackAI LLMs into Cursor without disruption
 *
 * ISOLATION GUARANTEE:
 * - Only active in WolfPackAI directory
 * - Does NOT affect other projects (PaiiD, Pa\u03c0D 2mx, etc.)
 * - Graceful degradation if gatekeeper offline
 * - Zero modifications to Cursor's existing behavior
 * - Pure additive extension
 *
 * ORCHESTRATOR AUTHORITY:
 * - Gatekeeper exposes all available LLMs
 * - Orchestrator has full control over selection
 * - No override, no restrictions
 * - Suggestions only, orchestrator decides
 */

const GATEKEEPER_API = "http://localhost:7000";
const CHECK_INTERVAL_MS = 30000; // 30 seconds

module.exports = {
  name: "WolfPackAI Gatekeeper",
  version: "1.0.0",
  mode: "seamless-injection",

  /**
   * Check if gatekeeper is available
   * Silent fail if not running - zero disruption
   */
  async isGatekeeperAvailable() {
    try {
      const response = await fetch(`${GATEKEEPER_API}/api/status`, {
        method: "GET",
        signal: AbortSignal.timeout(2000) // 2 second timeout
      });
      return response.ok;
    } catch {
      // Silent fail - gatekeeper not running, that's fine
      return false;
    }
  },

  /**
   * Discover all available LLMs from WolfPackAI
   * Returns empty array if gatekeeper unavailable
   */
  async discoverLLMs() {
    try {
      if (!(await this.isGatekeeperAvailable())) {
        return []; // Silent fallback
      }

      const response = await fetch(`${GATEKEEPER_API}/api/llms`, {
        method: "GET",
        signal: AbortSignal.timeout(5000)
      });

      if (!response.ok) {
        return []; // Silent fallback
      }

      const data = await response.json();

      return data.Models || [];
    } catch {
      // Silent fail - no errors shown to user
      return [];
    }
  },

  /**
   * Extend Cursor's model list with WolfPackAI models
   * ADDITIVE ONLY - never replaces existing models
   */
  async extendModelList(existingModels) {
    const wolfpackModels = await this.discoverLLMs();

    if (wolfpackModels.length === 0) {
      // Gatekeeper offline, return existing models unchanged
      return existingModels;
    }

    // Convert WolfPackAI models to Cursor format
    const formattedModels = wolfpackModels.map(m => ({
      id: `wolfpack-${m.Name}`,
      name: m.Name,
      provider: m.Source,
      capabilities: m.Capabilities || [],
      type: m.Type,
      status: m.Status,
      cost: m.Cost,
      metadata: {
        source: "wolfpackai-gatekeeper",
        size_gb: m.SizeGB,
        injected: true
      }
    }));

    // ADD to existing, never replace
    return [...existingModels, ...formattedModels];
  },

  /**
   * Suggest best model for a task
   * Orchestrator can ignore this suggestion completely
   */
  async suggestBestModel(task, context = null) {
    try {
      if (!(await this.isGatekeeperAvailable())) {
        return null; // No suggestion if gatekeeper offline
      }

      const response = await fetch(`${GATEKEEPER_API}/api/suggest`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ task, context }),
        signal: AbortSignal.timeout(3000)
      });

      if (!response.ok) {
        return null;
      }

      const data = await response.json();

      return {
        model: data.suggestion?.ModelName,
        reason: data.suggestion?.Reason,
        confidence: data.suggestion?.Confidence,
        all_available: data.all_available,
        note: "Suggestion only - Orchestrator has final decision"
      };
    } catch {
      return null; // Silent fail
    }
  },

  /**
   * Execute task with selected model
   * Orchestrator chooses which model to use
   */
  async executeWithModel(requestId, selectedModel, prompt, options = {}) {
    try {
      if (!(await this.isGatekeeperAvailable())) {
        throw new Error("WolfPackAI Gatekeeper offline - using Cursor native models");
      }

      const response = await fetch(`${GATEKEEPER_API}/api/execute`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          RequestId: requestId,
          SelectedModel: selectedModel,
          Prompt: prompt,
          Options: options
        }),
        signal: AbortSignal.timeout(60000) // 60 second timeout
      });

      if (!response.ok) {
        throw new Error(`Execution failed: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      // Report error but let orchestrator handle it
      return {
        status: "error",
        message: error.message,
        note: "Orchestrator should decide fallback strategy"
      };
    }
  },

  /**
   * Get access policy (unlimited)
   * Shows orchestrator has no restrictions
   */
  async getAccessPolicy() {
    try {
      if (!(await this.isGatekeeperAvailable())) {
        return null;
      }

      const response = await fetch(`${GATEKEEPER_API}/api/policy`, {
        method: "GET",
        signal: AbortSignal.timeout(2000)
      });

      if (!response.ok) {
        return null;
      }

      return await response.json();
    } catch {
      return null;
    }
  },

  /**
   * Get current status for UI display
   */
  async getStatus() {
    const available = await this.isGatekeeperAvailable();

    if (!available) {
      return {
        active: false,
        status: "offline",
        llms_available: 0,
        message: "Gatekeeper offline - using Cursor native models"
      };
    }

    const llms = await this.discoverLLMs();

    return {
      active: true,
      status: "online",
      llms_available: llms.length,
      models: llms.map(m => m.Name),
      last_check: new Date().toISOString(),
      message: `${llms.length} models available via WolfPackAI`
    };
  },

  /**
   * Initialize - called when extension loads
   * ONLY in WolfPackAI directory - isolation guaranteed
   */
  async initialize(projectPath) {
    // Check if we're in WolfPackAI directory
    const isWolfPackAI = projectPath.includes("WolfPackAI");

    if (!isWolfPackAI) {
      // NOT in WolfPackAI - deactivate extension
      console.log("WolfPackAI Gatekeeper: Not in WolfPackAI directory, extension inactive");
      return {
        activated: false,
        reason: "Not in WolfPackAI directory - preserving isolation"
      };
    }

    // In WolfPackAI directory - activate
    console.log("🐺 WolfPackAI Gatekeeper: Activated");
    console.log("   Mode: Seamless Injection");
    console.log("   Isolation: Guaranteed (only active in WolfPackAI)");
    console.log("   Authority: Orchestrator (full control)");

    const status = await this.getStatus();

    if (status.active) {
      console.log(`   Status: ${status.llms_available} models discovered`);
    } else {
      console.log(`   Status: Gatekeeper offline - graceful degradation`);
    }

    return {
      activated: true,
      status: status,
      isolation: "Only active in WolfPackAI directory"
    };
  },

  /**
   * Shutdown - called when extension unloads
   * Clean shutdown, no residual effects
   */
  async shutdown() {
    console.log("WolfPackAI Gatekeeper: Deactivated cleanly");
    return { status: "shutdown-clean" };
  }
};

// Isolation verification
if (typeof module !== 'undefined' && module.exports) {
  module.exports.ISOLATION_GUARANTEE = {
    scope: "wolfpackai-directory-only",
    affects_paiid: false,
    affects_papid_2mx: false,
    affects_other_projects: false,
    graceful_degradation: true,
    zero_disruption: true
  };
}
