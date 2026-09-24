using Arcade1972.Core;
using Arcade1972.Infrastructure;
using Microsoft.Windows.Storage;

namespace Arcade1972.App.Storage;

public sealed class LocalStateFreePlayQuotaStore : IFreePlayQuotaStore
{
    private readonly AtomicJsonFreePlayQuotaStore innerStore;

    public LocalStateFreePlayQuotaStore()
    {
        using var applicationData = ApplicationData.GetDefault();
        innerStore = new AtomicJsonFreePlayQuotaStore(applicationData.LocalPath);
    }

    public ValueTask<FreePlayQuotaState?> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        return innerStore.LoadAsync(cancellationToken);
    }

    public ValueTask SaveAsync(
        FreePlayQuotaState state,
        CancellationToken cancellationToken = default)
    {
        return innerStore.SaveAsync(state, cancellationToken);
    }
}