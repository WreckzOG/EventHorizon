using System;
using System.IO;
using System.Collections.Generic;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
namespace EventHorizon
{
    // Camera-local background pass followed by genuinely world-positioned celestial meshes.
    // Scene geometry and native transparent gameplay render normally.
    internal static class HorizonProjection
    {
        private sealed class Frame {internal Camera Camera;internal PostProcessLayer Layer;internal HorizonVisual Owner;internal bool AddedDepth,Reported;internal CommandBuffer Commands;internal PropertySheetFactory Sheets;}
        private static readonly Dictionary<Camera,Frame> frames=new Dictionary<Camera,Frame>();
        private static Shader projection,sceneryShader;
        private static Material sceneryCoverage;
        private static readonly int sceneryMask=Shader.PropertyToID("_EhzSceneryMask");
        private static readonly int temp=Shader.PropertyToID("_EventHorizonSkySource");
        private static bool attempted,reported;
        internal static bool Supported {get{Ensure();return projection;}}
        private static void Ensure()
        {
            if(attempted)return;attempted=true;
            if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Direct3D11)return;
            AssetBundle bundle=null;
            try {
                using(Stream stream=typeof(Plugin).Assembly.GetManifestResourceStream("SurvivorDomains.HorizonProjection")){
                    if(stream==null)throw new InvalidOperationException("Missing Event Horizon shader resource");
                    byte[] data=new byte[(int)stream.Length];int offset=0,n;
                    while(offset<data.Length&&(n=stream.Read(data,offset,data.Length-offset))>0)offset+=n;
                    if(offset!=data.Length)throw new EndOfStreamException();bundle=AssetBundle.LoadFromMemory(data);
                }
                if(!bundle)throw new InvalidOperationException("Event Horizon shader bundle did not load");
                projection=bundle.LoadAsset<Shader>("assets/survivordomains/horizonprojection.shader");
                if(!projection||!projection.isSupported)throw new InvalidOperationException("Event Horizon shader unsupported");
                try {
                using(Stream stream=typeof(Plugin).Assembly.GetManifestResourceStream("SurvivorDomains.HorizonSceneryMask")){
                    if(stream==null)throw new InvalidOperationException("Missing scenery coverage resource");
                    byte[] data=new byte[(int)stream.Length];int offset=0,n;
                    while(offset<data.Length&&(n=stream.Read(data,offset,data.Length-offset))>0)offset+=n;
                    if(offset!=data.Length)throw new EndOfStreamException();
                    AssetBundle maskBundle=AssetBundle.LoadFromMemory(data);
                    if(!maskBundle)throw new InvalidOperationException("Scenery coverage bundle did not load");
                    try{sceneryShader=maskBundle.LoadAsset<Shader>("assets/survivordomains/horizonscenerymask.shader");}
                    finally{maskBundle.Unload(false);}
                    if(!sceneryShader||!sceneryShader.isSupported)throw new InvalidOperationException("Scenery coverage shader unsupported");
                    sceneryCoverage=new Material(sceneryShader);sceneryCoverage.SetColor("_EhzMaskWhite",Color.white);
                }
                } catch(Exception maskError){
                    if(sceneryCoverage)UnityEngine.Object.Destroy(sceneryCoverage);sceneryCoverage=null;
                    if(sceneryShader)UnityEngine.Object.Destroy(sceneryShader);sceneryShader=null;
                    Plugin.Log.LogWarning("Event Horizon scenery fade unavailable; retaining cosmic sky and physical black hole: "+maskError);
                }
                Plugin.Log.LogInfo("Event Horizon map-wide v6: map-wide realm with scenery dissolve; camera-local fog arbitration; physical singularity preserved.");
            }catch(Exception e){if(sceneryCoverage)UnityEngine.Object.Destroy(sceneryCoverage);sceneryCoverage=null;if(sceneryShader)UnityEngine.Object.Destroy(sceneryShader);sceneryShader=null;if(projection)UnityEngine.Object.Destroy(projection);projection=null;Plugin.Log.LogError("Event Horizon projection unavailable: "+e);}
            finally{if(bundle)bundle.Unload(false);}
        }
        private static void Prepare(Camera camera,PostProcessLayer layer)
        {
            if(!camera||camera.name!="Scene Camera")return;
            HorizonVisual owner=camera.stereoEnabled||camera.orthographic?null:HorizonVisual.ForCamera(camera);Frame frame;
            if(!owner||!Supported){if(frames.TryGetValue(camera,out frame)){Dispose(frame);frames.Remove(camera);layer.OnVolumeSettingsChanged();}return;}
            if(!frames.TryGetValue(camera,out frame)){
                frame=new Frame{Camera=camera,Layer=layer,Commands=new CommandBuffer{name="Event Horizon sky and physical singularity"},Sheets=new PropertySheetFactory()};frames.Add(camera,frame);camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha,frame.Commands);
            }
            if(frame.Owner!=owner){frame.Owner=owner;frame.Reported=false;layer.OnVolumeSettingsChanged();}
            if((camera.depthTextureMode&DepthTextureMode.Depth)==0){frame.AddedDepth=true;camera.depthTextureMode|=DepthTextureMode.Depth;}
            CommandBuffer cmd=frame.Commands;cmd.Clear();Upload(frame);
            // Share the real writable camera depth for exact visible-scenery coverage.
            // Keep MSAA compatible with that attachment; use the existing sky-only route
            // if an unusual custom target is multisampled (the standard deferred path is not).
            bool maskSupported=sceneryCoverage&&(camera.actualRenderingPath==RenderingPath.DeferredShading||(!camera.allowMSAA||QualitySettings.antiAliasing<=1));
            if(camera.targetTexture&&camera.targetTexture.antiAliasing>1)maskSupported=false;
            if(maskSupported){
                cmd.GetTemporaryRT(sceneryMask,camera.scaledPixelWidth,camera.scaledPixelHeight,0,FilterMode.Point,RenderTextureFormat.ARGB32);
                cmd.SetRenderTarget(new RenderTargetIdentifier(sceneryMask),new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
                cmd.ClearRenderTarget(false,true,Color.clear);
                HorizonSurfaces.RecordScenery(cmd,camera,sceneryCoverage);
                cmd.SetGlobalTexture(sceneryMask,new RenderTargetIdentifier(sceneryMask));
            }else cmd.SetGlobalTexture(sceneryMask,Texture2D.blackTexture);
            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            cmd.GetTemporaryRT(temp,camera.scaledPixelWidth,camera.scaledPixelHeight,0,FilterMode.Bilinear,camera.allowHDR?RenderTextureFormat.DefaultHDR:RenderTextureFormat.Default);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.CameraTarget,temp);
            // The camera target owns writable depth. BuiltinRenderTextureType.Depth is a
            // sampled texture and must never be used as a writable depth attachment.
            cmd.BlitFullscreenTriangle(temp,BuiltinRenderTextureType.CameraTarget,BuiltinRenderTextureType.CameraTarget,frame.Sheets.Get(projection),0);
            cmd.ReleaseTemporaryRT(temp);
            if(maskSupported)cmd.ReleaseTemporaryRT(sceneryMask);
            if(owner.Singularity!=null)owner.Singularity.Record(cmd,camera,owner.VisualWeight(camera));
        }
        private static void Upload(Frame frame)
        {
            Camera camera=frame.Camera;HorizonVisual owner=frame.Owner;CommandBuffer cmd=frame.Commands;
            // This runs after PostProcessLayer resets/jitters its projection. Do not reuse rays
            // from the previous frame's projection or derive the celestial anchor from forward.
            Vector3 forward=camera.transform.forward;
            Vector3 a=camera.ViewportPointToRay(new Vector3(0,0,0)).direction;
            Vector3 b=camera.ViewportPointToRay(new Vector3(1,0,0)).direction;
            Vector3 c=camera.ViewportPointToRay(new Vector3(0,1,0)).direction;
            a/=Mathf.Max(.0001f,Vector3.Dot(a,forward));b/=Mathf.Max(.0001f,Vector3.Dot(b,forward));c/=Mathf.Max(.0001f,Vector3.Dot(c,forward));
            cmd.SetGlobalVector("_EhzRayOrigin",new Vector4(a.x,a.y,a.z,camera.scaledPixelWidth));
            Vector3 r=b-a,u=c-a;cmd.SetGlobalVector("_EhzRayRight",new Vector4(r.x,r.y,r.z,camera.scaledPixelHeight));cmd.SetGlobalVector("_EhzRayUp",u);
            cmd.SetGlobalVector("_EhzCamera",camera.transform.position);Vector3 center=owner.transform.position;
            cmd.SetGlobalVector("_EhzDomain",new Vector4(center.x,center.y,center.z,owner.RealmRadius));
            cmd.SetGlobalVector("_EhzState",new Vector4(owner.RealmAge,owner.VisualWeight(camera),owner.Instability,0));
            cmd.SetGlobalVector("_EhzCosmic",new Vector4(HorizonSettings.Quality.Value,HorizonSettings.Intensity.Value,owner.CollapseProgress,HorizonSettings.Lensing.Value));
            SceneCamera sceneCamera=camera.GetComponent<SceneCamera>();CharacterBody body=sceneCamera&&sceneCamera.cameraRigController?sceneCamera.cameraRigController.targetBody:null;
            Vector3 foot=body?body.footPosition:center;
            cmd.SetGlobalVector("_EhzFoot",new Vector4(owner.HoleSize,owner.HoleHeight,body&&body.characterMotor&&body.characterMotor.isGrounded?1:0,0));
            cmd.SetGlobalVector("_EhzGrandHit",new Vector4(HorizonSettings.Dust.Value,foot.x,foot.y,foot.z));
            cmd.SetGlobalVector("_EhzRight",camera.transform.right);cmd.SetGlobalVector("_EhzUp",camera.transform.up);cmd.SetGlobalVector("_EhzForward",forward);
            cmd.SetGlobalVector("_EhzParams",new Vector4(camera.farClipPlane,owner.RealmOpening,HorizonSettings.Intensity.Value,0));owner.UploadImpacts(cmd);
            if(!frame.Reported){frame.Reported=true;Plugin.Log.LogInfo("Event Horizon map-wide v6 camera configured: path="+camera.actualRenderingPath+"; target="+camera.scaledPixelWidth+"x"+camera.scaledPixelHeight+"; map-wide scenery dissolve; native camera depth preserved; physical anchor="+owner.SingularityPosition+"; map-wide fog arbitration active.");}
        }
        private static void Dispose(Frame frame)
        {
            if(frame.Camera&&frame.Commands!=null)frame.Camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha,frame.Commands);
            if(frame.Commands!=null){frame.Commands.Release();frame.Commands=null;}
            if(frame.Sheets!=null){frame.Sheets.Release();frame.Sheets=null;}
            if(frame.AddedDepth&&frame.Camera&&(!frame.Layer||(frame.Layer.cameraDepthFlags&DepthTextureMode.Depth)==0))frame.Camera.depthTextureMode&=~DepthTextureMode.Depth;
        }
        internal static void ReleaseOwner(HorizonVisual owner)
        {
            List<Camera> remove=new List<Camera>();foreach(var pair in frames)if(pair.Value.Owner==owner||!pair.Key){if(pair.Value.Layer)pair.Value.Layer.OnVolumeSettingsChanged();Dispose(pair.Value);remove.Add(pair.Key);}
            foreach(Camera camera in remove)frames.Remove(camera);
        }
        internal static void Release()
        {
            foreach(Frame frame in frames.Values){if(frame.Layer)frame.Layer.OnVolumeSettingsChanged();Dispose(frame);}frames.Clear();
            if(sceneryCoverage)UnityEngine.Object.Destroy(sceneryCoverage);sceneryCoverage=null;
            if(sceneryShader)UnityEngine.Object.Destroy(sceneryShader);sceneryShader=null;
            if(projection)UnityEngine.Object.Destroy(projection);projection=null;attempted=false;
        }
        [HarmonyPatch(typeof(PostProcessLayer),"OnPreCull")]
        private static class PrepareCamera
        {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(PostProcessLayer __instance)
            {
                // Fog commands are cached. Invalidate BEFORE native command recording,
                // both on entry and exit, including crossing the boundary in one frame.
                Camera camera=__instance.GetComponent<Camera>();
                if(!camera||camera.name!="Scene Camera")return;
                HorizonVisual owner=camera.stereoEnabled||camera.orthographic?null:HorizonVisual.ForCamera(camera);
                Frame frame;frames.TryGetValue(camera,out frame);
                if((frame==null?null:frame.Owner)!=owner)__instance.OnVolumeSettingsChanged();
            }
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(PostProcessLayer __instance)
            {try{Prepare(__instance.GetComponent<Camera>(),__instance);}catch(Exception error){if(!reported){reported=true;Plugin.Log.LogError("Event Horizon physical camera setup failed: "+error);}Release();attempted=true;}}
        }
    }
}
