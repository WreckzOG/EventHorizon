// Execute the actual ZeroCombat.cs against a small deterministic model of the native APIs.
// This tests ownership/lifecycle logic; it is not a Unity, rendering or multiplayer simulation.
using System;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object x) { return x != null; } }
    public class GameObject : Object
    {
        public bool activeInHierarchy = true;
        public readonly List<Component> components = new List<Component>();
        public T Add<T>() where T : Component, new() { T c = new T(); c.gameObject = this; components.Add(c); return c; }
        public T GetComponent<T>() where T : class { foreach (Component c in components) if (c is T) return c as T; return null; }
        public T[] GetComponents<T>() where T : class { List<T> found = new List<T>(); foreach (Component c in components) if (c is T) found.Add(c as T); return found.ToArray(); }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform = new Transform();
        public T GetComponent<T>() where T : class { return gameObject.GetComponent<T>(); }
        public T[] GetComponents<T>() where T : class { return gameObject.GetComponents<T>(); }
    }
    public class MonoBehaviour : Component { public bool enabled = true; public bool isActiveAndEnabled { get { return enabled && gameObject.activeInHierarchy; } } }
    public class Transform : Object { public Vector3 position; public Vector3 forward=Vector3.forward; public T GetComponent<T>() where T : class { return null; } }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float magnitude {get{return (float)Math.Sqrt(sqrMagnitude);}}
        public Vector3 normalized {get{return magnitude>0?this/magnitude:zero;}}
        public static Vector3 up {get{return new Vector3(0,1,0);}}
        public static Vector3 right {get{return new Vector3(1,0,0);}}
        public static Vector3 forward {get{return new Vector3(0,0,1);}}
        public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
        public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}
        public static Vector3 operator /(Vector3 a,float b){return a*(1/b);}
        public static Vector3 ClampMagnitude(Vector3 a,float max){return a.magnitude>max?a.normalized*max:a;}
        public static Vector3 Cross(Vector3 a,Vector3 b){return new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);}
        public static Vector3 RotateTowards(Vector3 a,Vector3 b,float angle,float max){return (a+b*angle).normalized;}
        public static Vector3 zero { get { return new Vector3(); } }
        public float sqrMagnitude { get { return x*x + y*y + z*z; } }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); }
    }
    public struct Quaternion {public Vector3 forward;public static Quaternion LookRotation(Vector3 v){return new Quaternion{forward=v};}}
    public enum RigidbodyConstraints { None = 0, FreezeRotation = 7, FreezeAll = 63 }
    public class Rigidbody : Component { public bool isKinematic; public Vector3 velocity, angularVelocity; public RigidbodyConstraints constraints; public Quaternion rotation; }
    public class Animator : MonoBehaviour { }
    public static partial class Mathf { public const float Deg2Rad=(float)Math.PI/180;
    public static float Max(float a,float b){return Math.Max(a,b);}
    public static float Min(float a,float b){return Math.Min(a,b);}
    public static float Clamp(float a,float b,float c){return Math.Max(b,Math.Min(c,a));}
    public static float Clamp01(float a){return Clamp(a,0,1);}
    public static float MoveTowards(float a,float b,float max){return a+Math.Sign(b-a)*Math.Min(Math.Abs(b-a),max);} }
    public static class Time { public static float time, fixedDeltaTime=.02f; }
}
namespace UnityEngine.Networking
{
    public static class NetworkServer { public static bool active = true;public static int destroyed;public static void Destroy(UnityEngine.GameObject obj){destroyed++;obj.activeInHierarchy=false;} }
    public struct NetworkInstanceId { public bool IsEmpty() { return false; } }
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string name) { } }
    [AttributeUsage(AttributeTargets.Method)] public class HarmonyPriority : Attribute { public HarmonyPriority(int value) { } }
    public static class Priority { public const int Last = 0; }
    public static class AccessTools
    {
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public static FieldInfo Field(Type t, string n) { return t.GetField(n, All); }
        public static PropertyInfo Property(Type t, string n) { return t.GetProperty(n, All); }
    }
}
namespace EntityStates
{
    public enum InterruptPriority { Any, Frozen, Death }
    public class EntityState { public RoR2.EntityStateMachine outer; protected float fixedAge { get { return 1; } } }
    public class Idle : EntityState { }
    public class Uninitialized : EntityState { }
    public class Death : EntityState { }
    public class FrozenState : EntityState { public float freezeDuration; protected float duration; }
}
namespace RoR2
{
    using UnityEngine;
    using EntityStates;
    public struct PhysForceInfo {public Vector3 force;public bool massIsOne,ignoreGroundStick,doNotExceed;public static PhysForceInfo Create(){return new PhysForceInfo();}}
    public static class DotController {
        public enum DotIndex {Burn} public static int Count;public static GameObject LastVictim,LastAttacker;public static float Duration;
        public static void InflictDot(GameObject victim,GameObject attacker,object box,DotIndex index,float duration,float multiplier){if(!UnityEngine.Networking.NetworkServer.active)throw new Exception("client burn");Count++;LastVictim=victim;LastAttacker=attacker;Duration=duration;}
    }
    public class DamageInfo {public GameObject attacker,inflictor;public float damage,procCoefficient;public Vector3 position;public bool crit;}
    public static class TeamManager {public static bool IsTeamEnemy(TeamIndex a,TeamIndex b){return a!=b;}}
    public enum TeamIndex { Player, Monster }
    public class TeamFilter : MonoBehaviour { public TeamIndex teamIndex; }
    public class TeamComponent : MonoBehaviour { public TeamIndex teamIndex; }
    public partial class HealthComponent : MonoBehaviour { public bool alive=true,immune;public int hits,forces; public UnityEngine.Vector3 lastForce;
    public bool IsImmuneToPhysicsForces(){return immune;}
    public void TakeDamage(DamageInfo info){hits++;}
    public void TakeDamageForce(PhysForceInfo f){forces++;lastForce=f.force;CharacterMotor m=GetComponent<CharacterMotor>();if(m)m.velocity+=f.force;else {Rigidbody r=GetComponent<Rigidbody>();if(r)r.velocity+=f.force;}} }
    public class CharacterMotor : MonoBehaviour { public Vector3 velocity, rootMotion;public bool isGrounded=true,isFlying; }
    public class ModelLocator : MonoBehaviour { public Transform modelTransform; }
    public partial class CharacterBody : MonoBehaviour
    {
        [Flags] public enum BodyFlags { None, Unmovable }
        public static readonly List<CharacterBody> readOnlyInstancesList = new List<CharacterBody>();
        public HealthComponent healthComponent; public TeamComponent teamComponent; public CharacterMotor characterMotor;
        public bool isFlying;public float damage=12;public bool isPlayerControlled, hasEffectiveAuthority = true, isBoss;
        public Object currentVehicle; public BodyFlags bodyFlags; public ModelLocator modelLocator;
        public Vector3 corePosition; public int slowCount;
        public void AddTimedBuff(object buff, float seconds) { if (seconds <= 0 || seconds > .4f) throw new Exception("Unbounded slow lease"); slowCount++; }
    }
    public class EntityStateMachine : MonoBehaviour
    {
        private EntityState nextState;
        public EntityState state = new Idle();
        public bool HasPendingState() { return nextState != null; }
        public bool SetInterruptState(EntityState s, InterruptPriority priority)
        { if ((nextState ?? state) is Death) return false; nextState = s; return true; }
        public void SetNextStateToMain() { nextState = new Idle(); }
        public void QueueDeath() { nextState = new Death(); }
        public void Commit() { if (nextState != null) { state = nextState; nextState = null; state.outer = this; } }
    }
    public class SetStateOnHurt : MonoBehaviour { public bool canBeFrozen = true,canBeStunned=true; public int stunCalls;public float duration;public EntityStateMachine targetStateMachine; public EntityStateMachine[] idleStateMachine=new EntityStateMachine[0]; }
    public static class RoR2Content { public static class Buffs { public static readonly object Slow80 = new object(); } }
}
namespace RoR2.Projectile
{
    using UnityEngine;
    public class ProjectileController : MonoBehaviour { public bool isPrediction; public RoR2.TeamFilter teamFilter; public UnityEngine.Networking.NetworkInstanceId netId; }
    public class ProjectileSimple : MonoBehaviour { public float desiredForwardSpeed = 20, oscillateMagnitude = 2;public bool enableVelocityOverLifetime,oscillate; }
    public class ProjectileDamage : MonoBehaviour { public float damage=5; }
    public class ProjectileImpactExplosion : MonoBehaviour { }
    public class ProjectileSteerTowardTarget : MonoBehaviour { }
}
public class AkGameObj : UnityEngine.MonoBehaviour { }
public class AkEvent : UnityEngine.MonoBehaviour { }
namespace ForeignMod { public class CustomMover : UnityEngine.MonoBehaviour { } }
namespace EventHorizon
{
    using UnityEngine;
    using UnityEngine.Networking;
    using RoR2;
    using RoR2.Projectile;
    internal class Setting<T>{internal T Value;internal Setting(T x){Value=x;}}
    internal static class HorizonSettings
    {
        internal static Setting<bool> Incapacitate=new Setting<bool>(true);
        internal static Setting<float> BurnRange=new Setting<float>(12),BurnInterval=new Setting<float>(1),BurnDuration=new Setting<float>(4),BurnMultiplier=new Setting<float>(1);
        internal static Setting<float> CoreRadius=new Setting<float>(12);
        internal static Setting<bool> Debuff=new Setting<bool>(true),Projectiles=new Setting<bool>(true);
        internal static Setting<float> Exposure=new Setting<float>(4),Pull=new Setting<float>(12),Speed=new Setting<float>(4),Boss=new Setting<float>(1),Lift=new Setting<float>(3),Orbit=new Setting<float>(.5f),Absorb=new Setting<float>(4),ProjectileSpeed=new Setting<float>(.55f),ProjectileTurn=new Setting<float>(55);
    }
    internal class VoidFiendDomain:MonoBehaviour
    {
        internal static List<VoidFiendDomain> Live=new List<VoidFiendDomain>();
        internal CharacterBody Body;internal uint CastId=1;internal float BlackHoleRadius=12.5f;internal bool Active=true;internal Vector3 Center;internal float Radius=62.5f,HoleHeight=.18f;
        internal Vector3 PullCenter {get{return Center+new Vector3(0,50,18.75f);}}
        internal bool Contains(Vector3 p,TeamIndex team){return Active&&team==TeamIndex.Monster;}
    }
    internal static class HorizonContent
    {
        internal static readonly Dictionary<CharacterBody,int> ExposureByBody=new Dictionary<CharacterBody,int>();
        internal static void Exposure(CharacterBody b,int n){if(b)ExposureByBody[b]=n;}
    }
    internal static class DomainStun {internal static bool Fail;internal static void Apply(SetStateOnHurt hurt,float duration){if(Fail)throw new Exception("injected native failure");hurt.stunCalls++;hurt.duration=duration;}}
    internal static class HorizonSystem {internal static void Absorb(List<Vector3> points){} }
    internal static class ZeroCombat {internal static readonly HashSet<ProjectileController> Frozen=new HashSet<ProjectileController>();internal static bool Suspended(ProjectileController p){return Frozen.Contains(p);}}
    internal static class HorizonProjectileRegistry {internal static readonly HashSet<ProjectileController> Projectiles=new HashSet<ProjectileController>(),Frozen=new HashSet<ProjectileController>();internal static bool Suspended(ProjectileController p){return Frozen.Contains(p);}}
    class HorizonChecks
    {
        static int checks;
        static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
        static void Reset(){NetworkServer.active=true;HorizonCombat.Clear();HorizonProjectiles.Clear();VoidFiendDomain.Live.Clear();CharacterBody.readOnlyInstancesList.Clear();HorizonProjectileRegistry.Projectiles.Clear();HorizonProjectileRegistry.Frozen.Clear();ZeroCombat.Frozen.Clear();HorizonContent.ExposureByBody.Clear();Time.time=0;NetworkServer.destroyed=0;DotController.Count=0;HorizonSettings.BurnMultiplier.Value=1;HorizonSettings.Pull.Value=12;HorizonSettings.Boss.Value=1;HorizonSettings.Debuff.Value=true;DomainStun.Fail=false;}
        static VoidFiendDomain Field(){var f=new GameObject().Add<VoidFiendDomain>();VoidFiendDomain.Live.Add(f);return f;}
        static CharacterBody Body(float x,bool boss=false){var o=new GameObject();var b=o.Add<CharacterBody>();b.healthComponent=o.Add<HealthComponent>();b.teamComponent=o.Add<TeamComponent>();b.teamComponent.teamIndex=TeamIndex.Monster;b.characterMotor=o.Add<CharacterMotor>();b.corePosition=new Vector3(x,0,0);b.isBoss=boss;CharacterBody.readOnlyInstancesList.Add(b);return b;}
        static ProjectileController Shot(float x,TeamIndex team=TeamIndex.Monster){var o=new GameObject();var p=o.Add<ProjectileController>();p.teamFilter=o.Add<TeamFilter>();p.teamFilter.teamIndex=team;p.transform.position=new Vector3(x,3,0);o.Add<ProjectileSimple>();o.Add<Rigidbody>().velocity=Vector3.forward*20;HorizonProjectileRegistry.Projectiles.Add(p);return p;}
        static void Bend(ProjectileController p){typeof(HorizonProjectiles).GetMethod("Bend",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p.GetComponent<ProjectileSimple>()});}
        static void Motor(CharacterBody b){Type hook=typeof(HorizonCombat).GetNestedType("CapturedMotor",BindingFlags.NonPublic);hook.GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{b.characterMotor});hook.GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{b.characterMotor,.02f});}
        static void Main()
        {
            Reset();var stunField=Field();var caught=Body(5);var caughtHurt=caught.gameObject.Add<SetStateOnHurt>();caughtHurt.targetStateMachine=caught.gameObject.Add<EntityStateMachine>();
            HorizonCombat.Refresh();Check(caughtHurt.stunCalls==1,"enemy incapacitated");Check(caughtHurt.duration==HorizonRules.StunLease&&caughtHurt.duration<.4f,"bounded renewable native stun lease");
            Time.time=.1f;HorizonCombat.Refresh();Check(caughtHurt.stunCalls==2,"sphere incapacitation maintained");
            caught.corePosition=new Vector3(13,0,0);HorizonCombat.Refresh();Check(caughtHurt.stunCalls==3,"outside old core still incapacitated");
            caught.corePosition=new Vector3(0,11.25f,0);HorizonCombat.Refresh();Check(caughtHurt.stunCalls==4,"airborne enemy incapacitated");
            caught.corePosition=new Vector3(0,25,0);HorizonCombat.Refresh();Check(caughtHurt.stunCalls==5,"upper sphere enemy incapacitated");
            caught.corePosition=new Vector3(0,0,0);caughtHurt.canBeStunned=false;HorizonCombat.Refresh();Check(caughtHurt.stunCalls==6&&!caughtHurt.canBeStunned,"stun resistance temporarily bypassed and exactly restored");
            caughtHurt.canBeStunned=true;caughtHurt.targetStateMachine.state=new EntityStates.FrozenState();HorizonCombat.Refresh();Check(caughtHurt.stunCalls==6,"incapacitation preserves freeze");
            caughtHurt.targetStateMachine.state=new EntityStates.Idle();caughtHurt.targetStateMachine.QueueDeath();HorizonCombat.Refresh();Check(caughtHurt.stunCalls==6,"pending death not replaced");
            caughtHurt.targetStateMachine.Commit();caughtHurt.targetStateMachine.state=new EntityStates.Idle();caught.teamComponent.teamIndex=TeamIndex.Player;HorizonCombat.Refresh();Check(caughtHurt.stunCalls==6,"allies are not incapacitated");
            caught.teamComponent.teamIndex=TeamIndex.Monster;NetworkServer.active=false;HorizonCombat.Refresh();Check(caughtHurt.stunCalls==6,"clients cannot renew stuns");NetworkServer.active=true;
            stunField.Active=false;HorizonCombat.Refresh();Check(caughtHurt.stunCalls==6,"domain expiry stops stun renewal");
            Reset();Field();var b=Body(10);HorizonCombat.Refresh();Motor(b);Check(b.characterMotor.velocity.x<0,"enemy pulled inward");Check(b.characterMotor.velocity.y>0&&b.characterMotor.velocity.z>0,"grounded enemy drifts up toward physical anchor");Check(b.characterMotor.velocity.magnitude<=18*.02f+.001f,"acceleration bounded");
            float one=b.characterMotor.velocity.magnitude;b.characterMotor.velocity=Vector3.zero;Field();HorizonCombat.Refresh();Motor(b);Check(Math.Abs(one-b.characterMotor.velocity.magnitude)<.001f,"overlap does not multiply force");
            Time.time=4.1f;HorizonCombat.Refresh();Check(HorizonContent.ExposureByBody[b]==5,"corruption builds to bounded five stacks");b.corePosition=new Vector3(100,0,0);HorizonCombat.Refresh();Check(HorizonContent.ExposureByBody[b]==5,"leaving former sphere retains corruption");
            Reset();Field();HorizonSettings.Boss.Value=.18f;var normal=Body(10);var boss=Body(10,true);HorizonCombat.Refresh();Motor(normal);Motor(boss);Check(boss.characterMotor.velocity.magnitude<normal.characterMotor.velocity.magnitude*.3f,"boss reduced pull");Check(boss.characterMotor.velocity.y>0,"boss also drifts toward physical anchor");
            var friend=Body(5);friend.teamComponent.teamIndex=TeamIndex.Player;var immovable=Body(5);immovable.bodyFlags=CharacterBody.BodyFlags.Unmovable;var immune=Body(5);immune.healthComponent.immune=true;var remote=Body(5);remote.hasEffectiveAuthority=false;var player=Body(5);player.isPlayerControlled=true;
            HorizonCombat.Refresh();foreach(var t in new[]{friend,immovable,immune,remote,player}){Motor(t);Check(t.healthComponent.forces==0,"excluded target not forced");}
            var kinematic=Body(10);kinematic.gameObject.Add<Rigidbody>().isKinematic=true;HorizonCombat.Refresh();Motor(kinematic);Check(kinematic.healthComponent.forces>0,"native character motor supported despite kinematic companion rigidbody");
            var air=Body(12);air.characterMotor.isGrounded=false;HorizonCombat.Refresh();Motor(air);Check(air.characterMotor.velocity.y>0,"airborne lift");
            var frozen=Body(5);var hurt=frozen.gameObject.Add<SetStateOnHurt>();hurt.targetStateMachine=frozen.gameObject.Add<EntityStateMachine>();hurt.targetStateMachine.state=new EntityStates.FrozenState();HorizonCombat.Refresh();Motor(frozen);Check(frozen.healthComponent.forces==0,"Artificer native freeze takes priority");
            Reset();var field=Field();b=Body(12);HorizonCombat.Refresh();NetworkServer.active=false;Motor(b);Check(b.healthComponent.forces==0,"client cannot apply enemy force");HorizonCombat.Tick();Check(HorizonContent.ExposureByBody[b]==0,"disconnect clears leases");
            Reset();var whole=Field();var edge=Body(62.4f);var edgeHurt=edge.gameObject.Add<SetStateOnHurt>();edgeHurt.targetStateMachine=edge.gameObject.Add<EntityStateMachine>();edgeHurt.canBeStunned=false;
            HorizonCombat.Refresh();Check(edgeHurt.stunCalls==1&&!edgeHurt.canBeStunned,"entire sphere includes stun-resistant edge enemies");
            edge.corePosition=new Vector3(1800,400,-900);HorizonCombat.Refresh();Check(edgeHurt.stunCalls==2,"distant stage enemy remains incapacitated");
            Motor(edge);Check(edge.healthComponent.forces>0&&edge.characterMotor.velocity.x<0,"distant stage enemy drifts toward black hole");
            edge.corePosition=new Vector3(20,0,0);DomainStun.Fail=true;try{HorizonCombat.Refresh();}catch(Exception){}Check(!edgeHurt.canBeStunned,"failed native stun restores immunity flag");DomainStun.Fail=false;
            HorizonCombat.Refresh();for(int tick=0;tick<800;tick++)Motor(edge);
            Check(edge.characterMotor.velocity.magnitude<=4.001f&&edge.characterMotor.velocity.y<=3.001f,"sustained suction converges to slow bounded drift");
            Check(edge.characterMotor.velocity.y>0&&edge.characterMotor.velocity.x<0&&edge.characterMotor.velocity.z>0,"suction points to the visible elevated offset black hole");
            whole.Active=false;HorizonCombat.Refresh();int forces=edge.healthComponent.forces;Motor(edge);Check(edge.healthComponent.forces==forces,"expiry ends all extra suction");
            Reset();Field();var shot=Shot(20);var friendly=Shot(20,TeamIndex.Player);var custom=Shot(20);custom.gameObject.Add<ForeignMod.CustomMover>();var homing=Shot(20);homing.gameObject.Add<ProjectileSteerTowardTarget>();var curve=Shot(20);curve.GetComponent<ProjectileSimple>().enableVelocityOverLifetime=true;
            HorizonProjectiles.Refresh();Bend(shot);Check(shot.GetComponent<Rigidbody>().velocity.magnitude<20,"hostile standard shot slows");Check(shot.GetComponent<Rigidbody>().velocity.x<0,"hostile standard shot curves");Check(shot.GetComponent<ProjectileSimple>().desiredForwardSpeed==20,"native speed property unchanged");
            foreach(var p in new[]{friendly,custom,homing,curve}){Bend(p);Check(p.GetComponent<Rigidbody>().velocity.magnitude==20,"excluded projectile unchanged");}
            shot.transform.position=new Vector3(1500,3,0);HorizonProjectiles.Refresh();Bend(shot);Check(shot.GetComponent<Rigidbody>().velocity.magnitude<20,"distant stage projectile remains slowed");
            var captured=Shot(1);captured.transform.position=new Vector3(1,50,18.75f);HorizonProjectiles.Refresh();Bend(captured);Check(NetworkServer.destroyed==1,"center absorbs hostile projectile once");Bend(captured);Check(NetworkServer.destroyed==1,"absorption not repeated");
            var handoff=Shot(15);HorizonProjectiles.Refresh();Bend(handoff);HorizonProjectiles.Yield(handoff);Check(Math.Abs(handoff.GetComponent<Rigidbody>().velocity.magnitude-20)<.001f,"freeze handoff captures native speed, not temporary slowed speed");
            var suspended=Shot(15);suspended.GetComponent<Rigidbody>().isKinematic=true;HorizonProjectiles.Refresh();Bend(suspended);Check(suspended.GetComponent<Rigidbody>().velocity.magnitude==20,"kinematic projectile movement preserved");
            var changed=Shot(15);HorizonProjectiles.Refresh();Bend(changed);changed.teamFilter.teamIndex=TeamIndex.Player;HorizonProjectiles.Refresh();Check(Math.Abs(changed.GetComponent<Rigidbody>().velocity.magnitude-20)<.001f,"ownership change restores speed");
            var cancel=Shot(15);HorizonProjectiles.Refresh();Bend(cancel);foreach(var f in VoidFiendDomain.Live)f.Active=false;HorizonProjectiles.Refresh();Check(Math.Abs(cancel.GetComponent<Rigidbody>().velocity.magnitude-20)<.001f,"interruption restores speed");
            Reset();var owner=Body(0);owner.teamComponent.teamIndex=TeamIndex.Player;var target=Body(5);HorizonCombat.Shock(owner,Vector3.zero,62.5f,12,0);Check(target.healthComponent.hits==0,"default collapse no damage");Check(target.healthComponent.lastForce.x>0&&target.healthComponent.lastForce.y==0,"collapse outward horizontal impulse");HorizonCombat.Shock(owner,Vector3.zero,62.5f,12,2);Check(target.healthComponent.hits==1,"optional collapse one damage event per invocation");
            Check(!HorizonRules.Active(-.001f,1,12)&&!HorizonRules.Active(0,1,12)&&!HorizonRules.Active(.999f,1,12)&&HorizonRules.Active(1,1,12)&&HorizonRules.Active(12.999f,1,12)&&!HorizonRules.Active(13,1,12)&&HorizonRules.Active(0,0,12),"one-second wind-up followed by full 12-second active duration");Check(HorizonRules.Strength(2,62.5f)>HorizonRules.Strength(60,62.5f),"gravity falloff");Check(HorizonRules.Capture(0,3)<0,"capture core repels stacking");
            Check(HorizonRules.Active(0,0,22)&&HorizonRules.Active(12,0,22)&&HorizonRules.Active(21.999f,0,22)&&!HorizonRules.Active(22,0,22),"instant full 22-second active lifetime");
            Reset();var burnField=Field();burnField.Body=Body(0);burnField.Body.teamComponent.teamIndex=TeamIndex.Player;
            var victim=Body(0);victim.corePosition=burnField.PullCenter+new Vector3(24.5f,0,0);
            HorizonCombat.Tick();Check(DotController.Count==0,"no immediate burn on entry");
            Time.time=.99f;HorizonCombat.Tick();Check(DotController.Count==0,"full dwell interval required");
            Time.time=1;HorizonCombat.Tick();Check(DotController.Count==1&&DotController.LastVictim==victim.gameObject&&DotController.LastAttacker==burnField.Body.gameObject&&DotController.Duration==4,"native burn at boundary attributed to caster");
            Time.time=2;HorizonCombat.Tick();Check(DotController.Count==2,"repeated dwell adds a new stack");
            victim.corePosition+=new Vector3(.1f,0,0);Time.time=2.5f;HorizonCombat.Tick();Time.time=4;HorizonCombat.Tick();Check(DotController.Count==2,"outside burn zone stops new stacks");
            victim.corePosition=burnField.PullCenter;Time.time=4.1f;HorizonCombat.Tick();Time.time=4.9f;HorizonCombat.Tick();Check(DotController.Count==2,"reentry does not inherit partial dwell");
            Time.time=5.1f;HorizonCombat.Tick();Check(DotController.Count==3,"reentry full interval burns");
            var overlapping=Field();overlapping.Body=burnField.Body;Time.time=6.1f;HorizonCombat.Tick();Check(DotController.Count==4,"overlapping black holes do not multiply burn cadence");
            burnField.CastId++;Time.time=6.2f;HorizonCombat.Tick();Time.time=7.1f;HorizonCombat.Tick();Check(DotController.Count==4,"restart resets dwell on same domain object");
            Time.time=7.2f;HorizonCombat.Tick();Check(DotController.Count==5,"new cast burns after full interval");
            HorizonSettings.BurnMultiplier.Value=0;Time.time=8.2f;HorizonCombat.Tick();Check(DotController.Count==5,"disabled burning does not inflict zero damage dots");
            HorizonSettings.BurnMultiplier.Value=1;Time.time=9;HorizonCombat.Tick();NetworkServer.active=false;Time.time=10;HorizonCombat.Tick();Check(DotController.Count==5,"clients never create damage");
            NetworkServer.active=true;victim.teamComponent.teamIndex=TeamIndex.Player;Time.time=11;HorizonCombat.Tick();Time.time=12;HorizonCombat.Tick();Check(DotController.Count==5,"allies never burn");
            victim.teamComponent.teamIndex=TeamIndex.Monster;Time.time=13;HorizonCombat.Tick();victim.healthComponent.alive=false;Time.time=14;HorizonCombat.Tick();Check(DotController.Count==5,"dead targets stop accruing burns");
            victim.healthComponent.alive=true;Time.time=15;HorizonCombat.Tick();burnField.Active=overlapping.Active=false;Time.time=16;HorizonCombat.Tick();Check(DotController.Count==5,"domain expiry stops new stacks");
            var timer=new HorizonBurnTimer();timer.Step(0,true,1,1);Check(timer.Step(10,true,1,1)&&!timer.Step(10,true,1,1),"hitch creates one stack, not a catch-up burst");
            Console.WriteLine("PASS: "+checks+" Event Horizon behavior assertions (production combat/projectile code, native API model).");
        }
    }
}

namespace EventHorizon { internal static class SupremacyProjectiles { internal static void Yield(RoR2.Projectile.ProjectileController p) {} } }
