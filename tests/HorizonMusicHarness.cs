using System;
using System.Reflection;
using System.Collections.Generic;
using EventHorizon;
namespace HarmonyLib {
 [AttributeUsage(AttributeTargets.Class)]public class HarmonyPatch:Attribute{public HarmonyPatch(){}public HarmonyPatch(Type t,string n){}public HarmonyPatch(Type t,string n,Type[] a){}}
 public static class AccessTools{public static MethodInfo Method(Type t,string n){return t.GetMethod(n);}public static FieldInfo Field(Type t,string n){return t.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static);}}
}
namespace UnityEngine {
 public class MonoBehaviour{public GameObject gameObject;}
 public class Transform{public void SetParent(Transform p,bool world){}}
 public class GameObject{public Transform transform=new Transform();public GameObject(string n){}public T AddComponent<T>()where T:MonoBehaviour,new(){return new T{gameObject=this};}}
 public static class Time{public static float time,timeScale=1;}
 public static class Mathf{public static float Max(float a,float b){return Math.Max(a,b);}}
}
namespace RoR2.ConVar {public abstract class BaseConVar{public abstract string GetString();}}
namespace RoR2 {
 public static class AudioManager {
  public static VolumeConVar cvVolumeMsx=new VolumeConVar("Volume_MSX"),Sfx=new VolumeConVar("Volume_SFX");
  public class VolumeConVar:ConVar.BaseConVar {
   string name;public VolumeConVar(string n){name=n;}
   public override string GetString(){
    var method=typeof(HorizonMusicMix).GetNestedType("PreserveMusicPreference",BindingFlags.NonPublic).GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic);
    object[] args={this,null};if(!(bool)method.Invoke(null,args))return (string)args[1];
    return AkSoundEngine.Gains[name].ToString(System.Globalization.CultureInfo.InvariantCulture);
   }
  }
 }
}
public enum AKRESULT{AK_Success,AK_Fail}
public static class AkSoundEngine {
 public static bool Ready=true,Fail;public static Dictionary<string,float> Gains=new Dictionary<string,float>{{"Volume_MSX",80},{"Volume_SFX",60}};
 public static bool IsInitialized(){return Ready;}
 public static AKRESULT SetRTPCValue(string id,float gain){
  var patch=typeof(HorizonMusicMix).GetNestedType("PreserveRequestedGain",BindingFlags.NonPublic).GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic);
  object[] args={id,gain};patch.Invoke(null,args);if(Fail)return AKRESULT.AK_Fail;Gains[id]=(float)args[1];return AKRESULT.AK_Success;
 }
}
namespace EventHorizon {
 class Setting<T>{public T Value;}
 static class Plugin{internal static Setting<bool> SoundEnabled=new Setting<bool>{Value=true};internal static Setting<float> SoundVolume=new Setting<float>{Value=.7f};internal static Logger Log=new Logger();internal static float Clamp(float x,float a,float b){return Math.Max(a,Math.Min(b,x));}}
 class Logger{public void LogWarning(string text){}}
 class HorizonWaveAsset{internal static HorizonWaveAsset Load(string name){return new HorizonWaveAsset();}}
 class HorizonWaveVoice:IDisposable{internal static bool Fail;internal bool Paused;internal HorizonWaveVoice(HorizonWaveAsset a,bool loop,float age,float gain){if(Fail)throw new Exception("Injected playback failure");}internal void Pause(bool paused){Paused=paused;}public void Dispose(){}}
}
class HorizonMusicHarness {
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
 static void Tick(object audio){Invoke(audio,"Update");}
 static void Invoke(object target,string name){target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);}
 static object Field(object target,string name){return target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);}
 static void Reset(){HorizonMusicMix.Shutdown();AkSoundEngine.Gains.Clear();AkSoundEngine.Ready=true;AkSoundEngine.Fail=false;HorizonWaveVoice.Fail=false;Plugin.SoundEnabled.Value=true;Plugin.SoundVolume.Value=.7f;UnityEngine.Time.time=0;UnityEngine.Time.timeScale=1;AkSoundEngine.SetRTPCValue("Volume_MSX",.8f);AkSoundEngine.SetRTPCValue("Volume_SFX",.6f);}
 static HorizonAudio Start(float windup=0){return HorizonAudio.Create(new UnityEngine.Transform(),0,windup,22,1);}
 static void Main(){
  Reset();var a=Start();Check(AkSoundEngine.Gains["Volume_MSX"]==0,"native music suppressed when soundtrack starts");Check(AkSoundEngine.Gains["Volume_SFX"]==.6f,"default/SFX output untouched");
  UnityEngine.Time.time=21.99f;Tick(a);Check(AkSoundEngine.Gains["Volume_MSX"]==0,"suppression lasts all 22 seconds");UnityEngine.Time.time=22;Tick(a);Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"expiry restores original output gain");Check(Field(a,"music")==null,"soundtrack stops at expiry");
  Reset();a=Start();var b=Start();a.Collapse(0);Check(AkSoundEngine.Gains["Volume_MSX"]==0,"first overlapping domain does not restore music");b.Collapse(0);Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"last overlapping domain restores music");Invoke(a,"OnDestroy");Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"duplicate cleanup harmless");
  Reset();a=Start();AkSoundEngine.SetRTPCValue("Volume_MSX",.35f);Check(AkSoundEngine.Gains["Volume_MSX"]==0,"external music gain change remains suppressed");a.Collapse(0);Check(AkSoundEngine.Gains["Volume_MSX"]==.35f,"latest external gain restored");
  Reset();a=Start();Invoke(a,"OnDisable");Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"disable/death/scene cleanup restores music");
  Reset();a=Start();HorizonMusicMix.Shutdown();Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"plugin shutdown restores music");a.Collapse(0);
  Reset();Plugin.SoundEnabled.Value=false;a=Start();Check(AkSoundEngine.Gains["Volume_MSX"]==.8f&&Field(a,"music")==null,"audio disabled leaves game music available");
  Reset();Plugin.SoundVolume.Value=0;a=Start();Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"zero soundtrack gain does not silence game music");
  Reset();a=Start();Plugin.SoundEnabled.Value=false;Tick(a);Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"disabling active soundtrack restores game music");
  Reset();HorizonWaveVoice.Fail=true;a=Start();Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"playback failure never suppresses game music");
  Reset();a=Start(1);Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"optional windup waits for soundtrack before suppressing");UnityEngine.Time.time=1;Tick(a);Check(AkSoundEngine.Gains["Volume_MSX"]==0,"reveal acquires suppression");a.Collapse(0);
  Reset();a=Start();UnityEngine.Time.timeScale=0;Tick(a);Check(AkSoundEngine.Gains["Volume_MSX"]==0&&((HorizonWaveVoice)Field(a,"music")).Paused,"pause keeps music gated and pauses soundtrack");UnityEngine.Time.timeScale=1;Tick(a);Check(!((HorizonWaveVoice)Field(a,"music")).Paused,"unpause resumes soundtrack");a.Collapse(0);
  Reset();AkSoundEngine.Ready=false;a=Start();Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"uninitialized engine is safe");AkSoundEngine.Ready=true;Tick(a);Check(AkSoundEngine.Gains["Volume_MSX"]==0,"engine becoming ready acquires gate");a.Collapse(0);
  Reset();AkSoundEngine.Fail=true;a=Start();Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"failed output write not recorded as successful mute");AkSoundEngine.Fail=false;Tick(a);Check(AkSoundEngine.Gains["Volume_MSX"]==0,"output mute retries after transient failure");a.Collapse(0);Check(AkSoundEngine.Gains["Volume_MSX"]==.8f,"retry preserves original gain");
  Reset();a=Start();Check(RoR2.AudioManager.cvVolumeMsx.GetString()=="0.8","settings getter preserves user preference while muted");Check(RoR2.AudioManager.Sfx.GetString()=="0.6","SFX preference unchanged");AkSoundEngine.SetRTPCValue("Volume_MSX",.25f);Check(RoR2.AudioManager.cvVolumeMsx.GetString()=="0.25","settings getter follows changes during domain");a.Collapse(0);Check(AkSoundEngine.Gains["Volume_MSX"]==.25f,"new user gain survives domain expiry");
  Console.WriteLine("PASS: "+checks+" production soundtrack/music-channel lifecycle assertions (modeled native APIs).");
 }
}
