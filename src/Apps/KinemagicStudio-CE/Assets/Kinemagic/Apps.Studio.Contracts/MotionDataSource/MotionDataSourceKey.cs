using System;

namespace Kinemagic.Apps.Studio.Contracts.MotionDataSource
{
    public readonly struct MotionDataSourceKey : IEquatable<MotionDataSourceKey>
    {
        public int Port { get; }
        public string ServerAddress { get; }

        public MotionDataSourceKey(int port) : this("0.0.0.0", port)
        {
        }

        public MotionDataSourceKey(string serverAddress, int port)
        {
            if (port < 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 0 and 65535");
            }

            Port = port;
            ServerAddress = string.IsNullOrWhiteSpace(serverAddress) ? "0.0.0.0" : serverAddress.Trim().ToLowerInvariant();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Port,
                StringComparer.OrdinalIgnoreCase.GetHashCode(ServerAddress ?? string.Empty)
            );
        }

        public override bool Equals(object obj)
        {
            return obj is MotionDataSourceKey key && Equals(key);
        }

        public bool Equals(MotionDataSourceKey other)
        {
            return Port == other.Port &&
                   string.Equals(ServerAddress, other.ServerAddress, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return $"{ServerAddress}:{Port}";
        }

        public static bool operator ==(MotionDataSourceKey left, MotionDataSourceKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MotionDataSourceKey left, MotionDataSourceKey right)
        {
            return !left.Equals(right);
        }
    }
}
