using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class RecordingOutputBehaviour : MonoBehaviour
    {
        private readonly RecordingOutput output = new RecordingOutput();

        public int RecordedFrameCount => output.Frames.Count;
        public string LastCsvLine => output.LastCsvLine;

        public void Send(in CameraSyncState state)
        {
            output.Send(state);
        }

        public void Clear()
        {
            output.Clear();
        }
    }
}
