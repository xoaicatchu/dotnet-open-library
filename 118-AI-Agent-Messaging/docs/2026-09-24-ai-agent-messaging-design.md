# 118-AI-Agent-Messaging Design

## Goal
Build a .NET 10 showcase project demonstrating AI Agent integration into an event-driven messaging pipeline using MassTransit, where the AI model is fully pluggable (swap/disable without affecting the core workflow).

## Core Principle
> AI model is a plug-in, NOT a dependency. The business workflow (Order → Message → Consumer → Update) works independently. AI is an "enrichment step" that can be enabled/disabled/swapped at runtime.

## Architecture

```
Client → POST /api/orders → OrdersController
  → Save Order (Status=Submitted) → Publish(OrderSubmitted)
  → OrderSubmittedConsumer:
      1. Call IAiOrderEnricher.EnrichAsync(order)
      2. Update Order (Status=Processed, AiCategory, AiSummary, AiProvider)
      3. If AI disabled/error → fallback NoOp, workflow still completes
```

## AI Pluggable Design

### Interface
```csharp
public interface IAiOrderEnricher
{
    Task<AiEnrichmentResult> EnrichAsync(OrderEnrichmentRequest request, CancellationToken ct);
    bool IsAvailable { get; }
}
```

### Implementations
1. **SemanticKernelOrderEnricher** — Uses Microsoft Semantic Kernel (OpenAI/Azure/Ollama)
2. **NoOpOrderEnricher** — Fallback when AI disabled, returns default values

### Configuration
```json
{
  "AiProvider": {
    "Enabled": true,
    "Provider": "OpenAI",
    "ModelId": "gpt-4o-mini",
    "ApiKey": "sk-...",
    "Endpoint": ""
  }
}
```

## Folder Structure
```
118-AI-Agent-Messaging/
├── AiAgentMessaging.Api/
│   ├── Ai/
│   ├── Consumers/
│   ├── Contracts/
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   └── Program.cs
├── AiAgentMessaging.Tests/
├── AiAgentMessaging.slnx
└── README.md
```

## Tech Stack
- .NET 10, C# 13
- MassTransit (InMemory transport)
- Microsoft.SemanticKernel
- xUnit + WebApplicationFactory
- Swashbuckle (Swagger)
- Port: 5153
