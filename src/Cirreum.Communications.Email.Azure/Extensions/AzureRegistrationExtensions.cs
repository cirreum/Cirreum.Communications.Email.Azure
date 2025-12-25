namespace Microsoft.Extensions.Hosting;

using Cirreum.Communications.Email;
using Cirreum.Communications.Email.Configuration;
using Cirreum.Communications.Email.Health;
using Cirreum.ServiceProvider.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

internal static class AzureRegistrationExtensions {

    public static void AddAzureEmailService(
        this IServiceCollection services,
        string serviceKey,
        AzureEmailInstanceSettings settings) {

        // Keyed IEmailService factory → constructs a client bound to this instance settings
        services.AddKeyedSingleton<IEmailService>(
            serviceKey,
            (sp, key) => {
                var logger = sp.GetRequiredService<ILogger<AzureEmailService>>();
                
                // Create client based on authentication method
                EmailClient client;
                if (!string.IsNullOrWhiteSpace(settings.ConnectionString)) {
                    // Connection string authentication (SysAdmin provided connection string)
                    client = new EmailClient(settings.ConnectionString);
                } else if (!string.IsNullOrWhiteSpace(settings.Endpoint)) {
                    // Endpoint authentication (SysAdmin provided endpoint only - use managed identity)
                    client = new EmailClient(new Uri(settings.Endpoint), new DefaultAzureCredential());
                } else {
                    throw new InvalidOperationException("Either ConnectionString or Endpoint must be configured");
                }

                return new AzureEmailService(client, settings, logger);
            });

        // Register Default (non-Keyed) Service Factory (wraps the keyed registration)
        if (serviceKey.Equals(ServiceProviderSettings.DefaultKey, StringComparison.OrdinalIgnoreCase)) {
            services.TryAddSingleton(sp => sp.GetRequiredKeyedService<IEmailService>(serviceKey));
        }
    }

    public static AzureEmailHealthCheck CreateAzureEmailHealthCheck(
        this IServiceProvider sp,
        string serviceKey,
        AzureEmailInstanceSettings settings) {
        
        var service = sp.GetRequiredKeyedService<IEmailService>(serviceKey);
        var logger = sp.GetRequiredService<ILogger<AzureEmailHealthCheck>>();
        return new AzureEmailHealthCheck(service, settings, logger);
    }
}