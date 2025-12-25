namespace Cirreum.Communications.Email.Health;

using Cirreum.Health;

/// <summary>
/// Configuration options for Azure Communication Services email health checks.
/// Extends the base health check options with Azure-specific monitoring capabilities.
/// </summary>
public sealed class AzureEmailHealthCheckOptions : ServiceProviderHealthCheckOptions {

    /// <summary>
    /// Gets or sets the timeout for the health check operation.
    /// This controls how long the health check will wait for Azure to respond.
    /// </summary>
    /// <value>The health check timeout. Defaults to 10 seconds.</value>
    public new TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets whether to perform a test email send as part of the health check.
    /// When true, attempts to send a test email to validate the service is fully operational.
    /// </summary>
    /// <value>true to send a test email; otherwise, false. Defaults to false.</value>
    public bool SendTestEmail { get; set; } = false;

    /// <summary>
    /// Gets or sets the recipient address for test emails when SendTestEmail is true.
    /// If not specified, uses a no-reply address at the sender domain.
    /// </summary>
    /// <value>The test email recipient address, or null to use default.</value>
    public string? TestEmailRecipient { get; set; }

    /// <summary>
    /// Gets or sets whether to wait for test email completion.
    /// When true, the health check waits for the email operation to complete.
    /// When false, only validates that the operation was accepted.
    /// </summary>
    /// <value>true to wait for completion; otherwise, false. Defaults to false.</value>
    public bool WaitForTestEmailCompletion { get; set; } = false;

}