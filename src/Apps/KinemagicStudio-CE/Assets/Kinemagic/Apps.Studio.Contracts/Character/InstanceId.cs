using System;

namespace Kinemagic.Apps.Studio.Contracts.Character
{
    public readonly struct InstanceId : IEquatable<InstanceId>
    {
        public readonly uint Value;

        public InstanceId(uint value)
        {
            Value = value;
        }

        public bool Equals(InstanceId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is InstanceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(InstanceId left, InstanceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InstanceId left, InstanceId right)
        {
            return !left.Equals(right);
        }
    }
}