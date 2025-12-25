namespace Cirreum.Communications.Email.Configuration;

using Cirreum.Communications.Email.Health;
using Cirreum.ServiceProvider.Configuration;

/// <summary>
/// Global configuration settings for Azure Communication Services email services.
/// Provides centralized configuration management for multiple Azure email service instances
/// within an application, supporting multiple service keys and health check configurations.
/// </summary>
/// <remarks>
/// This class serves as the root configuration container for Azure Communication Services email,
/// inheriting from the base ServiceProviderSettings to provide consistent configuration
/// patterns across the Cirreum service provider ecosystem.
/// </remarks>
public sealed class AzureEmailSettings
	: ServiceProviderSettings<
		AzureEmailInstanceSettings,
		AzureEmailHealthCheckOptions>;