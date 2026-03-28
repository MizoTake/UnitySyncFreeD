using System;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Abstractions
{
    public interface IFreeDPacketBuilder
    {
        int Build(in CameraSyncState state, Span<byte> destination);
    }
}
