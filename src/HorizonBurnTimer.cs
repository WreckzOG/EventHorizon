using System;
namespace EventHorizon
{
    // One dwell timer per victim. No queued catch-up burst after a hitch; overlapping fields
    // choose one caster, while native DotController owns stack damage, visuals and expiry.
    internal sealed class HorizonBurnTimer
    {
        private float next=-1;private uint cast;
        internal void Reset(){next=-1;cast=0;}
        internal bool Step(float now,bool inside,uint id,float interval)
        {
            if(!inside||float.IsNaN(now)||float.IsInfinity(now)){Reset();return false;}
            interval=Math.Max(.25f,interval);
            if(next<0||cast!=id){cast=id;next=now+interval;return false;}
            if(now+.00001f<next)return false;
            next=now+interval;return true;
        }
    }
}
