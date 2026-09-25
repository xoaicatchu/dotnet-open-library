using Microsoft.SemanticKernel;

namespace RagPipeline.Api.Rag;

/// <summary>
/// Extension method to register RAG services based on configuration.
/// AI model is pluggable — switch provider or disable entirely via appsettings.json.
/// </summary>
public static class RagServiceRegistration
{
    public static IServiceCollection AddRagPipeline(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AiProviderOptions.SectionName).Get<AiProviderOptions>()
                      ?? new AiProviderOptions();

        services.Configure<AiProviderOptions>(configuration.GetSection(AiProviderOptions.SectionName));
        services.AddSingleton<InMemoryVectorStore>();

        if (!options.Enabled || string.Equals(options.Provider, "None", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IRagService, KeywordRagService>();
            return services;
        }

        var kernelBuilder = services.AddKernel();

        switch (options.Provider.ToLowerInvariant())
        {
            case "openai":
                kernelBuilder.AddOpenAIChatCompletion(
                    modelId: options.ModelId,
                    apiKey: options.ApiKey);
                break;

            case "azureopenai":
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: options.ModelId,
                    endpoint: options.Endpoint,
                    apiKey: options.ApiKey);
                break;

            default:
                services.AddSingleton<IRagService, KeywordRagService>();
                return services;
        }

        services.AddSingleton<IRagService, SemanticKernelRagService>();
        return services;
    }
}
