using Microsoft.Extensions.Logging.Abstractions;
using WebApp.Infrastructure;

namespace VbptaWeb.Tests.Infrastructure;

public class MigrationTriggerProcessorTests
{
	[Fact]
	public async Task ProcessAsync_OnSuccess_RenamesTriggerToDoneFile()
	{
		var root = CreateTempDirectory();
		var trigger = Path.Combine(root, "run-migration.txt");
		await File.WriteAllTextAsync(trigger, "run");

		await MigrationTriggerProcessor.ProcessAsync(
			root,
			() => Task.CompletedTask,
			NullLogger.Instance,
			() => new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));

		Assert.False(File.Exists(trigger));
		Assert.True(File.Exists(Path.Combine(root, "run-migration.20260102030405.done")));
	}

	[Fact]
	public async Task ProcessAsync_OnFailure_RenamesTriggerToFailedFile()
	{
		var root = CreateTempDirectory();
		var trigger = Path.Combine(root, "run-migration.txt");
		await File.WriteAllTextAsync(trigger, "run");

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			MigrationTriggerProcessor.ProcessAsync(
				root,
				() => throw new InvalidOperationException("expected"),
				NullLogger.Instance,
				() => new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)));

		Assert.False(File.Exists(trigger));
		Assert.True(File.Exists(Path.Combine(root, "run-migration.failed.20260102030405.txt")));
	}

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"vbpta-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}
}
