using System.Collections.Generic;
using RoR2;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
namespace EventHorizon
{
    internal static class HorizonCombat
    {
        private sealed class Capture
        {internal CharacterBody Body;internal CharacterMotor Motor;internal Rigidbody Rigidbody;internal VoidFiendDomain Field;internal float Entered;internal VoidFiendDomain BurnField;internal readonly HorizonBurnTimer BurnTimer=new HorizonBurnTimer();internal Vector3 BeforeMove;}
        private static readonly Dictionary<CharacterBody,Capture> targets=new Dictionary<CharacterBody,Capture>();
        private static readonly List<CharacterBody> stale=new List<CharacterBody>(),shockTargets=new List<CharacterBody>();
        private static float nextScan;
        internal static VoidFiendDomain Field(Vector3 p,TeamIndex team)
        {
            VoidFiendDomain best=null;float distance=float.MaxValue;
            foreach(VoidFiendDomain f in VoidFiendDomain.Live)if(f&&f.Contains(p,team)){
                float d=(p-f.Center).sqrMagnitude/(f.Radius*f.Radius);if(d<distance){best=f;distance=d;}
            }
            return best; // One field owns a target per step: overlaps never multiply acceleration.
        }
        private static bool Valid(CharacterBody b)
        {return b&&b.isActiveAndEnabled&&b.healthComponent&&b.healthComponent.alive&&b.teamComponent&&!b.isPlayerControlled&&!b.currentVehicle;}
        private static bool Movable(CharacterBody b)
        {return Valid(b)&&(b.bodyFlags&CharacterBody.BodyFlags.Unmovable)==0&&!b.healthComponent.IsImmuneToPhysicsForces();}
        internal static void Refresh()
        {
            if(!NetworkServer.active){Clear();return;}
            stale.Clear();
            foreach(var pair in targets){CharacterBody b=pair.Key;VoidFiendDomain f=Valid(b)?Field(b.corePosition,b.teamComponent.teamIndex):null;if(!f)stale.Add(b);else pair.Value.Field=f;}
            foreach(CharacterBody b in stale){HorizonContent.Exposure(b,0);targets.Remove(b);}
            bool any=false;foreach(VoidFiendDomain f in VoidFiendDomain.Live)if(f&&f.Active){any=true;break;}if(!any)return;
            foreach(CharacterBody b in CharacterBody.readOnlyInstancesList){
                if(!Valid(b))continue;VoidFiendDomain f=Field(b.corePosition,b.teamComponent.teamIndex);if(!f)continue;
                Capture c;if(!targets.TryGetValue(b,out c)){c=new Capture{Body=b,Motor=b.characterMotor,Rigidbody=b.GetComponent<Rigidbody>(),Field=f,Entered=Time.time};targets.Add(b,c);}
                HorizonContent.Exposure(b,HorizonSettings.Debuff.Value?HorizonRules.Stacks(Time.time-c.Entered,HorizonSettings.Exposure.Value):0);
                Incapacitate(c);
            }
        }
        private static void Incapacitate(Capture capture)
        {
            if(!HorizonSettings.Incapacitate.Value)return;
            CharacterBody body=capture.Body;
            SetStateOnHurt hurt=body.GetComponent<SetStateOnHurt>();
            // Every hostile on the stage is incapacitated, including stun-resistant bosses.
            // Use native interrupt priority and RPCs. Do not replace
            // another domain's freeze or a pending spawn/death transition with a lesser stun.
            if(!hurt||!hurt.targetStateMachine||hurt.idleStateMachine==null||
                hurt.targetStateMachine.HasPendingState()||hurt.targetStateMachine.state is EntityStates.FrozenState)return;
            bool original=hurt.canBeStunned;
            try{hurt.canBeStunned=true;DomainStun.Apply(hurt,HorizonRules.StunLease);}
            finally{hurt.canBeStunned=original;}
        }
        internal static void Tick()
        {
            if(!NetworkServer.active){Clear();return;}
            if(Time.time>=nextScan){nextScan=Time.time+HorizonRules.Scan;Refresh();}
            foreach(Capture c in targets.Values){Burn(c);if(!c.Motor)Apply(c,Time.fixedDeltaTime);}
        }
        private static void Burn(Capture c)
        {
            CharacterBody b=c.Body;VoidFiendDomain field=null;float closest=float.MaxValue;
            if(Valid(b)&&HorizonSettings.BurnMultiplier.Value>0)foreach(VoidFiendDomain f in VoidFiendDomain.Live){
                if(!f||!f.Contains(b.corePosition,b.teamComponent.teamIndex)||!f.Body)continue;
                float reach=f.BlackHoleRadius+HorizonSettings.BurnRange.Value,d=(b.corePosition-f.PullCenter).sqrMagnitude;
                if(d<=reach*reach&&d<closest){field=f;closest=d;}
            }
            if(c.BurnField!=field){c.BurnTimer.Reset();c.BurnField=field;}
            if(c.BurnTimer.Step(Time.time,field,field?field.CastId:0,HorizonSettings.BurnInterval.Value))
                DotController.InflictDot(b.gameObject,field.Body.gameObject,null,DotController.DotIndex.Burn,HorizonSettings.BurnDuration.Value,HorizonSettings.BurnMultiplier.Value);
        }
        private static bool CanMove(Capture c)
        {
            CharacterBody b=c.Body;
            if(!Movable(b)||!c.Field||!c.Field.Contains(b.corePosition,b.teamComponent.teamIndex)||!b.hasEffectiveAuthority)return false;
            if(c.Rigidbody&&((!c.Motor&&c.Rigidbody.isKinematic)||c.Rigidbody.constraints==RigidbodyConstraints.FreezeAll))return false;
            SetStateOnHurt hurt=b.GetComponent<SetStateOnHurt>();
            return !hurt||!hurt.targetStateMachine||!(hurt.targetStateMachine.state is EntityStates.FrozenState);
        }
        private static void Apply(Capture c,float dt)
        {
            CharacterBody b=c.Body;if(!CanMove(c)||(!c.Motor&&!c.Rigidbody))return;
            Vector3 velocity=c.Motor?c.Motor.velocity:c.Rigidbody.velocity;
            Vector3 delta=c.Field.PullCenter-b.corePosition;float distance=delta.magnitude;
            Vector3 inward=distance>.02f?delta/distance:Vector3.right;
            float strength=HorizonRules.Strength(distance,c.Field.Radius),boss=b.isBoss?HorizonSettings.Boss.Value:1;
            float exposure=HorizonSettings.Debuff.Value?Mathf.Clamp01((Time.time-c.Entered)/HorizonSettings.Exposure.Value):0;
            float speed=HorizonSettings.Speed.Value;
            Vector3 desired=inward*(speed*(.5f+.5f*strength)*Mathf.Clamp01(distance/5f));
            desired+=Vector3.Cross(Vector3.up,inward)*HorizonSettings.Orbit.Value*(1-Mathf.Clamp01(distance/(c.Field.Radius*.6f)));
            desired*=boss;
            desired=Vector3.ClampMagnitude(desired,speed*boss);
            desired.y=Mathf.Clamp(desired.y,-HorizonSettings.Lift.Value,HorizonSettings.Lift.Value);
            Vector3 change=Vector3.ClampMagnitude(desired-velocity,HorizonSettings.Pull.Value*strength*(1+exposure*.25f)*boss*Mathf.Min(dt,.05f));
            PhysForceInfo force=PhysForceInfo.Create();force.force=change;force.massIsOne=true;force.ignoreGroundStick=true;
            b.healthComponent.TakeDamageForce(force);
        }
        [HarmonyPatch(typeof(CharacterMotor),"PreMove")]
        private static class CapturedMotor
        {
            private static void Prefix(CharacterMotor __instance)
            {
                if(!NetworkServer.active)return;CharacterBody b=__instance.GetComponent<CharacterBody>();Capture c;
                if(b&&targets.TryGetValue(b,out c))c.BeforeMove=__instance.velocity;
            }
            private static void Postfix(CharacterMotor __instance,float deltaTime)
            {
                if(!NetworkServer.active||HorizonSettings.Pull.Value<=0)return;CharacterBody b=__instance.GetComponent<CharacterBody>();Capture c;
                if(!b||!targets.TryGetValue(b,out c)||!CanMove(c))return;
                // Suppress only a fraction of this step's native locomotion/gravity response.
                // Retain external impulses and collision resolution; never toggle motor flags or global gravity.
                float response=.03f;
                Vector3 native=__instance.velocity,delta=native-c.BeforeMove;
                native.x=c.BeforeMove.x+delta.x*response;native.z=c.BeforeMove.z+delta.z*response;
                if(HorizonSettings.Lift.Value>0)native.y=c.BeforeMove.y+delta.y*response;
                __instance.velocity=native;Apply(c,deltaTime);
            }
        }
        internal static void Shock(CharacterBody owner,Vector3 center,float radius,float strength,float coefficient)
        {
            if(!NetworkServer.active||!owner||!owner.teamComponent)return;
            shockTargets.Clear();
            foreach(CharacterBody b in CharacterBody.readOnlyInstancesList)if(Valid(b)&&TeamManager.IsTeamEnemy(owner.teamComponent.teamIndex,b.teamComponent.teamIndex))shockTargets.Add(b);
            foreach(CharacterBody b in shockTargets){
                if(!Valid(b))continue;
                if(Movable(b)){
                    Vector3 outwards=b.corePosition-center;outwards.y=0;if(outwards.sqrMagnitude<.01f)outwards=Vector3.forward;
                    PhysForceInfo force=PhysForceInfo.Create();force.force=outwards.normalized*strength*(b.isBoss?HorizonSettings.Boss.Value:1);force.massIsOne=true;force.doNotExceed=true;
                    b.healthComponent.TakeDamageForce(force);
                }
                if(coefficient>0)b.healthComponent.TakeDamage(new DamageInfo{attacker=owner.gameObject,inflictor=owner.gameObject,damage=owner.damage*coefficient,position=b.corePosition,procCoefficient=0,crit=false});
            }
            shockTargets.Clear();
        }
        internal static void Clear(){foreach(CharacterBody b in targets.Keys)HorizonContent.Exposure(b,0);targets.Clear();nextScan=0;}
    }
}
