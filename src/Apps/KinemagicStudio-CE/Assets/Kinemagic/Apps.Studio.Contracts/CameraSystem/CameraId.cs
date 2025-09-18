using System;

namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public readonly struct CameraId : IEquatable<CameraId>
    {
        public readonly int Value;

        public CameraId(int value)
        {
            Value = value;
        }

        public bool Equals(CameraId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is CameraId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(CameraId left, CameraId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CameraId left, CameraId right)
        {
            return !left.Equals(right);
        }
    }
}