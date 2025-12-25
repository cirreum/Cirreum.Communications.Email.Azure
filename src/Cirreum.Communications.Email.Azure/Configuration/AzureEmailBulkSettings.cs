namespace Cirreum.Communications.Email.Configuration;

/// <summary>
/// Configuration settings for bulk email operations in Azure Communication Services.
/// Provides control over concurrency, batching, and rate limiting for bulk email sends.
/// </summary>
public sealed class AzureEmailBulkSettings {

	/// <summary>
	/// Gets or sets the maximum number of concurrent email operations.
	/// This controls how many emails can be sent in parallel.
	/// Valid range: 1-100.
	/// </summary>
	/// <value>The maximum concurrency level. Defaults to 10.</value>
	private int _maxConcurrency = 10;
	public int MaxConcurrency {
		get => _maxConcurrency;
		set => _maxConcurrency = Math.Clamp(value, 1, 100);
	}

	/// <summary>
	/// Gets or sets the maximum batch size for bulk operations.
	/// Since Azure doesn't have a native batch API, this controls how many
	/// recipients to include in a single email when using shared template mode.
	/// Valid range: 1-50 (Azure limit).
	/// </summary>
	/// <value>The maximum batch size. Defaults to 50.</value>
	private int _maxBatchSize = 50;
	public int MaxBatchSize {
		get => _maxBatchSize;
		set => _maxBatchSize = Math.Clamp(value, 1, 50);
	}

	/// <summary>
	/// Gets or sets the delay between batches to avoid rate limiting.
	/// Useful when sending large volumes to respect Azure's rate limits.
	/// </summary>
	/// <value>The delay between batches. Defaults to 100 milliseconds.</value>
	public TimeSpan BatchDelay { get; set; } = TimeSpan.FromMilliseconds(100);

	/// <summary>
	/// Gets or sets whether to wait for operation completion in bulk sends.
	/// When true, each email operation will be polled until completion.
	/// When false, operations are initiated but not tracked.
	/// </summary>
	/// <value>Whether to wait for completion. Defaults to false for better performance.</value>
	public bool WaitForCompletion { get; set; } = false;

	/// <summary>
	/// Gets or sets the maximum time to wait for an individual operation to complete
	/// when WaitForCompletion is true.
	/// </summary>
	/// <value>The operation timeout. Defaults to 2 minutes.</value>
	public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(2);
}