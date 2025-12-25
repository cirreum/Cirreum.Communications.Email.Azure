namespace Cirreum.Communications.Email.Configuration;

using Cirreum.Communications.Email.Health;
using Cirreum.ServiceProvider.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Configuration settings for an Azure Communication Services email instance.
/// Provides comprehensive configuration options for Azure email integration including
/// authentication, default settings, batching, and health monitoring.
/// </summary>
public sealed class AzureEmailInstanceSettings
	: ServiceProviderInstanceSettings<AzureEmailHealthCheckOptions> {


	/// <summary>
	/// Gets or sets the Azure Communication Services endpoint.
	/// Used when authenticating with Azure AD instead of connection string.
	/// </summary>
	/// <value>The endpoint URI, or null if using connection string.</value>
	public string? Endpoint { get; set; }



	/// <summary>
	/// Gets or sets the default sender email address used when no explicit sender is specified.
	/// Must be from a verified domain in Azure Communication Services.
	/// </summary>
	/// <value>An <see cref="EmailAddress"/> object containing the default sender information.</value>
	public EmailAddress DefaultFrom { get; set; }

	/// <summary>
	/// Gets or sets the sender domain for Azure Communication Services.
	/// Optional - can be inferred from DefaultFrom if not specified.
	/// </summary>
	/// <value>The sender domain (e.g., "contoso.com"), or null if not specified.</value>
	public string? SenderDomain { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of retry attempts for failed requests.
	/// Valid range: 0-10.
	/// </summary>
	private int _maxRetries = 3;
	public int MaxRetries {
		get => _maxRetries;
		set => _maxRetries = Math.Clamp(value, 0, 10);
	}

	/// <summary>
	/// Gets or sets whether to wait for email operations to complete.
	/// When true, SendEmailAsync will poll until the operation completes or times out.
	/// When false, returns immediately after the operation is queued.
	/// </summary>
	/// <value>true to wait for completion; otherwise, false. Defaults to true.</value>
	public bool WaitForCompletion { get; set; } = true;

	/// <summary>
	/// Gets or sets the timeout for email send operations.
	/// Applies when WaitForCompletion is true.
	/// </summary>
	/// <value>The operation timeout. Defaults to 2 minutes.</value>
	public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(2);

	/// <summary>
	/// Gets or sets the polling interval for checking operation status.
	/// Used when WaitForCompletion is true.
	/// </summary>
	/// <value>The polling interval. Defaults to 1 second.</value>
	public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

	/// <summary>
	/// Gets or sets the bulk sending options.
	/// </summary>
	public AzureEmailBulkSettings BulkOptions { get; set; } = new();

	/// <summary>
	/// Gets or sets a dictionary of global headers that will be added to all outgoing emails.
	/// These headers are applied automatically to every email sent through this instance.
	/// </summary>
	/// <value>A dictionary where keys are header names and values are header values. Defaults to an empty dictionary.</value>
	public Dictionary<string, string> GlobalHeaders { get; set; } = [];

	/// <summary>
	/// Gets or sets a list of global tags/categories for tracking in Azure.
	/// These are added as custom headers for analytics purposes.
	/// </summary>
	/// <value>A list of tag names as strings. Defaults to an empty list.</value>
	public List<string> GlobalTags { get; set; } = [];

	/// <summary>
	/// Gets or sets the health check options for monitoring the Azure email service instance.
	/// </summary>
	/// <value>A <see cref="AzureEmailHealthCheckOptions"/> object containing health check settings. Defaults to a new instance.</value>
	public override AzureEmailHealthCheckOptions? HealthOptions { get; set; } = new();

	/// <summary>
	/// Parses a JSON connection string to populate the ConnectionString and DefaultFrom properties.
	/// Allows KV/Env secret to be provided as JSON with connection details.
	/// Expected JSON format: { "ConnectionString":"...", "DefaultFrom": {"Address":"x@y","Name":"Z"} }
	/// </summary>
	/// <param name="jsonValue">The JSON string containing the configuration data.</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the JSON is invalid, missing required fields, or cannot be parsed.
	/// </exception>
	public override void ParseConnectionString(string jsonValue) {

		// Store the original JSON for debugging purposes
		this.ConnectionString = jsonValue;

		try {
			var keyVaultOptions =
				JsonSerializer.Deserialize<AzureConnectionData>(jsonValue, JsonSerializerOptions.Web)
				?? throw new InvalidOperationException("Invalid Azure email configuration JSON.");

			// Must have either ConnectionString or Endpoint
			if (string.IsNullOrWhiteSpace(keyVaultOptions.ConnectionString) && string.IsNullOrWhiteSpace(keyVaultOptions.Endpoint)) {
				throw new InvalidOperationException("Either ConnectionString or Endpoint must be provided in Azure configuration JSON.");
			}

			// Set the appropriate authentication properties
			if (!string.IsNullOrWhiteSpace(keyVaultOptions.ConnectionString)) {
				this.ConnectionString = keyVaultOptions.ConnectionString;
			}

			if (!string.IsNullOrWhiteSpace(keyVaultOptions.Endpoint)) {
				this.Endpoint = keyVaultOptions.Endpoint;
			}

			// local appsettings takes precedence over KeyVault
			if (string.IsNullOrWhiteSpace(this.DefaultFrom.Address)
				&& !string.IsNullOrWhiteSpace(keyVaultOptions.DefaultFrom?.Address)) {
				this.DefaultFrom = keyVaultOptions.DefaultFrom.Value;
			}

		} catch (JsonException ex) {
			throw new InvalidOperationException("Invalid Azure email configuration format.", ex);
		}
	}

	/// <summary>
	/// Internal record used for deserializing connection string JSON data.
	/// </summary>
	private sealed record AzureConnectionData(
		string? ConnectionString,
		string? Endpoint,
		EmailAddress? DefaultFrom
	);
}