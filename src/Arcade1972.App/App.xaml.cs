using Arcade1972.App.Storage;
using Arcade1972.Core;
using Arcade1972.Infrastructure;
using Microsoft.UI.Xaml;

namespace Arcade1972.App;

public partial class App : Application
{
    private Window? window;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var freePlayQuota = new DailyFreePlayQuota(
            new SystemUtcClock(),
            new LocalStateFreePlayQuotaStore());
        window = new MainWindow(freePlayQuota);
        window.Activate();
    }
}