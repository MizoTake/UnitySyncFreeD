using System;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Outputs
{
    public sealed class FreeDOutput : ICameraOutput
    {
        private readonly MizoTake.SyncFreeD.Core.Abstractions.IFreeDPacketBuilder packetBuilder;
        private readonly Action<byte[]> transport;
        private readonly byte[] buffer = new byte[FreeDPacketBuilder.PacketLength];
        private byte[] lastPacket = Array.Empty<byte>();
        private bool lastPacketDirty;

        public FreeDOutput(Action<byte[]> transport)
            : this(new FreeDPacketBuilder(), transport)
        {
        }

        public FreeDOutput(MizoTake.SyncFreeD.Core.Abstractions.IFreeDPacketBuilder packetBuilder, Action<byte[]> transport)
        {
            this.packetBuilder = packetBuilder ?? throw new ArgumentNullException(nameof(packetBuilder));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public byte[] LastPacket
        {
            get
            {
                if (!lastPacketDirty)
                {
                    return lastPacket;
                }

                if (lastPacket.Length != buffer.Length)
                {
                    lastPacket = new byte[buffer.Length];
                }

                Array.Copy(buffer, lastPacket, buffer.Length);
                lastPacketDirty = false;
                return lastPacket;
            }
        }

        public void Send(in CameraSyncState state)
        {
            packetBuilder.Build(state, buffer);
            lastPacketDirty = true;
            transport(buffer);
        }
    }
}
