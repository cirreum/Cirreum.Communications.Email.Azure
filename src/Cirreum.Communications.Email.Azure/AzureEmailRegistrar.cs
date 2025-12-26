namespace Cirreum.Communications.Email;

using Cirreum.Communications.Email.Configuration;
using Cirreum.Communications.Email.Health;
using Cirreum.Providers;
using Cirreum.ServiceProvider;
using Cirreum.ServiceProvider.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Registers and configures Azure Communication Services email services within the application's dependency injection container.
/// </summary>
/// <remarks>This class is responsible for integrating Azure email services into the application, including
/// validating configuration settings, adding service instances, and creating health checks. It supports multiple
/// service instances, each identified by a unique service key.</remarks>
public sealed class AzureEmailRegistrar() :
	ServiceProviderRegistrar<
		AzureEmailSettings,
		AzureEmailInstanceSettings,
		AzureEmailHealthCheckOptions> {

	/// <inheritdoc/>
	public override ProviderType ProviderType => ProviderType.Communications;

	/// <inheritdoc/>
	public override string ProviderName => "Email.Azure";

	/// <inheritdoc/>
	public override string[] ActivitySourceNames { get; } = ["Azure.Communication.*", "Azure.Core.*"];

	/// <inheritdoc/>
	public override void ValidateSettings(AzureEmailInstanceSettings settings) {

		// Must have either connection string or endpoint
		if (string.IsNullOrWhiteSpace(settings.ConnectionString) && string.IsNullOrWhiteSpace(settings.Endpoint)) {
			throw new InvalidOperationException("Azure Communication Services ConnectionString or Endpoint is required");
		}

		// If using endpoint without connection string, will use DefaultAzureCredential (managed identity)
		// Custom TokenCredential can optionally be provided for advanced scenarios

		// DefaultFrom is required
		if (string.IsNullOrWhiteSpace(settings.DefaultFrom.Address)) {
			throw new InvalidOperationException("DefaultFrom Address is required");
		}

		// Validate email format
		if (!EmailAddress.IsValidEmailAddress(settings.DefaultFrom.Address)) {
			throw new InvalidOperationException($"DefaultFrom Address '{settings.DefaultFrom.Address}' is not a valid email address");
		}

		// Extract domain from DefaultFrom if SenderDomain not specified
		if (string.IsNullOrWhiteSpace(settings.SenderDomain)) {
			var atIndex = settings.DefaultFrom.Address.IndexOf('@');
			if (atIndex > 0 && atIndex < settings.DefaultFrom.Address.Length - 1) {
				settings.SenderDomain = settings.DefaultFrom.Address[(atIndex + 1)..];
			}
		}
	}

	/// <inheritdoc/>
	protected override void AddServiceProviderInstance(
		IServiceCollection services,
		string serviceKey,
		AzureEmailInstanceSettings settings) {
		services.AddAzureEmailService(serviceKey, settings);
	}

	/// <inheritdoc/>
	protected override IServiceProviderHealthCheck<AzureEmailHealthCheckOptions> CreateHealthCheck(
		IServiceProvider serviceProvider,
		string serviceKey,
		AzureEmailInstanceSettings settings) {
		return serviceProvider.CreateAzureEmailHealthCheck(serviceKey, settings);
	}
}