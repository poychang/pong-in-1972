using System.Text.Json;
using Arcade1972.Core;
using Arcade1972.Infrastructure;

namespace Arcade1972.Tests;

public sealed class AtomicJsonFreePlayQuotaStoreTests : IDisposable
{
    private readonly string directoryPath = Path.Combine(
        Path.GetTempPath(),
        "Arcade1972.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Load_ReturnsNullWhenStateDoesNotExist()
    {
        var store = new AtomicJsonFreePlayQuotaStore(directoryPath);

        var state = await store.LoadAsync();

        Assert.Null(state);
    }

    [Fact]
    public async Task SaveAndLoad_PreservesQuotaState()
    {
        var store = new AtomicJsonFreePlayQuotaStore(directoryPath);
        var expected = CreateState(2);

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.Equal(expected.LastObservedUtcDate, actual!.LastObservedUtcDate);
        Assert.Equal(expected.CompletedMatches, actual.CompletedMatches);
    }

    [Fact]
    public async Task Save_ReplacesPreviousStateWithoutLeavingTemporaryFiles()
    {
        var store = new AtomicJsonFreePlayQuotaStore(directoryPath);

        await store.SaveAsync(CreateState(1));
        await store.SaveAsync(CreateState(3));
        var actual = await store.LoadAsync();

        Assert.Equal(3, actual!.CompletedMatches.Count);
        Assert.Empty(Directory.GetFiles(directoryPath, "*.tmp"));
    }

    [Fact]
    public async Task Load_QuarantinesMalformedJsonAndReturnsNull()
    {
        Directory.CreateDirectory(directoryPath);
        var stateFilePath = Path.Combine(directoryPath, "free-play-quota.json");
        await File.WriteAllTextAsync(stateFilePath, "{ not valid json");
        var store = new AtomicJsonFreePlayQuotaStore(directoryPath);

        var state = await store.LoadAsync();

        Assert.Null(state);
        Assert.False(File.Exists(stateFilePath));
        Assert.True(File.Exists($"{stateFilePath}.corrupt"));
    }

    [Fact]
    public async Task Load_QuarantinesSemanticallyInvalidState()
    {
        Directory.CreateDirectory(directoryPath);
        var stateFilePath = Path.Combine(directoryPath, "free-play-quota.json");
        var invalidState = new FreePlayQuotaState(
            new DateOnly(2026, 9, 24),
            [new CompletedFreePlay(Guid.Empty, new DateOnly(2026, 9, 24))]);
        await File.WriteAllTextAsync(stateFilePath, JsonSerializer.Serialize(invalidState));
        var store = new AtomicJsonFreePlayQuotaStore(directoryPath);

        var state = await store.LoadAsync();

        Assert.Null(state);
        Assert.True(File.Exists($"{stateFilePath}.corrupt"));
    }

    public void Dispose()
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private static FreePlayQuotaState CreateState(int completedMatchCount)
    {
        var quotaDate = new DateOnly(2026, 9, 24);
        var completedMatches = Enumerable.Range(0, completedMatchCount)
            .Select(_ => new CompletedFreePlay(Guid.NewGuid(), quotaDate))
            .ToArray();
        return new FreePlayQuotaState(quotaDate, completedMatches);
    }
}