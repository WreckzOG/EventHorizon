using System;
using UnityEngine;
using UnityEngine.Networking;
namespace EventHorizon {
    public sealed class DomainMessage : MessageBase
    {
        public NetworkInstanceId Body;
        public Vector3 Center;
        public float Radius, Duration;
        public float Freeze, Expansion, AttackMultiplier, MoveMultiplier;
        public uint CastId;
        public float Age, EnemyMove, EnemyAttack, TickInterval;
        public bool Ended, DestroyProjectiles;
        public float Collapse;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(Body); writer.Write(Center); writer.Write(Radius); writer.Write(Duration);
            writer.Write(Freeze); writer.Write(Expansion); writer.Write(AttackMultiplier); writer.Write(MoveMultiplier); writer.Write(CastId);
            writer.Write(Age); writer.Write(EnemyMove); writer.Write(EnemyAttack); writer.Write(TickInterval); writer.Write(Ended); writer.Write(DestroyProjectiles);
            writer.Write(Collapse);
        }
        public override void Deserialize(NetworkReader reader)
        {
            Body = reader.ReadNetworkId(); Center = reader.ReadVector3(); Radius = reader.ReadSingle(); Duration = reader.ReadSingle();
            Freeze = reader.ReadSingle(); Expansion = reader.ReadSingle(); AttackMultiplier = reader.ReadSingle(); MoveMultiplier = reader.ReadSingle(); CastId = reader.ReadUInt32();
            Age = reader.ReadSingle(); EnemyMove = reader.ReadSingle(); EnemyAttack = reader.ReadSingle(); TickInterval = reader.ReadSingle(); Ended = reader.ReadBoolean(); DestroyProjectiles = reader.ReadBoolean();
            Collapse = reader.ReadSingle();
        }
    }

    public sealed class ShatterMessage : MessageBase
    {
        public Vector3[] Points = new Vector3[0];
        public override void Serialize(NetworkWriter writer) { writer.Write((byte)Math.Min(Points.Length, 32)); for (int i = 0; i < Points.Length && i < 32; i++) writer.Write(Points[i]); }
        public override void Deserialize(NetworkReader reader) { int count = reader.ReadByte(); if (count > 32) throw new System.IO.InvalidDataException("Too many domain shatters"); Points = new Vector3[count]; for (int i = 0; i < count; i++) Points[i] = reader.ReadVector3(); }
    }

}
