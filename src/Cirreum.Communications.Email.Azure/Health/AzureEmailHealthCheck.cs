namespace Cirreum.Communications.Email.Health;

using Azure;
using Azure.Communication.Email;
using Cirreum.Communications.Email.Configuration;
using Cirreum.Communications.Email.Logging;
using Cirreum.ServiceProvider.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Health check implementation for Azure Communication Services email service.
/// Validates connectivity and optionally performs test email operations.
/// </summary>
public sealed class AzureEmailHealthCheck(
    IEmailService emailService,
    AzureEmailInstanceSettings settings,
    ILogger<AzureEmailHealthCheck> logger
) : IServiceProviderHealthCheck<AzureEmailHealthCheckOptions>
  , IDisposable {

    private const string LogHeader = "Azure Email Health";
    private readonly AzureEmailHealthCheckOptions _options = settings.HealthOptions ?? new();

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {

        var instanceName = settings.Name ?? "default";
        logger.LogHealthCheckStart(LogHeader, instanceName);

        var data = new Dictionary<string, object> {
            ["provider"] = "Azure Communication Services",
            ["instance"] = instanceName
        };

        if (!string.IsNullOrWhiteSpace(settings.SenderDomain)) {
            data["senderDomain"] = settings.SenderDomain;
        }

        logger.LogHealthCheckConfiguration(LogHeader, instanceName, _options.SendTestEmail, _options.WaitForTestEmailCompletion);

        try {
            // Set timeout for the operation
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.Timeout);

            // If test email is not requested, we can only verify the client is configured
            // Azure doesn't have a simple "ping" operation, so we'll consider the client creation successful
            if (!_options.SendTestEmail) {
                // The client was successfully created during DI registration
                // If there were auth issues, they would have failed there
                data["status"] = "configured";
                var result = HealthCheckResult.Healthy("Azure Communication Services email client is configured", data);
                logger.LogHealthCheckSuccess(LogHeader, instanceName, "configured");
                return result;
            }

            // Perform test email send using IEmailService
            var testRecipient = _options.TestEmailRecipient;
            if (string.IsNullOrWhiteSpace(testRecipient)) {
                // Use a no-reply address at the sender domain
                var domain = settings.SenderDomain ?? settings.DefaultFrom.Address.Split('@')[1];
                testRecipient = $"healthcheck-noreply@{domain}";
            }

            logger.LogTestEmailSending(LogHeader, testRecipient);

            var message = new Cirreum.Communications.Email.EmailMessage {
                From = settings.DefaultFrom,
                To = [new Cirreum.Communications.Email.EmailAddress(testRecipient)],
                Subject = "Health Check Test",
                TextContent = $"This is an automated health check test email sent at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.",
                HtmlContent = $"<p>This is an automated health check test email sent at <strong>{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</strong>.</p>",
                Headers = new Dictionary<string, string> {
                    ["X-Health-Check"] = "true",
                    ["X-Instance"] = settings.Name ?? "default"
                }
            };

            var emailResult = await emailService.SendEmailAsync(message, cts.Token);

            data["testEmailSent"] = true;
            data["testRecipient"] = testRecipient;
            if (!string.IsNullOrEmpty(emailResult.MessageId)) {
                data["messageId"] = emailResult.MessageId;
            }

            if (emailResult.Success) {
                data["emailStatus"] = "succeeded";
                logger.LogTestEmailSent(LogHeader, emailResult.MessageId ?? "<none>");
                var result = HealthCheckResult.Healthy("Test email sent successfully", data);
                logger.LogHealthCheckSuccess(LogHeader, instanceName, "succeeded");
                return result;
            } else {
                var errorMessage = emailResult.ErrorMessage ?? "Unknown error";
                data["emailStatus"] = "failed";
                data["error"] = errorMessage;
                if (emailResult.StatusCode.HasValue) {
                    data["statusCode"] = emailResult.StatusCode.Value;
                }
                logger.LogTestEmailNonSuccessStatus(LogHeader, Azure.Communication.Email.EmailSendStatus.Failed);
                var result = HealthCheckResult.Degraded($"Test email failed: {errorMessage}", data: data);
                logger.LogHealthCheckDegraded(LogHeader, instanceName, "Test email failed: " + errorMessage);
                return result;
            }

        } catch (RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403) {
            var errorMsg = "Authentication failed: " + ex.Message;
            data["error"] = errorMsg;
            data["statusCode"] = ex.Status;
            logger.LogAuthenticationFailure(ex, LogHeader, ex.Status);
            var result = HealthCheckResult.Unhealthy("Authentication failed", ex, data);
            logger.LogHealthCheckFailed(ex, LogHeader, instanceName, "Authentication failed");
            return result;
        } catch (RequestFailedException ex) when (ex.Status == 429) {
            data["error"] = "Rate limit exceeded";
            data["statusCode"] = ex.Status;
            logger.LogRateLimitExceeded(LogHeader, ex.Status);
            var result = HealthCheckResult.Degraded("Rate limit exceeded", ex, data);
            logger.LogHealthCheckDegraded(LogHeader, instanceName, "Rate limit exceeded");
            return result;
        } catch (RequestFailedException ex) {
            data["error"] = ex.Message;
            data["statusCode"] = ex.Status;
            logger.LogAzureRequestFailed(ex, LogHeader, ex.Status);
            var failureMessage = "Azure request failed: " + ex.Message;
            var result = HealthCheckResult.Unhealthy(failureMessage, ex, data);
            logger.LogHealthCheckFailed(ex, LogHeader, instanceName, failureMessage);
            return result;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            data["error"] = "Health check cancelled";
            logger.LogHealthCheckCancelled(LogHeader);
            var result = HealthCheckResult.Degraded("Health check cancelled", data: data);
            logger.LogHealthCheckDegraded(LogHeader, instanceName, "Health check cancelled");
            return result;
        } catch (OperationCanceledException) {
            data["error"] = "Health check timed out";
            logger.LogHealthCheckTimeout(LogHeader, _options.Timeout.TotalSeconds);
            var timeoutMessage = "Health check timed out after " + _options.Timeout.TotalSeconds + "s";
            var result = HealthCheckResult.Degraded(timeoutMessage, data: data);
            logger.LogHealthCheckDegraded(LogHeader, instanceName, timeoutMessage);
            return result;
        } catch (Exception ex) {
            data["error"] = ex.Message;
            logger.LogHealthCheckUnexpectedError(ex, LogHeader);
            var unexpectedMessage = "Unexpected error: " + ex.Message;
            var result = HealthCheckResult.Unhealthy(unexpectedMessage, ex, data);
            logger.LogHealthCheckFailed(ex, LogHeader, instanceName, unexpectedMessage);
            return result;
        }
    }

    /// <summary>
    /// Disposes resources used by the health check.
    /// </summary>
    public void Dispose() {
        // No unmanaged resources to dispose in this implementation
    }
}