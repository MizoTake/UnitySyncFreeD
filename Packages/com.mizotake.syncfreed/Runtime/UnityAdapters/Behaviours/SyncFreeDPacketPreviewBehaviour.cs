using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class SyncFreeDPacketPreviewBehaviour : MonoBehaviour
    {
        [SerializeField] private FreeDUdpOutputBehaviour output;
        [SerializeField] private Rect rect = new Rect(16f, 16f, 640f, 80f);

        private void Reset()
        {
            output = GetComponent<FreeDUdpOutputBehaviour>();
        }

        private void Awake()
        {
            if (output == null)
            {
                output = GetComponent<FreeDUdpOutputBehaviour>();
            }
        }

        private void OnGUI()
        {
            if (output == null)
            {
                output = GetComponent<FreeDUdpOutputBehaviour>();
            }

            if (output == null)
            {
                return;
            }

            GUI.Box(rect, output.LastPacketHex);
        }
    }
}
