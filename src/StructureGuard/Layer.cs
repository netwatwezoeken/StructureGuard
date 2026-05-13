using System;

namespace StructureGuard
{
    public class Layer : IEquatable<Layer>
    {
        public Layer(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public void Deconstruct(out string Name)
        {
            Name = this.Name;
        }

        public bool Equals(Layer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Layer)obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public static bool operator ==(Layer left, Layer right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Layer left, Layer right)
        {
            return !Equals(left, right);
        }
    }
}