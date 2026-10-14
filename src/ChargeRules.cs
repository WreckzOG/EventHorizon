using System;

namespace EventHorizon
{
    internal static class ChargeRules
    {
        internal static float Add(float charge, float dealt, float baseDamage, float multiplier)
        {
            if (float.IsNaN(dealt) || float.IsInfinity(dealt) || dealt <= 0f || baseDamage <= 0f || multiplier <= 0f) return charge;
            float next = charge + dealt / (baseDamage * multiplier);
            return next >= 0.99999f ? 1f : next;
        }
    }
}
