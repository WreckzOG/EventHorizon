using System;
using System.Collections.Generic;
using HarmonyLib;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
namespace EventHorizon
{
    internal static class HorizonProjectiles
    {
        private sealed class Shot {internal ProjectileController Controller;internal ProjectileSimple Simple;internal Rigidbody Body;internal float Speed;internal Vector3 Applied;}
        private static readonly Dictionary<ProjectileSimple,Shot> shots=new Dictionary<ProjectileSimple,Shot>();
        private static readonly List<ProjectileSimple> stale=new List<ProjectileSimple>();
        private static readonly List<Vector3> flashes=new List<Vector3>(32);
        private static float nextScan;
        private static bool Eligible(ProjectileController p)
        {return p&&p.isActiveAndEnabled&&!p.isPrediction&&!p.netId.IsEmpty()&&p.teamFilter;}
        private static bool Supported(ProjectileController p,ProjectileSimple simple,Rigidbody rb)
        {
            if(!simple||!simple.enabled||!rb||rb.isKinematic||simple.enableVelocityOverLifetime||simple.oscillate||simple.desiredForwardSpeed<0)return false;
            foreach(MonoBehaviour b in p.GetComponents<MonoBehaviour>()){
                if(!b)continue;Type t=b.GetType();if(b is EntityStateMachine)return false;
                if(t==typeof(AkGameObj)||t==typeof(AkEvent)||t.Namespace=="UnityEngine.Networking"||t.Name=="TeamFilter"||t.Name=="NetworkIdentity")continue;
                if(t.Namespace!="RoR2.Projectile")return false;
                if(t!=typeof(ProjectileController)&&t!=typeof(ProjectileSimple)&&t!=typeof(ProjectileDamage)&&t!=typeof(ProjectileImpactExplosion)&&t.Name!="ProjectileSingleTargetImpact"&&t.Name!="ProjectileNetworkTransform")return false;
            }
            return true;
        }
        private static void Release(Shot s)
        {
            // Restore only a velocity we still own. Native kinematic/frozen bodies retain their movement ownership.
            if(!s.Controller||!s.Controller.isActiveAndEnabled||!s.Body||s.Body.isKinematic)return;
            if((s.Body.velocity-s.Applied).sqrMagnitude<.001f&&s.Body.velocity.sqrMagnitude>.001f)s.Body.velocity=s.Body.velocity.normalized*s.Speed;
        }
        internal static void Refresh()
        {
            if(!NetworkServer.active){Clear();return;}
            stale.Clear();foreach(var pair in shots){Shot s=pair.Value;if(!HorizonSettings.Projectiles.Value||!Eligible(s.Controller)||!HorizonCombat.Field(s.Controller.transform.position,s.Controller.teamFilter.teamIndex))stale.Add(pair.Key);}
            foreach(ProjectileSimple s in stale){Release(shots[s]);shots.Remove(s);}
            if(!HorizonSettings.Projectiles.Value)return;
            bool any=false;foreach(VoidFiendDomain f in VoidFiendDomain.Live)if(f&&f.Active){any=true;break;}if(!any)return;
            foreach(ProjectileController p in HorizonProjectileRegistry.Projectiles){
                if(!Eligible(p))continue;VoidFiendDomain field=HorizonCombat.Field(p.transform.position,p.teamFilter.teamIndex);if(!field)continue;
                ProjectileSimple simple=p.GetComponent<ProjectileSimple>();if(simple&&shots.ContainsKey(simple))continue;
                Rigidbody rb=p.GetComponent<Rigidbody>();
                if(Supported(p,simple,rb)){shots.Add(simple,new Shot{Controller=p,Simple=simple,Body=rb,Speed=Mathf.Max(rb.velocity.magnitude,simple.desiredForwardSpeed),Applied=rb.velocity});}
                else if(flashes.Count<8&&(p.transform.position-field.Center).sqrMagnitude<64)flashes.Add(p.transform.position); // Cosmetic approximation only.
            }
        }
        internal static void Tick()
        {
            if(!NetworkServer.active){Clear();return;}
            if(Time.time<nextScan)return;nextScan=Time.time+HorizonRules.Scan;Refresh();
            HorizonSystem.Absorb(flashes);flashes.Clear();
        }
        private static void Bend(ProjectileSimple simple)
        {
            if(!NetworkServer.active)return;Shot s;if(!shots.TryGetValue(simple,out s))return;
            if(!Eligible(s.Controller)){Release(s);shots.Remove(simple);return;}
            VoidFiendDomain f=HorizonCombat.Field(s.Controller.transform.position,s.Controller.teamFilter.teamIndex);
            if(!f){Release(s);shots.Remove(simple);return;}
            if(!s.Body||s.Body.isKinematic||s.Body.constraints==RigidbodyConstraints.FreezeAll)return;
            Vector3 p=s.Controller.transform.position,target=f.PullCenter,delta=target-p;
            float dt=Time.fixedDeltaTime;
            Vector3 next=p+s.Body.velocity*dt;
            if(DomainRules.SegmentIntersectsSphere(p.x-target.x,p.y-target.y,p.z-target.z,next.x-target.x,next.y-target.y,next.z-target.z,HorizonSettings.Absorb.Value)){
                ProjectileDamage damage=s.Controller.GetComponent<ProjectileDamage>();if(damage)damage.damage=0;
                if(flashes.Count<32)flashes.Add(p);shots.Remove(simple);NetworkServer.Destroy(s.Controller.gameObject);return;
            }
            Vector3 direction=s.Controller.transform.forward;
            if(delta.sqrMagnitude>.01f)direction=Vector3.RotateTowards(direction,delta.normalized,HorizonSettings.ProjectileTurn.Value*Mathf.Deg2Rad*dt,0);
            float previous=s.Applied.magnitude;
            float speed=Mathf.MoveTowards(previous,s.Speed*HorizonSettings.ProjectileSpeed.Value,Mathf.Max(1,s.Speed)*dt*1.8f);
            s.Body.rotation=Quaternion.LookRotation(direction);s.Applied=direction*speed;s.Body.velocity=s.Applied;
        }
        internal static void Clear(){foreach(Shot s in shots.Values)Release(s);shots.Clear();flashes.Clear();nextScan=0;}
        internal static void Yield(ProjectileController projectile)
        {
            ProjectileSimple simple=projectile?projectile.GetComponent<ProjectileSimple>():null;Shot shot;
            if(simple&&shots.TryGetValue(simple,out shot)){Release(shot);shots.Remove(simple);}
        }
        [HarmonyPatch(typeof(ProjectileSimple),"FixedUpdate")]
        private static class CurveAfterNativeUpdate {private static void Postfix(ProjectileSimple __instance){Bend(__instance);}}
        [HarmonyPatch(typeof(ProjectileController),"OnDisable")]
        private static class ForgetDisabled {private static void Prefix(ProjectileController __instance){ProjectileSimple s=__instance.GetComponent<ProjectileSimple>();if(s)shots.Remove(s);}}
    }
}
