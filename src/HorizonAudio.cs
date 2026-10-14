using System;
using UnityEngine;
namespace EventHorizon
{
    // RoR2 disables Unity Audio. Local voices never modify device or Wwise volume.
    internal sealed class HorizonAudio:MonoBehaviour
    {
        private static HorizonWaveAsset hitClip,loopClip;
        private HorizonWaveVoice hit,music;
        private float start,windup,duration;
        private bool closing,hitStarted,warned;
        internal static HorizonAudio Create(Transform parent,float age,float windup,float duration,float collapse)
        {
            GameObject obj=new GameObject("Event Horizon supplied soundtrack");obj.transform.SetParent(parent,false);
            HorizonAudio a=obj.AddComponent<HorizonAudio>();a.start=Time.time-age;a.windup=windup;a.duration=duration;
            a.hitStarted=age>.3f;a.Tick();return a; // Late snapshots do not replay the impact.
        }
        internal void Unstable(){} // Keep the supplied track through the last active second.
        internal void Collapse(float elapsed){closing=true;Stop();}
        private void Update(){Tick();}
        private void Tick()
        {
            float age=Time.time-start;
            if(closing||age>=windup+duration){Stop();return;}
            if(!Plugin.SoundEnabled.Value){hitStarted=true;Stop();return;}
            try {
                float gain=Plugin.Clamp(Plugin.SoundVolume.Value,0,1);
                if(!hitStarted){hitStarted=true;if(hitClip==null)hitClip=HorizonWaveAsset.Load("SurvivorDomains.HorizonHit");hit=new HorizonWaveVoice(hitClip,false,Mathf.Max(0,age),gain);}
                if(age>=windup&&music==null){if(loopClip==null)loopClip=HorizonWaveAsset.Load("SurvivorDomains.HorizonLoop");music=new HorizonWaveVoice(loopClip,true,age-windup,gain*.65f);}
                bool paused=Time.timeScale<=0;
                if(hit!=null)hit.Pause(paused);if(music!=null)music.Pause(paused);
            }catch(Exception e){Stop();closing=true;if(!warned){warned=true;Plugin.Log.LogWarning("Event Horizon supplied audio: "+e.Message);}}
        }
        private void Stop(){if(hit!=null){hit.Dispose();hit=null;}if(music!=null){music.Dispose();music=null;}}
        private void OnDisable(){Stop();}
        private void OnDestroy(){Stop();}
    }
}
