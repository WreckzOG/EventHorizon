using System.Collections.Generic;
using HarmonyLib;
using RoR2.Projectile;
using UnityEngine;
namespace EventHorizon
{
    internal static class HorizonProjectileRegistry
    {
        internal static readonly HashSet<ProjectileController> Projectiles=new HashSet<ProjectileController>();
        internal static void Initialize(){foreach(var p in Object.FindObjectsOfType<ProjectileController>())Projectiles.Add(p);}
        [HarmonyPatch(typeof(ProjectileController),"Awake")]
        private static class Register {private static void Postfix(ProjectileController __instance){Projectiles.Add(__instance);}}
        [HarmonyPatch(typeof(ProjectileController),"OnDestroy")]
        private static class Remove {private static void Prefix(ProjectileController __instance){Projectiles.Remove(__instance);}}
    }
}
