using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Abstractions
{
    public interface IViscaTelemetryProvider
    {
        bool TryGetTelemetry(out ViscaTelemetryFrame frame);
    }
}
