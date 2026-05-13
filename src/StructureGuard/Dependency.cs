using System;

namespace StructureGuard
{
    public class Dependency : IEquatable<Dependency>
    {
        public Dependency(Layer from, Layer to)
        {
            From = from;
            To = to;
        }

        public Layer From { get; set; }
        public Layer To { get; set; }

        public void Deconstruct(out Layer From, out Layer To)
        {
            From = this.From;
            To = this.To;
        }

        public bool Equals(Dependency other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(From, other.From) && Equals(To, other.To);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Dependency)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((From != null ? From.GetHashCode() : 0) * 397) ^ (To != null ? To.GetHashCode() : 0);
            }
        }

        public static bool operator ==(Dependency left, Dependency right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Dependency left, Dependency right)
        {
            return !Equals(left, right);
        }
    }
}