using System;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
namespace EventHorizon
{
    internal static class HorizonSystem
    {
        private const short RequestId = 28240, StateId = 28241, AbsorbId = 28242;
        private static bool registered;
        private static NetworkClient client;
        internal static void Initialize(ConfigFile config)
        {
            RoR2.ContentManagement.ContentManager.collectContentPackProviders += HorizonContent.Register;
            HorizonSettings.Bind(config);HorizonSurfaces.Initialize(); CharacterBody.onBodyStartGlobal += Attach;
            GlobalEventManager.onServerDamageDealt += Damage;
        }
        private static void Attach(CharacterBody body)
        {
            if (body && body.name.StartsWith("VoidSurvivorBody", StringComparison.Ordinal) && !body.GetComponent<VoidFiendDomain>())
                body.gameObject.AddComponent<VoidFiendDomain>();
        }
        private static void Damage(DamageReport report)
        {
            if (!NetworkServer.active || report == null || !report.attackerBody || report.damageInfo == null || report.damageInfo.rejected ||
                !TeamManager.IsTeamEnemy(report.attackerTeamIndex, report.victimTeamIndex)) return;
            VoidFiendDomain domain = report.attackerBody.GetComponent<VoidFiendDomain>();
            if (domain) domain.AddCharge(Mathf.Min(report.damageDealt, report.combinedHealthBeforeDamage));
        }
        internal static void Update()
        {
            if (NetworkServer.active && !registered) { NetworkServer.RegisterHandler(RequestId, ReceiveRequest); registered = true; }
            if (!NetworkServer.active) registered = false;
            NetworkClient next = NetworkManager.singleton ? NetworkManager.singleton.client : null;
            if (next == client) return;
            if (client != null) { client.UnregisterHandler(StateId); client.UnregisterHandler(AbsorbId); }
            client = next; if (client != null) { client.RegisterHandler(StateId, ReceiveState); client.RegisterHandler(AbsorbId, ReceiveAbsorb); }
        }
        internal static void Request(CharacterBody body)
        {
            if (NetworkServer.active) { body.GetComponent<VoidFiendDomain>().ActivateServer(); return; }
            if (client != null) client.Send(RequestId, new DomainMessage { Body = body.netId });
        }
        private static void ReceiveRequest(NetworkMessage message)
        {
            DomainMessage request = message.ReadMessage<DomainMessage>();
            foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                if (!user || user.connectionToClient != message.conn || !user.master) continue;
                CharacterBody body = user.master.GetBody();
                if (!body || body.netId != request.Body) continue;
                VoidFiendDomain domain = body.GetComponent<VoidFiendDomain>();
                if (domain) domain.ActivateServer(); return;
            }
        }
        private static void ReceiveState(NetworkMessage message)
        {
            HorizonState data = message.ReadMessage<HorizonState>();
            GameObject obj = ClientScene.FindLocalObject(data.Cast.Body); if (!obj) return;
            Attach(obj.GetComponent<CharacterBody>());
            VoidFiendDomain domain = obj.GetComponent<VoidFiendDomain>(); if (domain) domain.Show(data);
        }
        internal static void Broadcast(VoidFiendDomain owner, HorizonState data)
        { NetworkServer.SendToAll(StateId, data); owner.Show(data); }
        internal static void Absorb(System.Collections.Generic.List<Vector3> points)
        { if(points.Count>0)NetworkServer.SendToAll(AbsorbId,new ShatterMessage{Points=points.ToArray()}); }
        private static void ReceiveAbsorb(NetworkMessage message)
        {
            foreach(Vector3 point in message.ReadMessage<ShatterMessage>().Points)
                foreach(HorizonVisual v in HorizonVisual.Instances)
                    if(v){v.Absorb(point);break;}
        }
        internal static void Shutdown()
        {
            RoR2.ContentManagement.ContentManager.collectContentPackProviders -= HorizonContent.Register;
            CharacterBody.onBodyStartGlobal -= Attach; GlobalEventManager.onServerDamageDealt -= Damage;
            foreach (VoidFiendDomain domain in UnityEngine.Object.FindObjectsOfType<VoidFiendDomain>()) UnityEngine.Object.DestroyImmediate(domain);
            HorizonCombat.Clear(); HorizonProjectiles.Clear();
            foreach(HorizonVisual v in HorizonVisual.Instances.ToArray())if(v)UnityEngine.Object.DestroyImmediate(v.gameObject);
            HorizonProjection.Release();HorizonSurfaces.Shutdown();
            if (registered) NetworkServer.UnregisterHandler(RequestId); registered = false;
            if (client != null) { client.UnregisterHandler(StateId); client.UnregisterHandler(AbsorbId); } client = null;
        }
    }
    public sealed class HorizonState : MessageBase
    {
        public DomainMessage Cast = new DomainMessage();
        public float Charge, MoveBonus, ImpactAge, FallSpeed, HoleSize, HoleHeight, Slow, AttackSlow;
        public bool Shattered;
        public float VisualRadius;public Vector3 VisualOffset,DiskAngles;
        public override void Serialize(NetworkWriter writer) { Cast.Serialize(writer); writer.Write(Charge); writer.Write(MoveBonus); writer.Write(ImpactAge); writer.Write(Shattered); writer.Write(FallSpeed); writer.Write(HoleSize); writer.Write(HoleHeight); writer.Write(Slow); writer.Write(AttackSlow);writer.Write(VisualRadius);writer.Write(VisualOffset);writer.Write(DiskAngles); }
        public override void Deserialize(NetworkReader reader) { Cast.Deserialize(reader); Charge = reader.ReadSingle(); MoveBonus = reader.ReadSingle(); ImpactAge = reader.ReadSingle(); Shattered = reader.ReadBoolean(); FallSpeed=reader.ReadSingle(); HoleSize=reader.ReadSingle(); HoleHeight=reader.ReadSingle(); Slow=reader.ReadSingle(); AttackSlow=reader.ReadSingle();VisualRadius=reader.ReadSingle();VisualOffset=reader.ReadVector3();DiskAngles=reader.ReadVector3(); }
    }
    public sealed class HorizonCharge : MonoBehaviour { internal float Fraction; }
}
