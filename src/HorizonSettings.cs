using BepInEx.Configuration;
using UnityEngine;
namespace EventHorizon
{
    internal static class HorizonSettings
    {
        internal static ConfigEntry<float> Radius, Duration, Windup, Opening, Collapse, Charge, Pull, Lift, Orbit, Speed, Boss,
            MoveBonus, FallSpeed, Slow, AttackSlow, Exposure, ProjectileSpeed, ProjectileTurn, Absorb, Knockback, Damage,
            BurnRange, BurnInterval, BurnDuration, BurnMultiplier, Intensity, Lensing, HoleSize, HoleHeight, Dust, CoreRadius, VisualRadius, VisualHeight, VisualX, VisualZ, DiskPitch, DiskYaw, DiskRoll;
        internal static ConfigEntry<bool> Projectiles, Debuff, Incapacitate;
        internal static ConfigEntry<int> Quality;
        internal static ConfigEntry<KeyCode> Key;
        internal static void Bind(ConfigFile c)
        {
            Radius=F(c,"Radius",62.5f,10,150,"Legacy influence scale for pull falloff and orbit; the domain covers the entire current stage.");
            Duration=F(c,"Duration",22,1,60,"Full active seconds after the wind-up, including the rapid visual transition.");
            Windup=F(c,"WindupDuration",0,0,3,"Seconds before the domain reveals and its gameplay begins. The opening hit plays immediately.");
            Opening=F(c,"OpeningDuration",.3f,.05f,2,"Rapid map-wide visual transition in seconds. Begins after WindupDuration.");
            Collapse=F(c,"CollapseDuration",1,.5f,8,"Post-expiry visual dissolution. Instability begins in the last active second.");
            Charge=F(c,"DamageToCharge",100,1,10000,"Level-scaled base damage multiples; charge persists on the master.");
            Pull=F(c,"PullAcceleration",12,0,60,"Bounded acceleration toward the physical black hole, meters/second squared.");
            Lift=F(c,"VerticalLift",3,0,12,"Maximum vertical drift toward the physical black hole; native collisions retained.");
            Orbit=F(c,"OrbitalSpeed",.5f,0,10,"Subtle tangential drift near the physical singularity.");
            Speed=F(c,"MaximumPullSpeed",4,1,20,"Slow capture speed in meters/second; external impulses are not abruptly clamped.");
            Boss=F(c,"BossPullMultiplier",1,0,1,"Boss pull multiplier; native immovable/force immunity flags remain in effect.");
            CoreRadius=F(c,"CoreIncapacitationRadius",12,1,30,"Legacy setting retained; incapacitation now covers every hostile in the current stage.");
            Incapacitate=c.Bind("VoidFiend","IncapacitateCapturedEnemies",true,"Incapacitate every hostile throughout the current stage, including stun-resistant bosses. Short native stun leases expire after release; existing freezes and death transitions take priority.");
            BurnRange=F(c,"BlackHoleBurnRange",12,0,80,"Extra meters outside the physical black hole surface that inflict stacking native fire damage. Server authoritative.");
            BurnInterval=F(c,"BlackHoleBurnInterval",1,.25f,10,"Continuous seconds near the black hole per new burn stack. Leaving resets the partial interval.");
            BurnDuration=F(c,"BlackHoleBurnDuration",4,.5f,12,"Seconds each native burn stack lasts, including after leaving the zone or domain expiry.");
            BurnMultiplier=F(c,"BlackHoleBurnDamageMultiplier",1,0,5,"Native Burn damage multiplier per stack (1 = native Burn strength, including native damage caps). Zero disables new burns.");
            MoveBonus=F(c,"MovementSpeedBonus",.2f,0,2,"Singularity Attunement movement bonus.");
            FallSpeed=F(c,"MaximumFallSpeed",12,2,60,"Attuned caster fall-speed cap; leaves corruption and global gravity untouched.");
            Slow=F(c,"CorruptionMovementReduction",.2f,0,.6f,"Maximum movement reduction at full exposure.");
            AttackSlow=F(c,"CorruptionAttackReduction",.15f,0,.5f,"Maximum attack-speed reduction at full exposure.");
            Exposure=F(c,"CorruptionBuildSeconds",4,.5f,15,"Time to build five exposure stacks; removed on exit.");
            ProjectileSpeed=F(c,"ProjectileSpeedMultiplier",.55f,.1f,1,"Supported hostile shots slow gradually; native speed fields remain unchanged.");
            ProjectileTurn=F(c,"ProjectileTurnDegrees",55,0,180,"Maximum degrees/second steering toward the central capture point.");
            Absorb=F(c,"ProjectileAbsorptionRadius",4,1,12,"Core capture radius; bounded violet absorption bursts.");
            Knockback=F(c,"CollapseKnockback",12,0,30,"Single natural-expiry outward impulse; bosses reduced, no vertical launch.");
            Damage=F(c,"CollapseDamageCoefficient",0,0,5,"Optional single burst. Default 0, set 2 for 200%; independent of the near-core burn.");
            Intensity=F(c,"EnvironmentalIntensity",1,0,1.5f,"Local cosmic projection strength; no global sky or fog writes.");
            Lensing=F(c,"GravitationalLensing",.8f,0,1.5f,"Legacy analytic lensing strength (retained for config compatibility; physical rebuild uses a local photon glow).");
            HoleSize=F(c,"BlackHoleRadiusFraction",.12f,.07f,.24f,"Legacy capture-visual fraction retained for gameplay/network compatibility. Use VisualBlackHoleRadius for the physical mesh.");
            HoleHeight=F(c,"BlackHoleHeightFraction",.18f,.12f,.85f,"Ground-to-core gameplay capture altitude. Independent of the physical sky object.");
            VisualRadius=F(c,"VisualBlackHoleRadius",12.5f,4,30,"Physical visual sphere radius in meters; does not change gameplay capture radius. Host synchronized.");
            VisualHeight=F(c,"VisualBlackHoleHeight",50,20,150,"Fixed visual height above cast center; clamped above the disk's lower extent. Host synchronized.");
            VisualX=F(c,"VisualBlackHoleOffsetX",0,-80,80,"Fixed world X offset from cast center, in meters. Never follows camera direction.");
            VisualZ=F(c,"VisualBlackHoleOffsetZ",18.75f,-80,80,"Fixed world Z offset from cast center, in meters.");
            DiskPitch=F(c,"AccretionDiskPitch",-35,-180,180,"Fixed world-space disk pitch in degrees.");
            DiskYaw=F(c,"AccretionDiskYaw",0,-180,180,"Fixed world-space disk yaw in degrees.");
            DiskRoll=F(c,"AccretionDiskRoll",18,-180,180,"Fixed world-space disk roll in degrees.");
            Dust=F(c,"CelestialDebrisDensity",.3f,0,1,"Batched local debris/trails; procedural distant stars remain available.");
            Quality=c.Bind("VoidFiend","VisualQuality",1,new ConfigDescription("0 low, 1 balanced, 2 high: nebula octaves, stellar layers and debris count.",new AcceptableValueRange<int>(0,2)));
            Projectiles=c.Bind("VoidFiend","DistortHostileProjectiles",true,"Conservative native ProjectileSimple support. Unsupported movers get cosmetic capture traces only.");
            Debuff=c.Bind("VoidFiend","VoidCorruption",true,"Server-authoritative stacking utility debuff, no damage over time.");
            Key=c.Bind("VoidFiend","ActivationKey",KeyCode.T,"Additional charged domain; all normal skills remain available.");

        }
        private static ConfigEntry<float> F(ConfigFile c,string key,float value,float min,float max,string description)
        {return c.Bind("VoidFiend",key,value,new ConfigDescription(description,new AcceptableValueRange<float>(min,max)));}
    }
}
