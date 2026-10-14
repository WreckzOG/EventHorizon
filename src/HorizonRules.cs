using System;
namespace EventHorizon
{
    internal static class HorizonRules
    {
        internal const float Scan=.1f;
        internal const float StunLease=.35f;
        internal static bool InCore(float horizontalSquared,float height,float holeHeight,float radius)
        {float dy=height<0?-height:Math.Max(0,height-holeHeight);return horizontalSquared+dy*dy<=radius*radius;}
        // Wind-up precedes the full active duration; the rapid reveal is part of active time.
        internal static bool Active(float age,float windup,float duration){return age>=windup&&age<windup+duration;}
        internal static float Strength(float distance,float radius){float t=Math.Max(0,Math.Min(1,distance/Math.Max(1,radius)));return .18f+.82f*(1-t)*(1-t);}
        internal static int Stacks(float elapsed,float build){return Math.Min(5,1+(int)(Math.Max(0,elapsed)/Math.Max(.1f,build)*4));}
        internal static float Capture(float distance,float capture){return Math.Max(-.35f,Math.Min(1,(distance-capture)/Math.Max(1,capture)));}
    }
}
