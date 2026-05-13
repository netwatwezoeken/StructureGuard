using System.Text.RegularExpressions;

namespace StructureGuard
{
    public class NamespaceMatcher
    {
        public static CodePart ToCodePart(string root, string input)
        {
            if (input is null)
                return null;

            var template = @"^" + root + /*lang=regex*/
                           @".(?<layer>[^.]+)(?:\.(?<feature>[^.]+))?(?:\.(?<subfeature>[^.]+))?";

            var regex = new Regex(template);
            var match = regex.Match(input);

            if (!match.Success)
            {
                return input.StartsWith(root) ? 
                    new CodePart(input, root) : 
                    new CodePart(input);
            }

            var groups = match.Groups;

            var layerGroup = groups["layer"];
            var featureGroup = groups["feature"];
            var subfeatureGroup = groups["subfeature"];

            if (subfeatureGroup.Success && !string.IsNullOrEmpty(subfeatureGroup.Value))
            {
                return new CodePart(
                    input,
                    root,
                    layerGroup.Value,
                    featureGroup.Value,
                    subfeatureGroup.Value);
            }

            if (featureGroup.Success && !string.IsNullOrEmpty(featureGroup.Value))
            {
                return new CodePart(
                    input,
                    root,
                    layerGroup.Value,
                    featureGroup.Value);
            }

            if (layerGroup.Success && !string.IsNullOrEmpty(layerGroup.Value))
            {
                return new CodePart(
                    input,
                    root,
                    layerGroup.Value);
            }

            return new CodePart(input);
        }
    }
}