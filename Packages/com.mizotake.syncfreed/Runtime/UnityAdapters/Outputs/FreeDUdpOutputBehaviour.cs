using System;
using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Outputs
{
    [Obsolete("Use MizoTake.SyncFreeD.UnityAdapters.Behaviours.FreeDUdpOutputBehaviour instead.")]
    [DisallowMultipleComponent]
    public class FreeDUdpOutputBehaviour : Behaviours.FreeDUdpOutputBehaviour
    {
        public void SendPacket(in CameraSyncState state)
        {
            Send(state);
        }
    }
}
