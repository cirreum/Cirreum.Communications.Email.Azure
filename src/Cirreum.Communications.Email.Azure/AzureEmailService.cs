namespace Cirreum.Communications.Email;

using Azure;
using Azure.Communication.Email;
using Cirreum.Communications.Email.Configuration;
using Cirreum.Communications.Email.Logging;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;

internal sealed class AzureEmailService(
    EmailClient emailClient,
    AzureEmailInstanceSettings settings,
    ILogger<AzureEmailService> logger
) : IEmailService {

    private const string LogHeader = "Azure Email";
    private const int MaxRetryBackoffExponent = 6;
    private const int JitterMinMs = 250;
    private const int JitterMaxMs = 1000;

    // --------------------------- Single Send ---------------------------
    public async Task<EmailResult> SendEmailAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(message);

        // If From is empty, use instance DefaultFrom
        var msg = this.EnsureFrom(message);

        var errors = Validate(msg);
        if (errors.Count > 0) {
            if (logger.IsEnabled(LogLevel.Warning)) {
                logger.LogWarning("{Header}: validation failed for primary To {To}: {Errors}", LogHeader, msg.To.FirstOrDefault().Address, string.Join("; ", errors));
            }
            return new EmailResult {
                EmailAddress = msg.To.FirstOrDefault().Address,
                Success = false,
                ErrorMessage = "Validation failed",
                ValidationErrors = errors
            };
        }

        cancellationToken.ThrowIfCancellationRequested();

        var to = msg.To.First();
        var toStr = to.ToString();
        var from = msg.From.ToString();
        try {
            logger.LogSendingEmail(
                LogHeader,
                toStr,
                from,
                msg.Subject ?? "<none>");

            var azureMessage = await this.BuildAzureEmailMessageAsync(msg, cancellationToken);
            var operation = await this.SendWithRetryAsync(
                () => emailClient.SendAsync(
                    settings.WaitForCompletion ? WaitUntil.Completed : WaitUntil.Started,
                    azureMessage,
                    cancellationToken),
                to.Address,
                cancellationToken);

            return await this.MapOperationResultAsync(operation, to.Address, cancellationToken);
        } catch (Exception ex) {
            if (logger.IsEnabled(LogLevel.Error)) {
                logger.LogError(ex, "{Header}: Error sending to {To}", LogHeader, to.Address);
            }
            return new EmailResult {
                EmailAddress = to.Address,
                Success = false,
                ErrorMessage = ex.Message,
                Provider = "Azure Communication Services"
            };
        }
    }

    // -------------- Bulk – shared template/message --------------
    public async Task<EmailResponse> SendBulkEmailAsync(
        EmailMessage template,
        IEnumerable<EmailAddress> recipients,
        bool validateOnly = false,
        CancellationToken cancellationToken = default) {

        var list = recipients?.ToList() ?? [];
        if (list.Count == 0) {
            throw new ArgumentException("Recipient list cannot be empty", nameof(recipients));
        }

        // Ensure From default
        var frame = this.EnsureFrom(template);

        var frameErrors = Validate(frame, validateTo: false);
        if (frameErrors.Count > 0) {
            if (logger.IsEnabled(LogLevel.Warning)) {
                logger.LogWarning("{Header}: bulk frame validation failed: {Errors}", LogHeader, string.Join("; ", frameErrors));
            }
            var failed = list.Select(r => new EmailResult {
                EmailAddress = r.Address,
                Success = false,
                ErrorMessage = "Validation failed",
                ValidationErrors = frameErrors
            }).ToList();
            return new EmailResponse(0, failed.Count, failed);
        }

        if (validateOnly) {
            var ok = list.Select(r => new EmailResult {
                EmailAddress = r.Address,
                Success = true,
                Provider = "Azure Communication Services"
            }).ToList();
            return new EmailResponse(ok.Count, 0, ok);
        }

        var results = new List<EmailResult>(list.Count);

        // Azure Communication Services doesn't have batch API, so we send to multiple recipients in chunks
        foreach (var chunk in list.Chunk(Math.Max(1, settings.BulkOptions.MaxBatchSize))) {
            cancellationToken.ThrowIfCancellationRequested();

            if (settings.BulkOptions.BatchDelay > TimeSpan.Zero && results.Count > 0) {
                await Task.Delay(settings.BulkOptions.BatchDelay, cancellationToken);
            }

            try {
                var baseMsg = this.EnsureFrom(frame);
                // Update To with this chunk's recipients
                baseMsg = baseMsg with { To = [.. chunk] };

                var azureMessage = await this.BuildAzureEmailMessageAsync(baseMsg, cancellationToken);
                var operation = await this.SendWithRetryAsync(
                    () => emailClient.SendAsync(
                        settings.BulkOptions.WaitForCompletion ? WaitUntil.Completed : WaitUntil.Started,
                        azureMessage,
                        cancellationToken),
                    $"{chunk.Length} recipients",
                    cancellationToken);

                var opResult = await this.MapOperationResultAsync(operation, chunk.First().Address, cancellationToken);

                // Apply result to all recipients in the chunk
                results.AddRange(chunk.Select(r => new EmailResult {
                    EmailAddress = r.Address,
                    Success = opResult.Success,
                    MessageId = opResult.MessageId,
                    ErrorMessage = opResult.ErrorMessage,
                    Provider = "Azure Communication Services",
                    StatusCode = opResult.StatusCode,
                    RetryAfter = opResult.RetryAfter
                }));

            } catch (Exception ex) {
                if (logger.IsEnabled(LogLevel.Error)) {
                    logger.LogError(ex, "{Header}: error sending bulk chunk of size {Count}", LogHeader, chunk.Length);
                }
                results.AddRange(chunk.Select(r => new EmailResult {
                    EmailAddress = r.Address,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Provider = "Azure Communication Services"
                }));
            }
        }

        var sent = results.Count(r => r.Success);
        var failedCount = results.Count - sent;
        return new EmailResponse(sent, failedCount, results);
    }

    // -------------- Bulk – fully personalized messages --------------
    public async Task<EmailResponse> SendBulkEmailAsync(
        IEnumerable<EmailMessage> messages,
        bool validateOnly = false,
        CancellationToken cancellationToken = default) {

        var list = messages?.ToList() ?? [];
        if (list.Count == 0) {
            throw new ArgumentException("Messages cannot be empty", nameof(messages));
        }

        var results = new ConcurrentBag<EmailResult>();
        int sent = 0, failed = 0;

        // Use semaphore to control concurrency
        using var semaphore = new SemaphoreSlim(settings.BulkOptions.MaxConcurrency, settings.BulkOptions.MaxConcurrency);

        await Parallel.ForEachAsync(
            list,
            new ParallelOptions {
                MaxDegreeOfParallelism = settings.BulkOptions.MaxConcurrency,
                CancellationToken = cancellationToken
            },
            async (msg, token) => {
                await semaphore.WaitAsync(token);
                try {
                    var withFrom = this.EnsureFrom(msg);
                    var errors = Validate(withFrom);
                    if (errors.Count > 0) {
                        results.Add(new EmailResult {
                            EmailAddress = withFrom.To.FirstOrDefault().Address,
                            Success = false,
                            ErrorMessage = "Validation failed",
                            ValidationErrors = errors
                        });
                        Interlocked.Increment(ref failed);
                        return;
                    }

                    if (validateOnly) {
                        results.Add(new EmailResult {
                            EmailAddress = withFrom.To.FirstOrDefault().Address,
                            Success = true,
                            Provider = "Azure Communication Services"
                        });
                        Interlocked.Increment(ref sent);
                        return;
                    }

                    var azureMessage = await this.BuildAzureEmailMessageAsync(withFrom, token);
                    var operation = await this.SendWithRetryAsync(
                        () => emailClient.SendAsync(
                            settings.BulkOptions.WaitForCompletion ? WaitUntil.Completed : WaitUntil.Started,
                            azureMessage,
                            token),
                        withFrom.To.First().Address,
                        token);

                    var mapped = await this.MapOperationResultAsync(
                        operation,
                        withFrom.To.First().Address,
                        token);

                    results.Add(mapped);
                    if (mapped.Success) {
                        Interlocked.Increment(ref sent);
                    } else {
                        Interlocked.Increment(ref failed);
                    }

                } catch (Exception ex) {
                    var target = msg.To.FirstOrDefault().Address ?? "<unknown>";
                    logger.LogParallelBulkError(ex, LogHeader, target);
                    results.Add(new EmailResult {
                        EmailAddress = target,
                        Success = false,
                        ErrorMessage = ex.Message,
                        Provider = "Azure Communication Services"
                    });
                    Interlocked.Increment(ref failed);
                } finally {
                    semaphore.Release();
                }
            });

        return new EmailResponse(sent, failed, [.. results]);
    }

    // ============================== Helpers ==============================

    private EmailMessage EnsureFrom(EmailMessage message) {
        if (string.IsNullOrWhiteSpace(message.From.Address)) {
            return message with { From = settings.DefaultFrom };
        }
        return message;
    }

    private static List<string> Validate(EmailMessage message, bool validateTo = true) {
        var errors = new List<string>();

        if (validateTo && (message.To is null || message.To.Count == 0)) {
            errors.Add("At least one To recipient is required.");
        }

        // Azure Communication Services requires content (no template support)
        var hasContent = !string.IsNullOrWhiteSpace(message.TextContent) ||
                        !string.IsNullOrWhiteSpace(message.HtmlContent);

        if (!hasContent) {
            errors.Add("Either Text or Html content must be provided.");
        }

        // Check recipient count (Azure limit is 50)
        var totalRecipients = (message.To?.Count ?? 0) + (message.Cc?.Count ?? 0) + (message.Bcc?.Count ?? 0);
        if (totalRecipients > 50) {
            errors.Add($"Total recipients ({totalRecipients}) exceeds Azure limit of 50.");
        }

        // Validate all recipients addresses
        var recipients = (message.To ?? [])
            .Concat(message.Cc ?? [])
            .Concat(message.Bcc ?? []);
        foreach (var recipient in recipients) {
            if (string.IsNullOrWhiteSpace(recipient.Address) || !EmailAddress.IsValidEmailAddress(recipient.Address)) {
                errors.Add($"Invalid recipient address: '{recipient.Address}'.");
            }
        }

        // Validate ReplyTo if provided
        if (message.ReplyTo?.Address is { } replyTo && !EmailAddress.IsValidEmailAddress(replyTo)) {
            errors.Add("Invalid ReplyTo address.");
        }

        // Validate attachments
        foreach (var attachment in message.Attachments ?? []) {
            if (attachment.Content is null && attachment.ContentStream is null) {
                errors.Add($"Attachment '{attachment.FileName}' must provide Content or ContentStream.");
            }

            if (string.IsNullOrWhiteSpace(attachment.ContentType)) {
                errors.Add($"Attachment '{attachment.FileName}' must provide ContentType.");
            }
        }

        return errors;
    }

    private async Task<Azure.Communication.Email.EmailMessage> BuildAzureEmailMessageAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default) {

        // Create content
        var content = new EmailContent(message.Subject ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(message.TextContent)) {
            content.PlainText = message.TextContent;
        }
        if (!string.IsNullOrWhiteSpace(message.HtmlContent)) {
            content.Html = message.HtmlContent;
        }

        // Create recipients - convert from Cirreum to Azure types
        var toRecipients = new EmailRecipients(
            message.To?.Select(a => new Azure.Communication.Email.EmailAddress(a.Address, a.Name)) ?? []
        );

        if (message.Cc?.Count > 0) {
            foreach (var cc in message.Cc.Select(a => new Azure.Communication.Email.EmailAddress(a.Address, a.Name))) {
                toRecipients.CC.Add(cc);
            }
        }

        if (message.Bcc?.Count > 0) {
            foreach (var bcc in message.Bcc.Select(a => new Azure.Communication.Email.EmailAddress(a.Address, a.Name))) {
                toRecipients.BCC.Add(bcc);
            }
        }

        // Create the message
        var azureMessage = new Azure.Communication.Email.EmailMessage(
            message.From.Address,
            toRecipients,
            content
        );

        // Set reply-to
        if (message.ReplyTo is { } rt) {
            azureMessage.ReplyTo.Add(new Azure.Communication.Email.EmailAddress(rt.Address, rt.Name));
        }

        // Add headers
        var headers = new Dictionary<string, string>(settings.GlobalHeaders, StringComparer.OrdinalIgnoreCase);
        foreach (var kv in message.Headers) {
            headers[kv.Key] = kv.Value;
        }

        // Map priority
        foreach (var kv in MapPriorityHeaders(message.Priority)) {
            headers[kv.Key] = kv.Value;
        }

        // Add tags as custom headers
        if (settings.GlobalTags.Count > 0 || message.Categories.Count > 0) {
            var tags = settings.GlobalTags.Concat(message.Categories).Distinct();
            headers["X-Tags"] = string.Join(",", tags);
        }

        // Add custom args as headers
        foreach (var kv in message.CustomArgs) {
            headers[$"X-Custom-{kv.Key}"] = kv.Value;
        }

        // Apply all headers
        foreach (var (k, v) in headers) {
            azureMessage.Headers.Add(k, v);
        }

        // Handle attachments
        foreach (var attachment in message.Attachments ?? []) {
            BinaryData binaryData;

            if (attachment.ContentStream is not null) {
                using var ms = new MemoryStream();
                // Safety check: reset position if possible
                var stream = attachment.ContentStream;
                if (stream.CanSeek && stream.Position != 0) {
                    try {
                        stream.Position = 0;
                    } catch (Exception ex) {
                        if (logger.IsEnabled(LogLevel.Warning)) {
                            logger.LogWarning("{Header}: Unable to reset stream position for attachment '{FileName}': {Error}",
                                LogHeader, attachment.FileName, ex.Message);
                        }
                    }
                }
                await stream.CopyToAsync(ms, cancellationToken);
                binaryData = BinaryData.FromBytes(ms.ToArray());
            } else if (attachment.Content is not null) {
                binaryData = BinaryData.FromBytes(attachment.Content);
            } else {
                continue; // Skip if no content
            }

            // Create Azure EmailAttachment using proper constructor
            var azureAttach = new Azure.Communication.Email.EmailAttachment(
                attachment.FileName,
                attachment.ContentType,
                binaryData);

            // Set ContentId for inline attachments if supported
            if (attachment.Disposition == EmailAttachmentDisposition.Inline &&
                !string.IsNullOrWhiteSpace(attachment.ContentId)) {
                // ContentId might be available in Azure SDK 1.1.0
                // azureAttach.ContentId = attachment.ContentId;
            }

            azureMessage.Attachments.Add(azureAttach);
        }

        return azureMessage;
    }

    private static Dictionary<string, string> MapPriorityHeaders(EmailPriority p)
        => p switch {
            EmailPriority.High => new() { ["X-Priority"] = "1", ["X-MSMail-Priority"] = "High", ["Importance"] = "High" },
            EmailPriority.Low => new() { ["X-Priority"] = "5", ["X-MSMail-Priority"] = "Low", ["Importance"] = "Low" },
            _ => []
        };

    private async Task<EmailResult> MapOperationResultAsync(
        EmailSendOperation operation,
        string primaryTo,
        CancellationToken cancellationToken) {

        cancellationToken.ThrowIfCancellationRequested();

        var status = operation.GetRawResponse()?.Status ?? 0;
        var success = (!operation.HasCompleted || operation.HasValue) && operation.Value?.Status == EmailSendStatus.Succeeded;
        var messageId = operation.Id;

        string? error = null;
        TimeSpan? retryAfter = null;

        if (!success) {
            // Note: EmailSendResult in current SDK may not have Error property
            // We'll rely on the status and HTTP response for error details
            error =
                operation.GetRawResponse() is { } response && response.ReasonPhrase is { } reason
                ? reason
                : "Email send failed";

            // Check for retry-after header
            if (operation.GetRawResponse()?.Headers.TryGetValue("Retry-After", out var retryValue) == true) {
                if (int.TryParse(retryValue, out var seconds)) {
                    retryAfter = TimeSpan.FromSeconds(seconds);
                } else if (DateTimeOffset.TryParse(retryValue, out var when)) {
                    retryAfter = when - DateTimeOffset.UtcNow;
                }
            }
        }

        if (success) {
            logger.LogEmailSuccess(LogHeader, messageId ?? "<none>", primaryTo);
        } else {
            logger.LogEmailFailure(LogHeader, status, error ?? "<no details>");
        }

        return new EmailResult {
            EmailAddress = primaryTo,
            Success = success == true,
            MessageId = messageId,
            ErrorMessage = error,
            Provider = "Azure Communication Services",
            StatusCode = status,
            RetryAfter = retryAfter
        };
    }

    // Simple retry with exponential backoff + jitter for rate limits
    private async Task<EmailSendOperation> SendWithRetryAsync(
        Func<Task<EmailSendOperation>> send,
        string target,
        CancellationToken cancellationToken) {
        var maxRetries = settings.MaxRetries;
        for (var attempt = 0; attempt <= maxRetries; attempt++) {
            try {
                var operation = await send();

                // If we're not waiting for completion, return immediately
                if (!settings.WaitForCompletion && !settings.BulkOptions.WaitForCompletion) {
                    return operation;
                }

                // Poll for completion with timeout
                var timeout = DateTimeOffset.UtcNow.Add(
                    settings.BulkOptions.WaitForCompletion
                        ? settings.BulkOptions.OperationTimeout
                        : settings.OperationTimeout);

                while (!operation.HasCompleted && DateTimeOffset.UtcNow < timeout) {
                    await Task.Delay(settings.PollingInterval, cancellationToken);
                    await operation.UpdateStatusAsync(cancellationToken);
                }

                // Check if we should retry based on status
                if (operation.GetRawResponse()?.Status is { } status && (status == 429 || status >= 500)) {
                    if (attempt < maxRetries) {
                        TimeSpan? retryAfter = null;
                        if (operation.GetRawResponse()?.Headers.TryGetValue("Retry-After", out var retryValue) == true) {
                            if (int.TryParse(retryValue, out var seconds)) {
                                retryAfter = TimeSpan.FromSeconds(seconds);
                            }
                        }

                        var delay = retryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, Math.Min(attempt, MaxRetryBackoffExponent))) +
                            TimeSpan.FromMilliseconds(Random.Shared.Next(JitterMinMs, JitterMaxMs));

                        logger.LogRetryAttempt(
                            LogHeader, status, target, (int)delay.TotalMilliseconds, attempt + 1, maxRetries);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                }

                return operation;
            } catch (RequestFailedException ex) when (attempt < maxRetries && (ex.Status == 429 || ex.Status >= 500)) {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, Math.Min(attempt, MaxRetryBackoffExponent))) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(JitterMinMs, JitterMaxMs));
                logger.LogExceptionRetry(ex,
                    LogHeader, target, (int)delay.TotalMilliseconds, attempt + 1, maxRetries);
                await Task.Delay(delay, cancellationToken);
            }
        }

        // Final attempt – no retries left
        return await send();
    }
}