using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class DebugLogOutputBehaviour : MonoBehaviour
    {
        [SerializeField] private bool logToUnityConsole = true;

        private DebugLogOutput output;

        public string LastMessage => output != null ? output.LastMessage : string.Empty;

        private void Awake()
        {
            output = new DebugLogOutput(HandleLogMessage);
        }

        public void Send(in CameraSyncState state)
        {
            if (output == null)
            {
                output = new DebugLogOutput(HandleLogMessage);
            }

            output.Send(state);
        }

        private void HandleLogMessage(string message)
        {
            if (logToUnityConsole)
            {
                Debug.Log(message, this);
            }
        }
    }
}
