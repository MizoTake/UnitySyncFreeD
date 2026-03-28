using System;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Outputs
{
    public sealed class FreeDOutput : ICameraOutput
    {
        private readonly IFreeDPacketBuilder packetBuilder;
        private readonly Action<byte[]> transport;
        private readonly byte[] buffer = new byte[FreeDPacketBuilder.PacketLength];

        public FreeDOutput(Action<byte[]> transport)
            : this(new FreeDPacketBuilder(), transport)
        {
        }

        public FreeDOutput(IFreeDPacketBuilder packetBuilder, Action<byte[]> transport)
        {
            this.packetBuilder = packetBuilder ?? throw new ArgumentNullException(nameof(packetBuilder));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public byte[] LastPacket { get; private set; } = Array.Empty<byte>();

        public void Send(in CameraSyncState state)
        {
            packetBuilder.Build(state, buffer);
            LastPacket = (byte[])buffer.Clone();
            transport(LastPacket);
        }
    }
}
