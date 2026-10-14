using HarmonyLib;
using RoR2;
using UnityEngine.Rendering.PostProcessing;
using System;
using System.Reflection;
namespace EventHorizon
{
    // Alien Skies writes fog directly into this pass, ignoring blended fogIntensity.
    // Own only an enclosed Event Horizon camera's pass, leaving profiles/config intact.
    [HarmonyPatch(typeof(RampFogRenderer),"Render")]
    internal static class HorizonFog
    {
        internal static bool OwnsCamera(PostProcessRenderContext context)
        {
            var camera=context.camera;
            if(!camera||camera.name!="Scene Camera"||camera.stereoEnabled||camera.orthographic)return false;
            var owner=HorizonVisual.ForCamera(camera);
            return owner&&owner.VisualWeight(camera)>.001f&&HorizonProjection.Supported;
        }
        [HarmonyPriority(Priority.First)]
        [HarmonyBefore("local.alienskies.fog")]
        private static bool Prefix(PostProcessRenderContext context)
        {
            if(!OwnsCamera(context))return true;
            // A skipped post-process must still write its destination. Returning false
            // prevents native fog. The optional bridge below handles Harmony versions
            // that still invoke other prefixes after a prefix has returned false.
            context.command.BlitFullscreenTriangle(context.source,context.destination);
            return false;
        }
    }
    [HarmonyPatch]
    internal static class HorizonAlienFog
    {
        private static bool Prepare(){return TargetMethod()!=null;}
        private static MethodBase TargetMethod()
        {
            Type type=AccessTools.TypeByName("AlienSkies.Plugin");
            return type==null?null:AccessTools.Method(type,"FogActive",new[]{typeof(PostProcessRenderContext)});
        }
        private static void Postfix(PostProcessRenderContext context,ref bool __result)
        {if(__result&&HorizonFog.OwnsCamera(context))__result=false;}
    }
}
