// Runs production HorizonSurfaces against a deterministic camera/property-block API model.
using System;
using System.Collections.Generic;
using UnityEngine;
using EventHorizon;
namespace UnityEngine {
 public class Object { public static void Destroy(Object o){} public static implicit operator bool(Object o){return o!=null;} public static T[] FindObjectsOfType<T>(){return TestScene.Meshes as T[];} }
 public class GameObject {public int layer;}
 public class Component:Object {public Transform transform;public GameObject gameObject=new GameObject();public List<Renderer> children=new List<Renderer>();public T[] GetComponentsInParent<T>(bool inactive){return new T[0];}public T GetComponentInParent<T>() where T:class{return null;}public void GetComponentsInChildren<T>(bool inactive,List<T> list){foreach(var r in children)if(r is T)list.Add((T)(object)r);}}
 public class MonoBehaviour:Component {}
 public class Shader:Object {public string name="Hopoo Games/Deferred/Standard";}
 public enum TextureFormat{RGBA32}
 public class Texture2D:Object {public string name;public Texture2D(int w,int h,TextureFormat f,bool m){}public void SetPixels(Color[] c){}public void Apply(bool m,bool r){}}
 public static class Mathf {public static float Sin(float x){return (float)Math.Sin(x);}public static float Lerp(float a,float b,float t){return a+(b-a)*t;}}
 public struct Vector4 {public float x,y,z,w;public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}public static Vector4 Lerp(Vector4 a,Vector4 b,float t){return new Vector4(Mathf.Lerp(a.x,b.x,t),Mathf.Lerp(a.y,b.y,t),Mathf.Lerp(a.z,b.z,t),Mathf.Lerp(a.w,b.w,t));}}
 public class Transform:Component {public string name="Scenery";public Transform parent;public Vector3 position;}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}public float sqrMagnitude{get{return x*x+y*y+z*z;}}}
 public struct Bounds {public Vector3 center;public Vector3 ClosestPoint(Vector3 p){return center;}}
 public struct Plane {}
 public static class GeometryUtility {public static void CalculateFrustumPlanes(Camera c,Plane[] p){}public static bool TestPlanesAABB(Plane[] p,Bounds b){return true;}}
 public static class Time {public static float unscaledTime;}
 public struct Color {public float r,g,b,a;public Color(float R,float G,float B,float A){r=R;g=G;b=B;a=A;}public static Color Lerp(Color x,Color y,float t){return new Color(x.r+(y.r-x.r)*t,x.g+(y.g-x.g)*t,x.b+(y.b-x.b)*t,x.a+(y.a-x.a)*t);}}
 public class MaterialPropertyBlock {internal Dictionary<string,object> data=new Dictionary<string,object>();public bool isEmpty{get{return data.Count==0;}}public bool HasVector(string n){return data.ContainsKey(n)&&data[n] is Vector4;}public Vector4 GetVector(string n){return (Vector4)data[n];}public void SetVector(string n,Vector4 v){data[n]=v;}public void SetTexture(string n,Texture2D v){data[n]=v;}public bool HasColor(string n){return data.ContainsKey(n)&&data[n] is Color;}public bool HasFloat(string n){return data.ContainsKey(n)&&data[n] is float;}public Color GetColor(string n){return (Color)data[n];}public float GetFloat(string n){return (float)data[n];}public void SetColor(string n,Color v){data[n]=v;}public void SetFloat(string n,float v){data[n]=v;}internal void Copy(MaterialPropertyBlock b){data.Clear();if(b!=null)foreach(var p in b.data)data.Add(p.Key,p.Value);}}
 public class Material:Object {public string name="matGeneric";public int renderQueue=2000;public Shader shader=new Shader();public Vector4 GetVector(string n){return values.GetVector(n);}public MaterialPropertyBlock values=new MaterialPropertyBlock();public bool HasProperty(string n){return values.data.ContainsKey(n);}public Color GetColor(string n){return values.GetColor(n);}public float GetFloat(string n){return values.GetFloat(n);}}
 public class Renderer:Component {public bool enabled=true,forceRenderingOff;public Bounds bounds;public Material[] sharedMaterials;public bool FailOnce;public MaterialPropertyBlock wide=new MaterialPropertyBlock();public Dictionary<int,MaterialPropertyBlock> slots=new Dictionary<int,MaterialPropertyBlock>();public void GetPropertyBlock(MaterialPropertyBlock b){b.Copy(wide);}public void GetPropertyBlock(MaterialPropertyBlock b,int slot){MaterialPropertyBlock v;slots.TryGetValue(slot,out v);b.Copy(v);}public void SetPropertyBlock(MaterialPropertyBlock b,int slot){if(FailOnce){FailOnce=false;throw new Exception("injected renderer failure");}if(b==null){slots.Remove(slot);return;}var v=new MaterialPropertyBlock();v.Copy(b);slots[slot]=v;}}
 public class LineRenderer:Renderer {}public class ParticleSystemRenderer:Renderer {}public class MeshRenderer:Renderer {}public class BillboardRenderer:Renderer {}
 public class Camera:Component {public int cullingMask=-1;public string name="Scene Camera";public bool stereoEnabled,orthographic;public bool Inside=true;public static event Action<Camera> onPreCull,onPostRender;public void Begin(){if(onPreCull!=null)onPreCull(this);}public void End(){if(onPostRender!=null)onPostRender(this);}}
 public static class TestScene {public static Renderer[] Meshes;}
}
namespace RoR2 {
 public class CharacterBody:Component {public static List<CharacterBody> readOnlyInstancesList=new List<CharacterBody>();public ModelLocator modelLocator;}
 public class ModelLocator:Component {public Transform modelTransform;}
 public interface IInteractable {}
 public class GenericPickupController:Component {}public class EffectComponent:Component {}public class ShopTerminalBehavior:Component {}public class Highlight:Component {}public class PickupDropletController:Component {}public class SceneExitController:Component {}
 public static class InstanceTracker {public static List<T> GetInstancesList<T>(){return Registry<T>.Items;}}
 public static class Registry<T>{public static List<T> Items=new List<T>();}
 public static class LayerIndex {public static Layer world=new Layer();public class Layer {public int mask=1<<11;}}
}
namespace RoR2.Projectile {public class ProjectileController:UnityEngine.Component {}}
public static class InterfaceInstanceTracker<T>{public static List<T> GetInstancesList(){return RoR2.Registry<T>.Items;}}
namespace EventHorizon {
 public class HorizonVisual:Component {public static List<HorizonVisual> Instances=new List<HorizonVisual>();public Transform transform=new Transform();public float RealmRadius=62.5f;public float SurfaceReach=62.5f;public float Weight=1;public float VisualWeight(Camera c){return Weight;}public static HorizonVisual ForCamera(Camera c){return c.Inside&&Instances.Count>0?Instances[0]:null;}}
 public class ZeroVisual:Component {}public static class HorizonProjection {public static bool Supported=true;}
 public static class HorizonProjectileRegistry {public static List<Component> Projectiles=new List<Component>();}
 public static class Plugin {public static Logger Log=new Logger();public class Logger{public int Errors;public void LogError(string s){Errors++;}}}
}
class SurfaceHarness {
 static int count;static void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;}
 static float Red(Renderer r){return r.slots.ContainsKey(0)?r.slots[0].GetColor("_Color").r:r.wide.GetColor("_Color").r;}
 static void Main(){
  var m=new Material();m.values.SetColor("_Color",new Color(.7f,.6f,.5f,.4f));m.values.SetFloat("_EmPower",3);
  var r=new MeshRenderer{sharedMaterials=new[]{m}};r.wide.SetColor("_Color",new Color(.9f,.8f,.7f,.3f));r.wide.SetFloat("unrelated",17);
  var actor=new MeshRenderer{sharedMaterials=new[]{m}};var body=new RoR2.CharacterBody();body.children.Add(actor);RoR2.CharacterBody.readOnlyInstancesList.Add(body);
  var terrainMaterial=new Material();terrainMaterial.shader.name="Hopoo Games/Deferred/Triplanar Terrain Blend Emissive";
  terrainMaterial.values.SetColor("_RColor",new Color(1,.5f,.3f,1));terrainMaterial.values.SetColor("_emColorRed",new Color(5,0,0,1));terrainMaterial.values.SetFloat("_emIntensityRed",5);terrainMaterial.values.SetFloat("_RedChannelSpecularStrength",1);terrainMaterial.values.SetFloat("_RedChannelSmoothness",1);terrainMaterial.values.SetTexture("_RedChannelTopTex",new Texture2D(1,1,TextureFormat.RGBA32,false));terrainMaterial.values.SetVector("_Albedo",new Vector4(1,1,1,.7f));
  var terrain=new MeshRenderer{sharedMaterials=new[]{terrainMaterial}};terrain.gameObject.layer=20;
  var leaf=new BillboardRenderer{sharedMaterials=new[]{m}};
  var far=new MeshRenderer{sharedMaterials=new[]{m},bounds=new Bounds{center=new Vector3(800,50,0)}};
  var fogMaterial=new Material{renderQueue=3000};fogMaterial.values.SetColor("_Color",new Color(1,1,1,.8f));
  var fog=new ParticleSystemRenderer{sharedMaterials=new[]{fogMaterial},transform=new Transform{name="Low atmospheric wisps"}};
  var effect=new ParticleSystemRenderer{sharedMaterials=new[]{fogMaterial},transform=new Transform{name="Combat sparks"}};
  var grass=new MeshRenderer{sharedMaterials=new[]{m},transform=new Transform{name="spmGPGrass_LOD0"}};
  var cable=new LineRenderer{sharedMaterials=new[]{m},transform=new Transform{name="cableLoop_end_r"}};
  var bush=new BillboardRenderer{sharedMaterials=new[]{m},transform=new Transform{name="spmBbDryBush_Billboard"}};
  var hiddenBefore=new MeshRenderer{sharedMaterials=new[]{m},transform=new Transform{name="Grass"},forceRenderingOff=true};
  var loot=new MeshRenderer{sharedMaterials=new[]{m},transform=new Transform{name="cableLoot"}};
  var lootObject=new TestInteraction();lootObject.children.Add(loot);RoR2.Registry<RoR2.IInteractable>.Items.Add(lootObject);
  actor.transform=new Transform{name="GrassGolem"};
  var owner=new HorizonVisual();HorizonVisual.Instances.Add(owner);TestScene.Meshes=new Renderer[]{grass,cable,bush,hiddenBefore,loot,r,actor,terrain,leaf,far,fog,effect};HorizonSurfaces.Initialize();var outer=new Camera();var nested=new Camera{Inside=false};
  outer.Begin();Check(grass.forceRenderingOff&&cable.forceRenderingOff&&bush.forceRenderingOff,"decorative grass, cable and billboard hidden");Check(!loot.forceRenderingOff&&!actor.forceRenderingOff,"loot and grass-named actor protected");Check(Math.Abs(Red(r)-.0088f)<.0001,"native scenery darkened");Check(Math.Abs(r.slots[0].GetColor("_Color").a-.3f)<.0001,"alpha cutout preserved");Check(r.slots[0].GetFloat("unrelated")==17,"renderer-wide unrelated properties inherited");Check(r.slots[0].GetFloat("_EmPower")==0,"emission suppressed");Check(actor.slots.Count==0,"live actor excluded");Check(m.GetColor("_Color").r==.7f,"shared material unchanged");
  Check(terrain.slots[0].GetColor("_RColor").r<.011f,"separate terrain channel blackened on non-world layer");Check(terrain.slots[0].GetFloat("_emIntensityRed")==0,"terrain channel emission suppressed");Check(terrain.slots[0].GetVector("_Albedo").w==.7f,"optimized terrain vector alpha retained");Check(Math.Abs(terrain.slots[0].GetFloat("_RedChannelSpecularStrength")-.018f)<.00001f,"restrained surface reflection");Check(terrain.slots[0].data.ContainsKey("_RedChannelTopTex"),"triplanar albedo identity replaced");Check(!terrain.slots[0].data.ContainsKey("_MainTex"),"foliage alpha texture never replaced");Check(leaf.slots.Count>0,"billboard vegetation covered");
  Check(far.slots.Count>0,"scenery 800m away transforms with enclosed camera");
  Check(fog.slots[0].GetColor("_Color").a<.1f,"environment haze recedes into violet wisps");
  Check(effect.slots.Count==0,"unclassified gameplay particles untouched");
  var commands=new UnityEngine.Rendering.CommandBuffer();HorizonSurfaces.RecordScenery(commands,outer,new Material());
  Check(commands.Drawn.Contains(r)&&commands.Drawn.Contains(terrain)&&commands.Drawn.Contains(far),"coverage includes near and 800m rigid scenery");
  Check(!commands.Drawn.Contains(actor)&&!commands.Drawn.Contains(leaf)&&!commands.Drawn.Contains(fog)&&!commands.Drawn.Contains(effect),"coverage excludes actors, billboard vegetation and all particle effects");
  outer.cullingMask=0;commands.Drawn.Clear();HorizonSurfaces.RecordScenery(commands,outer,new Material());Check(commands.Drawn.Count==0,"coverage respects actual camera layers");outer.cullingMask=-1;
  var interactable=new TestInteraction();interactable.children.Add(far);RoR2.Registry<RoR2.IInteractable>.Items.Add(interactable);commands.Drawn.Clear();HorizonSurfaces.RecordScenery(commands,outer,new Material());Check(!commands.Drawn.Contains(far),"live interactable excluded even after surface cache built");RoR2.Registry<RoR2.IInteractable>.Items.Clear();
  nested.Begin();Check(!grass.forceRenderingOff&&!cable.forceRenderingOff&&!bush.forceRenderingOff,"nested non-domain camera restores decoration");Check(Red(r)==.9f,"outside nested camera sees original");nested.End();Check(grass.forceRenderingOff&&cable.forceRenderingOff,"outer suppression resumed");Check(Math.Abs(Red(r)-.0088f)<.0001,"outer tint resumed");outer.End();Check(!grass.forceRenderingOff&&!cable.forceRenderingOff&&!bush.forceRenderingOff&&hiddenBefore.forceRenderingOff,"draw flags restored exactly, prehidden renderer stays hidden");Check(fog.slots.Count==0&&far.slots.Count==0,"distant scenery and atmospheric particles restore");Check(r.slots.Count==0&&Red(r)==.9f,"indexed absence and wide precedence restored");
  var original=new MaterialPropertyBlock();original.SetColor("_Color",new Color(.2f,.3f,.4f,.6f));original.SetFloat("unrelated",23);r.SetPropertyBlock(original,0);owner.Weight=.5f;outer.Begin();Check(Math.Abs(Red(r)-.1044f)<.0001,"fade uses original indexed color");Check(r.slots[0].GetColor("_Color").a==.6f,"indexed alpha retained");outer.End();Check(Red(r)==.2f&&r.slots[0].GetFloat("unrelated")==23,"indexed values restored exactly");
  r.FailOnce=true;outer.Begin();Check(!grass.forceRenderingOff&&!cable.forceRenderingOff,"failed scope rolls back hidden decoration");Check(Red(r)==.2f&&Plugin.Log.Errors==1,"failed apply restores snapshot");nested.Begin();nested.End();Check(Red(r)==.2f,"failed scope cannot reapply tint");outer.End();
  outer.Begin();HorizonSurfaces.ReleaseOwner(owner);Check(!grass.forceRenderingOff&&!cable.forceRenderingOff,"owner destruction restores decorations");Check(Red(r)==.2f,"owner teardown restores active draw");outer.End();
  outer.Begin();HorizonSurfaces.Shutdown();Check(!grass.forceRenderingOff&&!cable.forceRenderingOff,"shutdown restores decorations");Check(Red(r)==.2f,"shutdown restores active draw");outer.End();outer.Begin();Check(Red(r)==.2f,"callbacks unsubscribed");
  Console.WriteLine("PASS: "+count+" production surface lifecycle assertions (modeled Unity API).");
 }
}


namespace UnityEngine.Rendering {public class CommandBuffer {public List<UnityEngine.Renderer> Drawn=new List<UnityEngine.Renderer>();public void DrawRenderer(UnityEngine.Renderer r,UnityEngine.Material m,int slot,int pass){Drawn.Add(r);}}}

class TestInteraction:UnityEngine.Component,RoR2.IInteractable {}
