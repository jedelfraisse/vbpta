namespace WebApp.Infrastructure;

public static class MigrationTriggerProcessor
{
	public static async Task ProcessAsync(
		string webRootPath,
		Func<Task> migrateAsync,
		ILogger logger,
		Func<DateTimeOffset>? nowProvider = null)
	{
		var triggerPath = Path.Combine(webRootPath, "run-migration.txt");
		if (!File.Exists(triggerPath))
		{
			return;
		}

		var timestamp = (nowProvider ?? (() => DateTimeOffset.UtcNow))().ToString("yyyyMMddHHmmss");
		try
		{
			await migrateAsync();
			var donePath = Path.Combine(webRootPath, $"run-migration.{timestamp}.done");
			File.Move(triggerPath, donePath, true);
			logger.LogInformation("EF Core migrations completed successfully. Marker renamed to {DonePath}", donePath);
		}
		catch (Exception ex)
		{
			var failedPath = Path.Combine(webRootPath, $"run-migration.failed.{timestamp}.txt");
			File.Move(triggerPath, failedPath, true);
			logger.LogError(ex, "EF Core migrations failed. Marker renamed to {FailedPath}", failedPath);
			throw;
		}
	}
}
