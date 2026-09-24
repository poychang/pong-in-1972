using Arcade1972.Core;

namespace Arcade1972.Infrastructure;

public sealed class SystemUtcClock : IUtcClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}