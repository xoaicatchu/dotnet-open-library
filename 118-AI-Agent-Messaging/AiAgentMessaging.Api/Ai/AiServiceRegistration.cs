using Microsoft.SemanticKernel;

namespace AiAgentMessaging.Api.Ai;

/// <summary>
/// Extension method to register AI services based on configuration.
/// This is the single point where AI provider is plugged in or disabled.
/// </summary>
public static class AiServiceRegistration
{
    public static IServiceCollection AddAiEnricher(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AiProviderOptions.SectionName).Get<AiProviderOptions>()
                      ?? new AiProviderOptions();

        services.Configure<AiProviderOptions>(configuration.GetSection(AiProviderOptions.SectionName));

        if (!options.Enabled || string.Equals(options.Provider, "None", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAiOrderEnricher, NoOpOrderEnricher>();
            return services;
        }

        // Register Semantic Kernel with the configured provider
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
                // For unknown providers, fall back to NoOp
                services.AddSingleton<IAiOrderEnricher, NoOpOrderEnricher>();
                return services;
        }

        services.AddSingleton<IAiOrderEnricher, SemanticKernelOrderEnricher>();
        return services;
    }
}
