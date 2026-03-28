namespace MizoTake.SyncFreeD.Networking
{
    public readonly struct FreeDUdpNetworkInterfaceInfo
    {
        public FreeDUdpNetworkInterfaceInfo(string name, string address)
        {
            Name = name ?? string.Empty;
            Address = address ?? string.Empty;
        }

        public string Name { get; }
        public string Address { get; }
        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? Address : $"{Name} ({Address})";
        }
    }
}
