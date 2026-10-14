using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace EventHorizon
{
    [BepInPlugin("wreckzog.eventhorizon", "Event Horizon", "1.0.1")]
    [BepInIncompatibility("local.survivordomains")]
    [BepInDependency("local.alienskies", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static BepInEx.Logging.ManualLogSource Log;
        internal static ConfigEntry<bool> SoundEnabled;
        internal static ConfigEntry<float> SoundVolume;
        private Harmony patches;
        private void Awake()
        {
            Log=Logger;
            SoundEnabled=Config.Bind("Audio","Enabled",true,"Play the opening impact and domain soundtrack (Windows audio output).");
            SoundVolume=Config.Bind("Audio","Volume",.7f,new ConfigDescription("Domain audio gain. Independent of the game's audio sliders; applies on the next cast.",new AcceptableValueRange<float>(0,1)));
            HorizonSystem.Initialize(Config);
            patches=new Harmony("wreckzog.eventhorizon");patches.PatchAll(typeof(Plugin).Assembly);
            HorizonProjectileRegistry.Initialize();
            Log.LogInfo("Event Horizon 1.0.1 ready: Void Fiend only, 22-second default, soundtrack replaces game music during the domain.");
        }
        private void Update(){HorizonSystem.Update();}
        private void FixedUpdate(){HorizonCombat.Tick();HorizonProjectiles.Tick();}
        private void OnDestroy()
        {
            HorizonSystem.Shutdown();
            HorizonMusicMix.Shutdown();
            if(patches!=null)patches.UnpatchSelf();
            HorizonProjectileRegistry.Projectiles.Clear();DomainVisual.Clear();
        }
        internal static float Clamp(float value,float min,float max){return Mathf.Clamp(value,min,max);}
    }
}
