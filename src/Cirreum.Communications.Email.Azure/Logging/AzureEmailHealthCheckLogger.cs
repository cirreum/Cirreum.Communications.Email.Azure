namespace Cirreum.Communications.Email.Logging;

using Azure.Communication.Email;
using Microsoft.Extensions.Logging;

/// <summary>
/// High-performance logging extensions for Azure Communication Services email health check operations.
/// Uses compile-time source generation for optimal performance and structured logging.
/// </summary>
internal static partial class AzureEmailHealthCheckLogger {

	private const int BaseEventId = 14100; // Azure Email Health Check range

	[LoggerMessage(
		EventId = BaseEventId + 1,
		Level = LogLevel.Debug,
		Message = "{Header}: Starting health check for instance '{Instance}'")]
	public static partial void LogHealthCheckStart(this ILogger logger, string header, string instance);

	[LoggerMessage(
		EventId = BaseEventId + 2,
		Level = LogLevel.Information,
		Message = "{Header}: Health check completed successfully for instance '{Instance}' - Status: {Status}")]
	public static partial void LogHealthCheckSuccess(this ILogger logger, string header, string instance, string status);

	[LoggerMessage(
		EventId = BaseEventId + 3,
		Level = LogLevel.Warning,
		Message = "{Header}: Health check completed with degraded status for instance '{Instance}' - Reason: {Reason}")]
	public static partial void LogHealthCheckDegraded(this ILogger logger, string header, string instance, string reason);

	[LoggerMessage(
		EventId = BaseEventId + 4,
		Level = LogLevel.Error,
		Message = "{Header}: Health check failed for instance '{Instance}' - Reason: {Reason}")]
	public static partial void LogHealthCheckFailed(this ILogger logger, Exception? ex, string header, string instance, string reason);

	[LoggerMessage(
		EventId = BaseEventId + 5,
		Level = LogLevel.Debug,
		Message = "{Header}: Sending test email to '{Recipient}' for health check validation")]
	public static partial void LogTestEmailSending(this ILogger logger, string header, string recipient);

	[LoggerMessage(
		EventId = BaseEventId + 6,
		Level = LogLevel.Information,
		Message = "{Header}: Test email sent successfully - Operation ID: {OperationId}")]
	public static partial void LogTestEmailSent(this ILogger logger, string header, string operationId);

	[LoggerMessage(
		EventId = BaseEventId + 7,
		Level = LogLevel.Warning,
		Message = "{Header}: Test email operation completed but with status '{Status}' instead of succeeded")]
	public static partial void LogTestEmailNonSuccessStatus(this ILogger logger, string header, EmailSendStatus status);

	[LoggerMessage(
		EventId = BaseEventId + 8,
		Level = LogLevel.Information,
		Message = "{Header}: Test email accepted for processing - Operation ID: {OperationId}")]
	public static partial void LogTestEmailAccepted(this ILogger logger, string header, string operationId);

	[LoggerMessage(
		EventId = BaseEventId + 9,
		Level = LogLevel.Warning,
		Message = "{Header}: Authentication failed during health check - Status: {StatusCode}")]
	public static partial void LogAuthenticationFailure(this ILogger logger, Exception ex, string header, int statusCode);

	[LoggerMessage(
		EventId = BaseEventId + 10,
		Level = LogLevel.Warning,
		Message = "{Header}: Rate limit exceeded during health check - Status: {StatusCode}")]
	public static partial void LogRateLimitExceeded(this ILogger logger, string header, int statusCode);

	[LoggerMessage(
		EventId = BaseEventId + 11,
		Level = LogLevel.Error,
		Message = "{Header}: Azure request failed during health check - Status: {StatusCode}")]
	public static partial void LogAzureRequestFailed(this ILogger logger, Exception ex, string header, int statusCode);

	[LoggerMessage(
		EventId = BaseEventId + 12,
		Level = LogLevel.Warning,
		Message = "{Header}: Health check timed out after {TimeoutSeconds} seconds")]
	public static partial void LogHealthCheckTimeout(this ILogger logger, string header, double timeoutSeconds);

	[LoggerMessage(
		EventId = BaseEventId + 13,
		Level = LogLevel.Debug,
		Message = "{Header}: Health check cancelled")]
	public static partial void LogHealthCheckCancelled(this ILogger logger, string header);

	[LoggerMessage(
		EventId = BaseEventId + 14,
		Level = LogLevel.Error,
		Message = "{Header}: Unexpected error during health check")]
	public static partial void LogHealthCheckUnexpectedError(this ILogger logger, Exception ex, string header);

	[LoggerMessage(
		EventId = BaseEventId + 15,
		Level = LogLevel.Debug,
		Message = "{Header}: Health check disabled for instance '{Instance}'")]
	public static partial void LogHealthCheckDisabled(this ILogger logger, string header, string instance);

	[LoggerMessage(
		EventId = BaseEventId + 16,
		Level = LogLevel.Information,
		Message = "{Header}: Health check configuration validated - Instance: '{Instance}', TestEmail: {TestEmailEnabled}, WaitForCompletion: {WaitForCompletion}")]
	public static partial void LogHealthCheckConfiguration(this ILogger logger, string header, string instance, bool testEmailEnabled, bool waitForCompletion);
}