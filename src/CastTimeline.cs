using System;

namespace EventHorizon
{
    internal static class CastTimeline
    {
        internal static bool Reached(float distanceSquared, float radius) { return radius > 0f && distanceSquared <= radius * radius; }
        internal static float Expansion(float age, float seconds)
        {
            float t = Math.Max(0f, Math.Min(1f, age / Math.Max(0.01f, seconds)));
            return t * t * (3f - 2f * t);
        }
    }
}
