using System;

namespace NavigationPlatform.Domain
{
    public class DistanceKm : IEquatable<DistanceKm>
    {
        public decimal Value { get; }

        public DistanceKm(decimal value)
        {
            if (value < 0)
            {
                throw new ArgumentException("Distance cannot be negative", nameof(value));
            }

            Value = Math.Round(value, 2);
        }

        public static implicit operator decimal(DistanceKm distance) => distance.Value;

        public static DistanceKm operator +(DistanceKm a, DistanceKm b) => new DistanceKm(a.Value + b.Value);

        public static DistanceKm operator -(DistanceKm a, DistanceKm b) => 
            a.Value >= b.Value ? new DistanceKm(a.Value - b.Value) : new DistanceKm(0);

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            
            return Equals((DistanceKm)obj);
        }

        public bool Equals(DistanceKm other)
        {
            if (other is null) return false;
            return Value.Equals(other.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(DistanceKm left, DistanceKm right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(DistanceKm left, DistanceKm right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{Value} km";
        }
    }
} 