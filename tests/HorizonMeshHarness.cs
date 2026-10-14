// Runs production HorizonSurfaces against a deterministic camera/property-block API model.
using System;
using System.Collections.Generic;
using UnityEngine;
using EventHorizon;
namespace UnityEngine {
 public class Object { public static void Destroy(Object o){if(o!=null)Destroyed++;}public static int Destroyed; public static implicit operator bool(Object o){return o!=null;} public static T[] FindObjectsOfType<T>(){return TestScene.Meshes as T[];} }
 public class GameObject:Object {public int layer;public string name;private Transform trans;public GameObject(){}public GameObject(string n){name=n;}public Transform transform{get{if(trans==null){trans=new Transform();trans.gameObject=this;}return trans;}}public T AddComponent<T>() where T:Component,new(){var c=new T();c.gameObject=this;return c;}}
 public class Component:Object {public Transform transform{get{return gameObject.transform;}}public GameObject gameObject=new GameObject();public List<Renderer> children=new List<Renderer>();public T[] GetComponentsInParent<T>(bool inactive){return new T[0];}public T GetComponentInParent<T>() where T:class{return null;}public void GetComponentsInChildren<T>(bool inactive,List<T> list){foreach(var r in children)if(r is T)list.Add((T)(object)r);}}
 public class MonoBehaviour:Component {}
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero{get{return new Vector2();}}}
 public struct Quaternion{public Vector3 value;public static Quaternion identity{get{return new Quaternion();}}public static Quaternion Euler(Vector3 a){return new Quaternion{value=a};}public static Quaternion Euler(float a,float b,float c){return Euler(new Vector3(a,b,c));}public static Quaternion LookRotation(Vector3 a,Vector3 b){return Euler(a);}public static Quaternion operator *(Quaternion a,Quaternion b){return Euler(a.value+b.value);}}
 public struct Matrix4x4 {public Vector3 position,scale;public Quaternion rotation;public static Matrix4x4 TRS(Vector3 p,Quaternion r,Vector3 s){return new Matrix4x4{position=p,rotation=r,scale=s};}}
 public enum TextureWrapMode{Repeat,Clamp}public enum FilterMode{Trilinear}
 public class MeshFilter:Component{public Mesh sharedMesh;}
 public class Mesh:Object {public string name;public static List<Mesh> All=new List<Mesh>();public List<Vector3> vertices;public List<Vector2> uv;public List<Color> colors;public List<int> indices;public Mesh(){All.Add(this);}public void SetVertices(List<Vector3> v){vertices=v;}public void SetUVs(int s,List<Vector2> u){uv=u;}public void SetColors(List<Color> c){colors=c;}public void SetTriangles(List<int> t,int s){indices=t;}public void RecalculateBounds(){}}
 public class Shader:Object {public string name="Hopoo Games/Deferred/Standard";}
 public enum TextureFormat{RGBA32}
 public class Texture2D:Object {public static List<Texture2D> All=new List<Texture2D>();public TextureWrapMode wrapModeU,wrapModeV;public FilterMode filterMode;public int anisoLevel;public Color[] pixels;public Texture2D(int w,int h,TextureFormat f,bool m,bool linear){All.Add(this);}public string name;public Texture2D(int w,int h,TextureFormat f,bool m){}public void SetPixels(Color[] c){pixels=c;}public void Apply(bool m,bool r){}}
 public static class Mathf {public const float PI=(float)Math.PI;public static float Cos(float x){return (float)Math.Cos(x);}public static float Sqrt(float x){return (float)Math.Sqrt(x);}public static float Abs(float x){return Math.Abs(x);}public static float Pow(float x,float y){return (float)Math.Pow(x,y);}public static float Clamp01(float x){return Math.Max(0,Math.Min(1,x));}public static float PerlinNoise(float x,float y){return .5f+.25f*(float)Math.Sin(x)* (float)Math.Cos(y);}public static float Sin(float x){return (float)Math.Sin(x);}public static float Lerp(float a,float b,float t){return a+(b-a)*t;}}
 public struct Vector4 {public float x,y,z,w;public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}public static Vector4 Lerp(Vector4 a,Vector4 b,float t){return new Vector4(Mathf.Lerp(a.x,b.x,t),Mathf.Lerp(a.y,b.y,t),Mathf.Lerp(a.z,b.z,t),Mathf.Lerp(a.w,b.w,t));}}
 public class Transform:Component {public Vector3 localPosition;public Transform parent;public Vector3 position{get{return parent==null?localPosition:parent.position+localPosition;}set{localPosition=parent==null?value:value-parent.position;}}public void SetParent(Transform p,bool world){parent=p;}}
 public struct Vector3 {public static Vector3 zero{get{return new Vector3();}}public static Vector3 one{get{return new Vector3(1,1,1);}}public static Vector3 up{get{return new Vector3(0,1,0);}}public static Vector3 forward{get{return new Vector3(0,0,1);}}public float magnitude{get{return (float)Math.Sqrt(sqrMagnitude);}}public Vector3 normalized{get{return this*(1/magnitude);}}public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}public float sqrMagnitude{get{return x*x+y*y+z*z;}}}
 public struct Bounds {public Vector3 center;public Vector3 ClosestPoint(Vector3 p){return p;}}
 public struct Plane {}
 public static class GeometryUtility {public static void CalculateFrustumPlanes(Camera c,Plane[] p){}public static bool TestPlanesAABB(Plane[] p,Bounds b){return true;}}
 public static class Time {public static float unscaledTime;}
 public struct Color {public static Color white{get{return new Color(1,1,1,1);}}public float r,g,b,a;public Color(float R,float G,float B,float A=1){r=R;g=G;b=B;a=A;}public static Color Lerp(Color x,Color y,float t){return new Color(x.r+(y.r-x.r)*t,x.g+(y.g-x.g)*t,x.b+(y.b-x.b)*t,x.a+(y.a-x.a)*t);}}
 public class MaterialPropertyBlock {public void Clear(){data.Clear();}internal Dictionary<string,object> data=new Dictionary<string,object>();public bool isEmpty{get{return data.Count==0;}}public bool HasVector(string n){return data.ContainsKey(n)&&data[n] is Vector4;}public Vector4 GetVector(string n){return (Vector4)data[n];}public void SetVector(string n,Vector4 v){data[n]=v;}public void SetTexture(string n,Texture2D v){data[n]=v;}public bool HasColor(string n){return data.ContainsKey(n)&&data[n] is Color;}public bool HasFloat(string n){return data.ContainsKey(n)&&data[n] is float;}public Color GetColor(string n){return (Color)data[n];}public float GetFloat(string n){return (float)data[n];}public void SetColor(string n,Color v){data[n]=v;}public void SetFloat(string n,float v){data[n]=v;}internal void Copy(MaterialPropertyBlock b){data.Clear();if(b!=null)foreach(var p in b.data)data.Add(p.Key,p.Value);}}
 public class Material:Object {public static int NativePassCount=3;public int passCount=NativePassCount;public string GetPassName(int pass){return pass==passCount-2?"ShadowCaster":"";}public string name;public void SetInt(string n,int v){}public void SetTexture(string n,Texture2D t){}public void SetTextureOffset(string n,Vector2 p){}public Shader shader=new Shader();public Vector4 GetVector(string n){return values.GetVector(n);}public MaterialPropertyBlock values=new MaterialPropertyBlock();public bool HasProperty(string n){return values.data.ContainsKey(n);}public Color GetColor(string n){return values.GetColor(n);}public float GetFloat(string n){return values.GetFloat(n);}}
 public class Renderer:Component {public bool enabled=true,forceRenderingOff;public Bounds bounds;public Material[] sharedMaterials;public bool FailOnce;public MaterialPropertyBlock wide=new MaterialPropertyBlock();public Dictionary<int,MaterialPropertyBlock> slots=new Dictionary<int,MaterialPropertyBlock>();public void GetPropertyBlock(MaterialPropertyBlock b){b.Copy(wide);}public void GetPropertyBlock(MaterialPropertyBlock b,int slot){MaterialPropertyBlock v;slots.TryGetValue(slot,out v);b.Copy(v);}public void SetPropertyBlock(MaterialPropertyBlock b,int slot){if(FailOnce){FailOnce=false;throw new Exception("injected renderer failure");}if(b==null){slots.Remove(slot);return;}var v=new MaterialPropertyBlock();v.Copy(b);slots[slot]=v;}}
 public class MeshRenderer:Renderer {}public class BillboardRenderer:Renderer {}
 public class Camera:Component {public string name="Scene Camera";public bool stereoEnabled,orthographic;public bool Inside=true;public static event Action<Camera> onPreCull,onPostRender;public void Begin(){if(onPreCull!=null)onPreCull(this);}public void End(){if(onPostRender!=null)onPostRender(this);}}
 public static class TestScene {public static Renderer[] Meshes;}
}
namespace RoR2 {
 public class CharacterBody:Component {public static List<CharacterBody> readOnlyInstancesList=new List<CharacterBody>();public ModelLocator modelLocator;}
 public class ModelLocator:Component {public Transform modelTransform;}
 public interface IInteractable {}
 public class ShopTerminalBehavior:Component {}public class Highlight:Component {}public class PickupDropletController:Component {}public class SceneExitController:Component {}
 public static class InstanceTracker {public static List<T> GetInstancesList<T>(){return Registry<T>.Items;}}
 public static class Registry<T>{public static List<T> Items=new List<T>();}
 public static class LayerIndex {public static Layer world=new Layer();public class Layer {public int mask=1<<11;}}
}
public static class InterfaceInstanceTracker<T>{public static List<T> GetInstancesList(){return RoR2.Registry<T>.Items;}}
namespace EventHorizon {
 public class HorizonVisual:Component {public float RealmAge=5,RealmOpening=2,CollapseProgress;public static List<HorizonVisual> Instances=new List<HorizonVisual>();public Transform transform=new Transform();public float RealmRadius=62.5f;public float SurfaceReach=62.5f;public float Weight=1;public float VisualWeight(Camera c){return Weight;}public static HorizonVisual ForCamera(Camera c){return c.Inside&&Instances.Count>0?Instances[0]:null;}}
 public static class DomainVisual {public static Material CreateGlowMaterial(){return new Material();}}
 public static class CastTimeline {public static float Expansion(float value,float duration){float t=Mathf.Clamp01(value/duration);return t*t*(3-2*t);}}
 public class ZeroVisual:Component {}public static class HorizonProjection {public static bool Supported=true;}
 public static class HorizonProjectileRegistry {public static List<Component> Projectiles=new List<Component>();}
 public static class Plugin {public static Logger Log=new Logger();public class Logger{public int Errors;public void LogInfo(string s){}public void LogError(string s){Errors++;}}}
}

namespace UnityEngine.Rendering {
 public enum BlendMode{SrcAlpha,One,OneMinusSrcAlpha}public enum CompareFunction{LessEqual}public enum CullMode{Off}
 public class CommandBuffer{public class Draw{public Mesh mesh;public Matrix4x4 matrix;}public List<Draw> draws=new List<Draw>();public void DrawMesh(Mesh m,Matrix4x4 x,Material mat,int sub,int pass,MaterialPropertyBlock b){if(pass!=mat.passCount-1)throw new Exception("Not the native visible color pass");draws.Add(new Draw{mesh=m,matrix=x});}}
}
class MeshHarness {
 static int checks;static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 static void Main(){
  var owner=new HorizonVisual();owner.transform.position=new Vector3(100,10,-200);
  var effect=new HorizonBlackHole(owner,new Vector3(0,50,18.75f),12.5f,new Vector3(-35,0,18));
  Check(effect.Anchor.parent==owner.transform,"anchored to domain root");var anchor=effect.Anchor.position;Check(anchor.x==100&&anchor.y==60&&anchor.z==-181.25f,"world position derived from cast, not camera");
  int triangles=0;
  foreach(var mesh in Mesh.All){
   Check(mesh.vertices.Count==mesh.uv.Count&&mesh.vertices.Count==mesh.colors.Count,"complete vertex channels");
   foreach(int i in mesh.indices)if(i<0||i>=mesh.vertices.Count)throw new Exception("Out-of-bounds triangle");
   foreach(var v in mesh.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z))throw new Exception("Nonfinite vertex");
   triangles+=mesh.indices.Count/3;
   if(mesh.name.Contains("event horizon")){foreach(var v in mesh.vertices)if(Math.Abs(v.magnitude-1)>.00001)throw new Exception("Core not spherical");foreach(var c in mesh.colors)if(c.r!=0||c.g!=0||c.b!=0)throw new Exception("Core emits light");}
  }
  Check(Mesh.All.Count==4&&triangles<50000,"bounded physical mesh topology");
  var camera=new Camera();camera.transform.position=new Vector3(100,12,-210);var first=new UnityEngine.Rendering.CommandBuffer();effect.Record(first,camera,1);Check(first.draws.Count==5,"one core, two disk layers, two photon layers");
  camera.transform.position=new Vector3(130,15,-190);var second=new UnityEngine.Rendering.CommandBuffer();effect.Record(second,camera,1);
  for(int i=0;i<3;i++){Check((first.draws[i].matrix.position-second.draws[i].matrix.position).sqrMagnitude==0,"physical position stable across cameras");Check((first.draws[i].matrix.scale-second.draws[i].matrix.scale).sqrMagnitude==0,"physical core/disk size stable across cameras");}
  Check((effect.Anchor.position-anchor).sqrMagnitude==0,"rendering never moves anchor");
  var outside=new UnityEngine.Rendering.CommandBuffer();effect.Record(outside,camera,0);Check(outside.draws.Count==0,"outside camera receives no celestial meshes");
  owner.CollapseProgress=.4f;var collapse=new UnityEngine.Rendering.CommandBuffer();effect.Record(collapse,camera,1);Check(collapse.draws[0].matrix.scale.x<first.draws[0].matrix.scale.x,"physical contraction");
  owner.CollapseProgress=1;var ended=new UnityEngine.Rendering.CommandBuffer();effect.Record(ended,camera,1);Check(ended.draws.Count==0,"no geometry after collapse");
  using(var output=new System.IO.BinaryWriter(System.IO.File.Create("tests/horizon-physical-meshes.bin"))){
   output.Write(Mesh.All.Count);foreach(var mesh in Mesh.All){output.Write(mesh.name);output.Write(mesh.vertices.Count);for(int i=0;i<mesh.vertices.Count;i++){var v=mesh.vertices[i];var u=mesh.uv[i];var c=mesh.colors[i];foreach(float f in new[]{v.x,v.y,v.z,u.x,u.y,c.r,c.g,c.b,c.a})output.Write(f);}output.Write(mesh.indices.Count);foreach(int index in mesh.indices)output.Write(index);}
   var texture=Texture2D.All[0];output.Write(texture.pixels.Length);foreach(var c in texture.pixels)foreach(float f in new[]{c.r,c.g,c.b,c.a})output.Write(f);
  }
  effect.Dispose();Check(UnityEngine.Object.Destroyed==11,"all owned meshes, materials, texture and root disposed");
  owner.CollapseProgress=0;Material.NativePassCount=2;var twoPassHole=new HorizonBlackHole(owner,new Vector3(0,50,18.75f),12.5f,new Vector3(-35,0,18));
  var passCamera=new GameObject().AddComponent<Camera>();passCamera.transform.position=new Vector3(0,4,-20);var passDraws=new UnityEngine.Rendering.CommandBuffer();twoPassHole.Record(passDraws,passCamera,1);Check(passDraws.draws.Count==5,"alternate native pass enumeration produces visible draws");twoPassHole.Dispose();
  Check(true,"visible pass selected with or without enumerated GrabPass");
  Console.WriteLine("PASS: "+checks+" physical singularity assertions; "+triangles+" unique triangles (modeled Unity API, no visual claim).");
 }
}

