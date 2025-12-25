namespace Cirreum.Communications.Email.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
/// High-performance logging extensions for Azure Communication Services email operations.
/// Uses compile-time source generation for optimal performance and structured logging.
/// </summary>
internal static partial class AzureEmailServiceLogger {

	private const int BaseEventId = 14000; // Azure Email range

	[LoggerMessage(
		EventId = BaseEventId + 1,
		Level = LogLevel.Information,
		Message = "{Header}: Sending email to {To} from {From} with subject '{Subject}'")]
	public static partial void LogSendingEmail(this ILogger logger, string header, string to, string from, string subject);

	[LoggerMessage(
		EventId = BaseEventId + 2,
		Level = LogLevel.Information,
		Message = "{Header}: Email sent successfully with operation ID {MessageId} to {To}")]
	public static partial void LogEmailSuccess(this ILogger logger, string header, string messageId, string to);

	[LoggerMessage(
		EventId = BaseEventId + 3,
		Level = LogLevel.Warning,
		Message = "{Header}: Email send failed with status {StatusCode}: {Error}")]
	public static partial void LogEmailFailure(this ILogger logger, string header, int statusCode, string error);

	[LoggerMessage(
		EventId = BaseEventId + 4,
		Level = LogLevel.Information,
		Message = "{Header}: Retrying request (status {StatusCode}) for {Target}. Delay: {DelayMs}ms. Attempt {Attempt}/{MaxAttempts}")]
	public static partial void LogRetryAttempt(this ILogger logger, string header, int statusCode, string target, int delayMs, int attempt, int maxAttempts);

	[LoggerMessage(
		EventId = BaseEventId + 5,
		Level = LogLevel.Warning,
		Message = "{Header}: Exception during send to {Target}, retrying. Delay: {DelayMs}ms. Attempt {Attempt}/{MaxAttempts}")]
	public static partial void LogExceptionRetry(this ILogger logger, Exception ex, string header, string target, int delayMs, int attempt, int maxAttempts);

	[LoggerMessage(
		EventId = BaseEventId + 6,
		Level = LogLevel.Error,
		Message = "{Header}: Error in parallel bulk send to {Target}")]
	public static partial void LogParallelBulkError(this ILogger logger, Exception ex, string header, string target);

	[LoggerMessage(
		EventId = BaseEventId + 7,
		Level = LogLevel.Information,
		Message = "{Header}: Starting bulk send of {Count} emails")]
	public static partial void LogBulkSendStart(this ILogger logger, string header, int count);

	[LoggerMessage(
		EventId = BaseEventId + 8,
		Level = LogLevel.Information,
		Message = "{Header}: Bulk send completed. Sent: {Sent}, Failed: {Failed}")]
	public static partial void LogBulkSendComplete(this ILogger logger, string header, int sent, int failed);

	[LoggerMessage(
		EventId = BaseEventId + 9,
		Level = LogLevel.Debug,
		Message = "{Header}: Polling operation {OperationId} for completion. Status: {Status}")]
	public static partial void LogOperationPolling(this ILogger logger, string header, string operationId, string status);

	[LoggerMessage(
		EventId = BaseEventId + 10,
		Level = LogLevel.Warning,
		Message = "{Header}: Operation {OperationId} timed out after {TimeoutSeconds} seconds")]
	public static partial void LogOperationTimeout(this ILogger logger, string header, string operationId, int timeoutSeconds);
}