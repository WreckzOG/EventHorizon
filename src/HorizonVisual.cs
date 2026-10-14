using System.Collections.Generic;
using RoR2;
using UnityEngine;
namespace EventHorizon
{
    // Map-wide cosmic presentation; the singularity retains its fixed physical anchor.
    public sealed class HorizonVisual : MonoBehaviour
    {
        internal static readonly List<HorizonVisual> Instances=new List<HorizonVisual>();
        private DomainMessage data;
        private float start,closing=-1;
        private HorizonAudio sound;
        private UnityEngine.SceneManagement.Scene scene;
        private GUIStyle title;
        private readonly Vector4[] impacts=new Vector4[16];
        private int impactIndex;
        internal float HoleSize, HoleHeight;
        internal HorizonBlackHole Singularity;
        internal Vector3 SingularityPosition {get{return Singularity!=null&&Singularity.Anchor?Singularity.Anchor.position:transform.position;}}
        internal float SurfaceReach {get{return RealmRadius*CastTimeline.Expansion(RealmAge,data.Expansion);}}
        internal float RealmAge {get{return Time.time-start;}}
        internal float RealmOpening {get{return data.Expansion;}}
        internal float RealmRadius {get{return data.Radius;}}
        internal bool RealmClosing {get{return closing>=0;}}
        internal float CollapseProgress {get{return closing<0?0:Mathf.Clamp01((Time.time-closing)/data.Collapse);}}
        internal float Envelope {get{return 1-CastTimeline.Expansion(CollapseProgress-.35f,.65f);}}
        internal float Instability {get{return RealmClosing?1:Mathf.Clamp01(RealmAge-data.Duration+1);}}
        internal float VisualWeight(Camera camera)
        {
            return Mathf.Clamp01(HorizonSettings.Intensity.Value)*Envelope*CastTimeline.Expansion(RealmAge,Mathf.Max(.05f,RealmOpening));
        }
        internal static HorizonVisual Create(DomainMessage data,float holeSize,float holeHeight,Vector3 visualOffset,float visualRadius,Vector3 diskAngles)
        {
            GameObject root=new GameObject("Event Horizon - death of space");root.transform.position=data.Center;
            HorizonVisual v=root.AddComponent<HorizonVisual>();v.data=data;v.start=Time.time-data.Age+data.Freeze;v.HoleSize=holeSize;v.HoleHeight=holeHeight;
            v.scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            for(int i=0;i<v.impacts.Length;i++)v.impacts[i].w=-100;
            try {
                v.Singularity=new HorizonBlackHole(v,visualOffset,Mathf.Max(1,visualRadius),diskAngles);
                v.sound=HorizonAudio.Create(root.transform,v.RealmAge+data.Freeze,data.Freeze,data.Duration,data.Collapse);
                Instances.Add(v);
            } catch {Destroy(root);throw;}
            return v;
        }
        internal static HorizonVisual ForCamera(Camera camera)
        {
            HorizonVisual best=null;float score=float.MaxValue;
            foreach(HorizonVisual v in Instances) {
                if(!v||v.RealmAge<0||v.Envelope<=0)continue;
                float distance=(camera.transform.position-v.transform.position).sqrMagnitude;
                if(v.scene!=UnityEngine.SceneManagement.SceneManager.GetActiveScene())continue;
                if(distance<score){best=v;score=distance;}
            }
            return best;
        }
        internal void BeginCollapse(float elapsed=0)
        {if(closing>=0)return;closing=Time.time-elapsed;if(sound)sound.Collapse(elapsed);}
        internal void Absorb(Vector3 point)
        {impacts[impactIndex++%impacts.Length]=new Vector4(point.x,point.y,point.z,RealmAge);}
        internal void UploadImpacts(UnityEngine.Rendering.CommandBuffer commands){commands.SetGlobalVectorArray("_EhzHits",impacts);}
        private void Update()
        {
            if(!Run.instance||scene!=UnityEngine.SceneManagement.SceneManager.GetActiveScene()){Destroy(gameObject);return;}
            if(RealmAge>=data.Duration)BeginCollapse(RealmAge-data.Duration);
            if(CollapseProgress>=1){Destroy(gameObject);return;}
            if(sound && Instability>0 && !RealmClosing)sound.Unstable();
        }
        private void OnGUI()
        {
            if(RealmAge<RealmOpening||RealmAge>RealmOpening+2.5f)return;
            Camera c=Camera.main;if(!c||ForCamera(c)!=this)return;
            if(title==null)title=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=28,fontStyle=FontStyle.Bold};
            Color saved=GUI.color;GUI.color=new Color(.78f,.61f,1,Mathf.Clamp01((RealmOpening+2.5f-RealmAge)*2));
            GUI.Label(new Rect(0,Screen.height*.19f,Screen.width,80),"DOMAIN EXPANSION\nEVENT HORIZON",title);GUI.color=saved;
        }
        private void OnDestroy()
        {Instances.Remove(this);HorizonSurfaces.ReleaseOwner(this);HorizonProjection.ReleaseOwner(this);if(Singularity!=null)Singularity.Dispose();}
    }
}
