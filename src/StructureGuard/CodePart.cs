namespace StructureGuard
{
    public class CodePart
    {
        public CodePart(string fullName, 
            string root = null,
            string layer = null, 
            string feature = null, 
            string subfeature = null)
        {
            FullName = fullName;
            Root = root;
            Layer = layer;
            Feature = feature;
            Subfeature = subfeature;
        }
        
        public bool TobeAnalyzed => !string.IsNullOrEmpty(Root) && !string.IsNullOrEmpty(Layer) ||
                                    !string.IsNullOrEmpty(Root) && !string.IsNullOrEmpty(Feature);

        public string FullName { get; set; }
        public string Root { get; set; }
        public string Layer { get; set; }
        public string Feature { get; set; }
        public string Subfeature { get; set; }

        public void Deconstruct(out string FullName, out string Root, out string Layer, out string Feature, out string Subfeature)
        {
            FullName = this.FullName;
            Root = this.Root;
            Layer = this.Layer;
            Feature = this.Feature;
            Subfeature = this.Subfeature;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FullName != null ? FullName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Root != null ? Root.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Layer != null ? Layer.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Feature != null ? Feature.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Subfeature != null ? Subfeature.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}