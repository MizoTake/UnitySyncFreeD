using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Outputs
{
    public interface IFreeDPacketBuilder
    {
        int Build(in CameraSyncState state, System.Span<byte> destination);
    }
}
