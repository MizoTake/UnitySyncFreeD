namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    public readonly struct FreeDUdpDestinationDiagnostic
    {
        public FreeDUdpDestinationDiagnostic(string endpoint, int order, long elapsedMicroseconds, bool success)
            : this(ParseIpAddress(endpoint), ParsePort(endpoint), order, elapsedMicroseconds, success)
        {
        }

        public FreeDUdpDestinationDiagnostic(string ipAddress, int port, int order, long elapsedMicroseconds, bool success)
        {
            IpAddress = ipAddress ?? string.Empty;
            Port = port;
            Order = order;
            ElapsedMicroseconds = elapsedMicroseconds;
            Success = success;
        }

        public string Endpoint => string.IsNullOrEmpty(IpAddress) ? string.Empty : $"{IpAddress}:{Port}";
        public int Order { get; }
        public long ElapsedMicroseconds { get; }
        public bool Success { get; }
        public string Label => Order == 0 ? "Primary" : $"Additional {Order}";
        public string IpAddress { get; }
        public int Port { get; }
        public bool Succeeded => Success;
        public long OffsetMicroseconds => ElapsedMicroseconds;

        private static string ParseIpAddress(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                return string.Empty;
            }

            var separatorIndex = endpoint.LastIndexOf(':');
            return separatorIndex > 0 ? endpoint.Substring(0, separatorIndex) : endpoint;
        }

        private static int ParsePort(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                return 0;
            }

            var separatorIndex = endpoint.LastIndexOf(':');
            return separatorIndex >= 0 && separatorIndex < endpoint.Length - 1 && int.TryParse(endpoint.Substring(separatorIndex + 1), out var port) ? port : 0;
        }
    }
}
