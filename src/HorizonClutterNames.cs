namespace EventHorizon
{
    internal static class HorizonClutterNames
    {
        // Native stage cable01/cableLoop and FOLIAGE/SpeedTree names verified in game bundles.
        // Classification is used only after world-layer and gameplay ownership checks.
        internal static bool Decoration(string name)
        {
            if(string.IsNullOrEmpty(name))return false;string n=name.ToLowerInvariant();
            return n.Contains("grass")||n.Contains("foliage")||n.Contains("speedtree")||n.Contains("nature/")||n.Contains("bush")||n.Contains("fern")||n.Contains("leaves")||n.Contains("leaf")||n.Contains("flower")||n.Contains("cable")||n.Contains("rope")||n.Contains("vine");
        }
    }
}
