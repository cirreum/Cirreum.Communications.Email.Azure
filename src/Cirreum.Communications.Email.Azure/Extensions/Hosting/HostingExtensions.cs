namespace Cirreum.Communications.Email.Extensions.Hosting;

using Cirreum.Communications.Email.Configuration;
using Cirreum.Communications.Email.Health;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for <see cref="IHostApplicationBuilder"/> to configure Azure Communication Services email services.
/// Provides fluent configuration methods for registering Azure email clients with dependency injection,
/// supporting multiple service instances, health checks, and various configuration approaches.
/// </summary>
/// <remarks>
/// These extensions integrate Azure Communication Services email into the .NET hosting model,
/// supporting both connection string and Azure AD authentication from sources like Azure Key Vault.
/// All methods support optional health check configuration for monitoring service availability.
/// </remarks>
public static class HostingExtensions {

	/// <summary>
	/// Adds a manually configured keyed <see cref="IEmailService"/> instance for Azure Communication Services.
	/// </summary>
	/// <param name="builder">The host application builder.</param>
	/// <param name="serviceKey">The unique key identifying this service instance.</param>
	/// <param name="settings">The configuration settings for the Azure email service.</param>
	/// <param name="configureHealth">Optional delegate to configure health check options.</param>
	/// <returns>The builder for chaining.</returns>
	public static IHostApplicationBuilder AddAzureEmailClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		AzureEmailInstanceSettings settings,
		Action<AzureEmailHealthCheckOptions>? configureHealth = null) {
		ArgumentNullException.ThrowIfNull(builder);

		settings.HealthOptions ??= new AzureEmailHealthCheckOptions();
		configureHealth?.Invoke(settings.HealthOptions);

		var registrar = new AzureEmailRegistrar();
		registrar.RegisterInstance(serviceKey, settings, builder.Services, builder.Configuration);
		return builder;
	}

	/// <summary>
	/// Adds a keyed <see cref="IEmailService"/> instance using a configuration delegate.
	/// </summary>
	/// <param name="builder">The host application builder.</param>
	/// <param name="serviceKey">The unique key identifying this service instance.</param>
	/// <param name="configure">Delegate to configure the service settings.</param>
	/// <param name="configureHealth">Optional delegate to configure health check options.</param>
	/// <returns>The builder for chaining.</returns>
	public static IHostApplicationBuilder AddAzureEmailClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		Action<AzureEmailInstanceSettings> configure,
		Action<AzureEmailHealthCheckOptions>? configureHealth = null) {
		var settings = new AzureEmailInstanceSettings();
		configure?.Invoke(settings);
		if (string.IsNullOrWhiteSpace(settings.Name)) {
			settings.Name = serviceKey;
		}
		return builder.AddAzureEmailClient(serviceKey, settings, configureHealth);
	}

	/// <summary>
	/// Adds a keyed <see cref="IEmailService"/> instance using a JSON connection string.
	/// This is useful for configuration stored in Azure Key Vault or environment variables.
	/// </summary>
	/// <param name="builder">The host application builder.</param>
	/// <param name="serviceKey">The unique key identifying this service instance.</param>
	/// <param name="connectionJson">JSON string containing ConnectionString and optionally DefaultFrom.</param>
	/// <param name="configureHealth">Optional delegate to configure health check options.</param>
	/// <returns>The builder for chaining.</returns>
	public static IHostApplicationBuilder AddAzureEmailClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		string connectionJson,
		Action<AzureEmailHealthCheckOptions>? configureHealth = null) {
		var settings = new AzureEmailInstanceSettings {
			Name = serviceKey
		};
		settings.ParseConnectionString(connectionJson);
		return builder.AddAzureEmailClient(serviceKey, settings, configureHealth);
	}

	/// <summary>
	/// Adds a keyed <see cref="IEmailService"/> instance with Azure AD authentication using managed identity.
	/// </summary>
	/// <param name="builder">The host application builder.</param>
	/// <param name="serviceKey">The unique key identifying this service instance.</param>
	/// <param name="endpoint">The Azure Communication Services endpoint URL.</param>
	/// <param name="defaultFrom">The default sender email address.</param>
	/// <param name="configureHealth">Optional delegate to configure health check options.</param>
	/// <returns>The builder for chaining.</returns>
	public static IHostApplicationBuilder AddAzureEmailClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		string endpoint,
		EmailAddress defaultFrom,
		Action<AzureEmailHealthCheckOptions>? configureHealth = null) {
		var settings = new AzureEmailInstanceSettings {
			Name = serviceKey,
			Endpoint = endpoint,
			DefaultFrom = defaultFrom
		};
		return builder.AddAzureEmailClient(serviceKey, settings, configureHealth);
	}
}