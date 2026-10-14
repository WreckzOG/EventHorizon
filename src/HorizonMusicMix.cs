using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using RoR2;

namespace EventHorizon
{
    // Temporarily gate the native music RTPC. Its convar getter reports the user's
    // requested value while gated, so opening/saving settings cannot persist zero.
    internal static class HorizonMusicMix
    {
        private const string MusicRtpc="Volume_MSX";
        private static readonly FieldInfo musicField=AccessTools.Field(typeof(AudioManager),"cvVolumeMsx");
        private static readonly HashSet<object> owners=new HashSet<object>();
        private static bool writing,muted;
        private static float requested;
        private static RoR2.ConVar.BaseConVar MusicVolume {get{return musicField==null?null:musicField.GetValue(null) as RoR2.ConVar.BaseConVar;}}
        internal static void Acquire(object owner){owners.Add(owner);Refresh();}
        internal static void Refresh()
        {
            if(owners.Count==0||muted||!AkSoundEngine.IsInitialized())return;
            var volume=MusicVolume;float gain;
            if(volume==null||!float.TryParse(volume.GetString(),NumberStyles.Float,CultureInfo.InvariantCulture,out gain))return;
            requested=gain;
            muted=Write(0);
        }
        internal static void Release(object owner)
        {if(owners.Remove(owner)&&owners.Count==0)Restore();}
        private static bool Write(float gain)
        {
            if(!AkSoundEngine.IsInitialized())return false;
            writing=true;
            try{return AkSoundEngine.SetRTPCValue(MusicRtpc,gain)==AKRESULT.AK_Success;}
            finally{writing=false;}
        }
        private static void Restore()
        {if(muted){Write(requested);muted=false;}}
        internal static void Shutdown(){owners.Clear();Restore();}
        [HarmonyPatch(typeof(AkSoundEngine),"SetRTPCValue",new Type[]{typeof(string),typeof(float)})]
        private static class PreserveRequestedGain
        {
            private static void Prefix(string __0,ref float __1)
            {if(!writing&&muted&&__0==MusicRtpc){requested=__1;__1=0;}}
        }
        [HarmonyPatch]
        private static class PreserveMusicPreference
        {
            private static MethodBase TargetMethod()
            {return AccessTools.Method(typeof(AudioManager).GetNestedType("VolumeConVar",BindingFlags.Public|BindingFlags.NonPublic),"GetString");}
            private static bool Prefix(object __instance,ref string __result)
            {
                if(!muted||!ReferenceEquals(__instance,MusicVolume))return true;
                __result=requested.ToString(CultureInfo.InvariantCulture);return false;
            }
        }
    }
}
