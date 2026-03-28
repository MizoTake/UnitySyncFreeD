using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class FreeDLoopbackReceiverBehaviour : MonoBehaviour
    {
        [SerializeField] private string bindAddress = "127.0.0.1";
        [SerializeField] private int listenPort = 40000;

        private UdpClient udpClient;

        public byte[] LastPacket { get; private set; } = Array.Empty<byte>();
        public string LastPacketHex { get; private set; } = string.Empty;
        public int ReceivedCount { get; private set; }

        private void OnEnable()
        {
            try
            {
                udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(bindAddress), listenPort));
                udpClient.Client.Blocking = false;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SyncFreeD loopback receiver failed to bind: {exception.Message}", this);
                udpClient = null;
            }
        }

        private void Update()
        {
            if (udpClient == null || udpClient.Available <= 0)
            {
                return;
            }

            try
            {
                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                LastPacket = udpClient.Receive(ref remoteEndPoint);
                LastPacketHex = BitConverter.ToString(LastPacket);
                ReceivedCount++;
            }
            catch (SocketException)
            {
            }
        }

        private void OnDisable()
        {
            udpClient?.Dispose();
            udpClient = null;
        }
    }
}
