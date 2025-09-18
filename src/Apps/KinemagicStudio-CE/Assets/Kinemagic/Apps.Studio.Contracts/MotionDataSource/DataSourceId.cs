using System;

namespace Kinemagic.Apps.Studio.Contracts.MotionDataSource
{
    public readonly struct DataSourceId : IEquatable<DataSourceId>
    {
        public readonly int Value;

        public DataSourceId(int value)
        {
            Value = value;
        }

        public bool Equals(DataSourceId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DataSourceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(DataSourceId left, DataSourceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DataSourceId left, DataSourceId right)
        {
            return !left.Equals(right);
        }
    }
}
