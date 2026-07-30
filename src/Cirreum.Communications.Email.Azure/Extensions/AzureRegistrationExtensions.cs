namespace Microsoft.Extensions.Hosting;

using global::Azure.Core;
using Cirreum.Communications.Email;
using Cirreum.Communications.Email.Configuration;
using Cirreum.Communications.Email.Health;
using Cirreum.Providers.Configuration;
using Cirreum.ServiceProvider.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

internal static class AzureRegistrationExtensions {

    public static void AddAzureEmailService(
        this IServiceCollection services,
        string serviceKey,
        AzureEmailInstanceSettings settings) {

        // Mirrors the client construction below: a non-blank connection string is key-based
        // authentication, which a Credential block cannot apply to.
        if (!string.IsNullOrWhiteSpace(settings.ConnectionString) && settings.Credential is not null) {
            throw new InvalidOperationException(
                "A Credential block is configured but the instance uses a connection string. " +
                "Identity-based authentication requires Endpoint without a connection string.");
        }

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
                    // Endpoint authentication — identity selected by the instance Credential block
                    client = new EmailClient(new Uri(settings.Endpoint), settings.GetCredential());
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

    private static TokenCredential GetCredential(
        this AzureEmailInstanceSettings settings) {

        var tenantId = string.IsNullOrWhiteSpace(settings.Identifier) ? null : settings.Identifier;
        var credential = settings.Credential ?? new CredentialSettings();
        var identityId = string.IsNullOrWhiteSpace(credential.IdentityId) ? null : credential.IdentityId;

        return credential.Mode switch {

            CredentialMode.Default => new DefaultAzureCredential(new DefaultAzureCredentialOptions {
                TenantId = tenantId,
                ManagedIdentityClientId = identityId,
            }),

            CredentialMode.ManagedIdentity => new ManagedIdentityCredential(
                identityId is null
                    ? ManagedIdentityId.SystemAssigned
                    : ManagedIdentityId.FromUserAssignedClientId(identityId)),

            CredentialMode.Developer => new ChainedTokenCredential(
                new VisualStudioCredential(new VisualStudioCredentialOptions { TenantId = tenantId }),
                new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId }),
                new AzurePowerShellCredential(new AzurePowerShellCredentialOptions { TenantId = tenantId })),

            _ => throw new InvalidOperationException(
                $"CredentialMode '{credential.Mode}' is not supported by the Azure Email provider."),

        };

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