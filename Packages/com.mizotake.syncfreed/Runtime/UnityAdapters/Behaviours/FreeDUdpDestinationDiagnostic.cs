namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    public readonly struct FreeDUdpDestinationDiagnostic
    {
        public FreeDUdpDestinationDiagnostic(string endpoint, int order, long elapsedMicroseconds, bool success)
        {
            Endpoint = endpoint ?? string.Empty;
            Order = order;
            ElapsedMicroseconds = elapsedMicroseconds;
            Success = success;
        }

        public string Endpoint { get; }
        public int Order { get; }
        public long ElapsedMicroseconds { get; }
        public bool Success { get; }
        public string Label => Order == 0 ? "Primary" : $"Additional {Order}";
        public string IpAddress => SplitEndpoint(0);
        public int Port => int.TryParse(SplitEndpoint(1), out var port) ? port : 0;
        public bool Succeeded => Success;
        public long OffsetMicroseconds => ElapsedMicroseconds;

        private string SplitEndpoint(int index)
        {
            var parts = Endpoint.Split(':');
            return index >= 0 && index < parts.Length ? parts[index] : string.Empty;
        }
    }
}
