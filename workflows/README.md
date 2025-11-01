# WolfPackAI n8n Workflow Templates

Pre-built workflow templates for common AI automation tasks using WolfPackAI services.

## Available Templates

### 1. AI Document Summarizer (`01-ai-document-summarizer.json`)

**Purpose**: Webhook endpoint that summarizes documents using AI

**Endpoint**: `POST http://localhost:5678/webhook/summarize-document`

**Request Body**:
```json
{
  "document": "Your long document text here..."
}
```

**Response**:
```json
{
  "summary": "• Point 1\n• Point 2\n• Point 3",
  "model": "ollama/deepseek-coder-v2:16b",
  "usage": {
    "prompt_tokens": 150,
    "completion_tokens": 45,
    "total_tokens": 195
  }
}
```

**Use Cases**:
- Summarize emails
- Condense meeting notes
- Extract key points from articles
- Generate TL;DR for long documents

---

### 2. Scheduled AI Report Generator (`02-scheduled-ai-report.json`)

**Purpose**: Automatically generates weekly reports every Monday at 9 AM

**Schedule**: Every Monday at 9:00 AM

**Output**: Saves report as `Weekly_Report_YYYY-MM-DD.txt`

**Customization**:
- Edit the cron expression to change schedule
- Modify the prompt to generate different report types
- Add email node to send reports automatically

**Use Cases**:
- Weekly status reports
- Monthly summaries
- Daily standup notes
- Automated documentation

---

### 3. AI Code Review Assistant (`03-ai-code-reviewer.json`)

**Purpose**: AI-powered code review via webhook

**Endpoint**: `POST http://localhost:5678/webhook/review-code`

**Request Body**:
```json
{
  "code": "function example() { ... }",
  "language": "javascript"
}
```

**Response**:
```json
{
  "language": "javascript",
  "review": "Detailed AI review with feedback...",
  "timestamp": "2025-10-31T12:00:00Z",
  "model": "ollama/deepseek-coder-v2:16b",
  "codeLength": 245
}
```

**Reviews Include**:
- Bug detection
- Security vulnerabilities
- Performance suggestions
- Best practices
- Code style improvements

**Use Cases**:
- Pre-commit code reviews
- Pull request automation
- Learning tool for developers
- Code quality gates

---

## Installation

### Import Individual Workflow

1. Open n8n at `http://localhost:5678`
2. Click **"Workflows"** → **"Add Workflow"**
3. Click **"⋮"** menu → **"Import from File"**
4. Select the JSON template file
5. Click **"Save"** and **"Activate"**

### Import All Workflows

```bash
# Copy templates to n8n volume
docker cp workflows/n8n-templates/. wolfpackai-n8n:/home/node/.n8n/workflows/

# Restart n8n
docker restart wolfpackai-n8n
```

---

## Configuration

### Environment Variables

These workflows use environment variables for authentication:

- `LITELLM_MASTER_KEY` - Your LiteLLM API key (automatically available in n8n)

### Webhook URLs

After importing, n8n will generate webhook URLs:

1. Open the workflow
2. Click on the **Webhook** node
3. Copy the **Test URL** or **Production URL**
4. Use this URL to trigger the workflow

---

## Customization Guide

### Changing AI Models

Edit the `model` parameter in HTTP Request nodes:

```json
{
  "model": "ollama/your-model-name"
}
```

Available models (must be installed in Ollama):
- `ollama/deepseek-coder-v2:16b` (coding tasks)
- `ollama/llama3.1:8b` (general purpose)
- `ollama/mistral:7b` (fast responses)

### Adjusting AI Creativity

Modify the `temperature` parameter (0.0 - 1.0):
- `0.1-0.3`: Focused, deterministic (code review, facts)
- `0.5-0.7`: Balanced (general purpose)
- `0.8-1.0`: Creative (writing, brainstorming)

### Adding Email Notifications

Add a **Send Email** node after the AI response:

1. Add node → **Gmail / SMTP**
2. Configure credentials
3. Set recipient, subject, body
4. Connect to workflow

---

## Testing Workflows

### Test via n8n Interface

1. Open workflow
2. Click **"Execute Workflow"** button
3. Provide test data
4. View results in each node

### Test via cURL

**Document Summarizer**:
```bash
curl -X POST http://localhost:5678/webhook/summarize-document \
  -H "Content-Type: application/json" \
  -d '{
    "document": "WolfPackAI is a comprehensive AI development platform..."
  }'
```

**Code Reviewer**:
```bash
curl -X POST http://localhost:5678/webhook/review-code \
  -H "Content-Type: application/json" \
  -d '{
    "code": "function add(a, b) { return a + b; }",
    "language": "javascript"
  }'
```

---

## Advanced Workflows

### Create Multi-Step AI Workflows

**Example: Research Assistant**

1. **Trigger**: Webhook with topic
2. **Step 1**: Generate research questions (AI)
3. **Step 2**: Search web for each question
4. **Step 3**: Summarize findings (AI)
5. **Step 4**: Compile final report (AI)
6. **Response**: Send formatted report

### Integrate with External Services

**Popular Integrations**:
- **Slack**: Send AI summaries to channels
- **GitHub**: Automated PR reviews
- **Google Sheets**: Log AI responses
- **Discord**: AI chatbot
- **Notion**: Auto-generate documentation

---

## Troubleshooting

### Workflow Fails to Execute

**Check**:
1. n8n container is running: `docker ps | grep n8n`
2. LiteLLM is accessible: `curl http://litellm:4000/health`
3. Environment variables are set
4. Workflow is activated (toggle switch)

### AI Returns Errors

**Common Issues**:
- Ollama model not downloaded: `docker exec wolfpackai-ollama ollama list`
- Invalid API key: Check `LITELLM_MASTER_KEY` in environment
- Rate limiting: Adjust workflow execution frequency

### Webhook Not Responding

**Debug Steps**:
1. Check webhook URL is correct
2. Verify n8n port is exposed: `docker ps | grep 5678`
3. Test with n8n's built-in test URL first
4. Check n8n logs: `docker logs wolfpackai-n8n`

---

## Best Practices

1. **Test Before Production**: Use n8n's test mode before activating
2. **Monitor Token Usage**: AI requests consume tokens/resources
3. **Add Error Handling**: Use n8n's error workflows
4. **Version Control**: Export workflows and commit to git
5. **Document Custom Workflows**: Add descriptions in n8n
6. **Secure Webhooks**: Add authentication for production
7. **Rate Limit**: Avoid overwhelming AI services

---

## Contributing

Have a workflow template idea?

1. Create the workflow in n8n
2. Export as JSON
3. Add documentation
4. Submit PR to WolfPackAI repository

---

## Resources

- [n8n Documentation](https://docs.n8n.io/)
- [LiteLLM API Reference](https://docs.litellm.ai/)
- [Ollama Models](https://ollama.ai/library)
- [WolfPackAI Documentation](../../README.md)

---

**Ready to automate?** Import these templates and start building your AI workflows! 🚀
