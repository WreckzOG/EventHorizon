using System;
namespace EventHorizon
{
    internal static class DomainRules
    {
        internal static float DamageStretch(float duration, float budget) { return Math.Max(0.01f, duration) / Math.Max(0.01f, budget); }
        internal static bool SegmentIntersectsSphere(float ax, float ay, float az, float bx, float by, float bz, float radius)
        {
            if (radius <= 0f) return false;
            float dx = bx - ax, dy = by - ay, dz = bz - az;
            float length = dx * dx + dy * dy + dz * dz;
            float t = length > 0.000001f ? Math.Max(0f, Math.Min(1f, -(ax * dx + ay * dy + az * dz) / length)) : 0f;
            float x = ax + dx * t, y = ay + dy * t, z = az + dz * t;
            return x * x + y * y + z * z <= radius * radius;
        }
    }
}
