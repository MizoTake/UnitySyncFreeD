using MizoTake.SyncFreeD.Core.Abstractions;

namespace MizoTake.SyncFreeD.Core.Sync
{
    public sealed class SystemTimestampProvider : ITimestampProvider
    {
        public long GetTimestampTicks()
        {
            return System.DateTime.UtcNow.Ticks;
        }
    }
}
