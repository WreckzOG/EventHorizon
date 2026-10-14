using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;
namespace EventHorizon
{
    // Native shaders retain their texture alpha, wind, vertex deformation, depth and shadows.
    // Property blocks and decorative draw suppression are scoped to a single camera draw.
    internal static class HorizonSurfaces
    {
        private sealed class Surface {internal Renderer Renderer;internal Material[] Materials;internal bool Atmosphere,Clutter;}
        private sealed class Change
        {
            internal Renderer Renderer;internal int Slot;
            internal readonly MaterialPropertyBlock Original=new MaterialPropertyBlock(),Tinted=new MaterialPropertyBlock();
            internal void Restore(){if(Renderer)Renderer.SetPropertyBlock(Original.isEmpty?null:Original,Slot);}
            internal void Apply(){if(Renderer)Renderer.SetPropertyBlock(Tinted,Slot);}
        }
        private sealed class Hidden
        {
            internal Renderer Renderer;internal bool Original;
            internal void Restore(){if(Renderer)Renderer.forceRenderingOff=Original;}
            internal void Apply(){if(Renderer)Renderer.forceRenderingOff=true;}
        }
        private sealed class Scope {internal Camera Camera;internal HorizonVisual Owner;internal readonly List<Change> Changes=new List<Change>();internal readonly List<Hidden> Hidden=new List<Hidden>();}
        private static readonly List<Surface> surfaces=new List<Surface>();
        private static readonly HashSet<Renderer> gameplay=new HashSet<Renderer>();
        private static readonly List<Renderer> children=new List<Renderer>();
        private static readonly List<Scope> scopes=new List<Scope>();
        private static readonly Stack<Hidden> spareHidden=new Stack<Hidden>();
        private static readonly Stack<Change> spareChanges=new Stack<Change>();
        private static readonly Stack<Scope> spareScopes=new Stack<Scope>();
        private static readonly Plane[] planes=new Plane[6];
        // Verified against installed HG triplanar, Standard, grass and SpeedTree shader properties.
        private static readonly string[] tints={"_Color","_BaseColor","_TintColor","_overColor","_overallColor","_RColor","_GColor","_BColor","_TintRed","_TintGreen","_TintBlue","_RedTint","_GreenTint","_BlueTint","_greenColor","_blueColor","_RandomSplatColor","_SecSplatColor","_HueVariation"};
        private static readonly string[] vectors={"_Albedo"};
        private static readonly string[] emissions={"_EmColor","_EmissionColor","_EmissionTint","_EmTint","_emColorRed","_emColorGreen","_emColorBlue","_emBackground","_InnerColor","_FlashColor","_SpecColor"};
        private static readonly string[] powers={"_EmPower","_EmissionPower","_FlowEmissionStrength","_emissiveIntensity","_emIntensityRed","_emIntensityGreen","_emIntensityBlue","_emIntensityBackground","_EmBoost","_IneerColorIntensity","_FresnelBoost","_FresnelAdd","_PrintBoost"};
        private static readonly string[] specular={"_SpecularStrength","_RedChannelSpecularStrength","_GreenChannelSpecularStrength","_BlueChannelSpecularStrength"};
        private static readonly string[] smoothness={"_Smoothness","_SecondarySmoothness","_RedChannelSmoothness","_GreenChannelSmoothness","_BlueChannelSmoothness","_Glossiness"};
        private static float nextScan;
        private static bool initialized,reported;
        private static Texture2D terrainAlbedo;
        private static readonly string[] terrainTextures={"_RedChannelTopTex","_RedChannelSideTex","_GreenChannelTex","_BlueChannelTex","_RedSplatTex","_GreenSplatTex","_BlueSplatTex","_secSplatTex"};
        private static Texture2D TerrainAlbedo()
        {
            if(terrainAlbedo)return terrainAlbedo;
            terrainAlbedo=new Texture2D(2,2,TextureFormat.RGBA32,false){name="Event Horizon neutral charcoal terrain"};
            terrainAlbedo.SetPixels(new[]{new Color(.12f,.09f,.16f,1),new Color(.13f,.10f,.17f,1),new Color(.13f,.10f,.17f,1),new Color(.12f,.09f,.16f,1)});
            terrainAlbedo.Apply(false,true);return terrainAlbedo;
        }
        internal static int LastCount;
        internal static void Initialize()
        {if(initialized)return;initialized=true;Camera.onPreCull+=BeforeCamera;Camera.onPostRender+=AfterCamera;}
        private static void Protect(Component component)
        {if(!component)return;children.Clear();component.GetComponentsInChildren<Renderer>(true,children);foreach(Renderer r in children)if(r)gameplay.Add(r);}
        private static void CollectGameplay()
        {
            gameplay.Clear();
            foreach(CharacterBody body in CharacterBody.readOnlyInstancesList){Protect(body);if(body&&body.modelLocator)Protect(body.modelLocator.modelTransform);}
            foreach(var shot in HorizonProjectileRegistry.Projectiles)Protect(shot);
            foreach(IInteractable interaction in InterfaceInstanceTracker<IInteractable>.GetInstancesList())Protect(interaction as Component);
            foreach(var drop in InstanceTracker.GetInstancesList<PickupDropletController>())Protect(drop);
            foreach(var exit in InstanceTracker.GetInstancesList<SceneExitController>())Protect(exit);
        }
        private static void Refresh()
        {
            if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+.5f;surfaces.Clear();
            foreach(Renderer renderer in UnityEngine.Object.FindObjectsOfType<Renderer>()){
                // World/default mesh scenery. Skinned characters, particle/line/trail renderers,
                // UI layers and this mod's meshes are deliberately not surface candidates.
                if(!renderer)continue;
                bool atmosphere=IsAtmosphere(renderer),clutter=IsClutter(renderer);
                if(!(renderer is MeshRenderer)&&!(renderer is BillboardRenderer)&&!(atmosphere&&renderer is ParticleSystemRenderer)&&!(clutter&&renderer is LineRenderer))continue;
                if(renderer.GetComponentInParent<GenericPickupController>()||renderer.GetComponentInParent<EffectComponent>()||renderer.GetComponentInParent<CharacterBody>()||renderer.GetComponentInParent<RoR2.Projectile.ProjectileController>())continue;
                bool environment=((1<<renderer.gameObject.layer)&(LayerIndex.world.mask|1))!=0;
                foreach(Material m in renderer.sharedMaterials)if(m&&m.shader){string n=m.shader.name.ToLowerInvariant();environment|=n.Contains("terrain")||n.Contains("environment/")||n.Contains("speedtree")||n.StartsWith("nature/")||n.StartsWith("hopoo games/deferred/");}
                environment|=atmosphere;
                if(!environment)continue;
                bool owned=false;foreach(MonoBehaviour component in renderer.GetComponentsInParent<MonoBehaviour>(true))if(component&&component.GetType().Namespace=="EventHorizon"){owned=true;break;}
                if(owned)continue;
                if(renderer.GetComponentInParent<HorizonVisual>())continue;
                if(renderer.GetComponentInParent<ShopTerminalBehavior>()||renderer.GetComponentInParent<Highlight>())continue;
                surfaces.Add(new Surface{Renderer=renderer,Materials=renderer.sharedMaterials,Atmosphere=atmosphere,Clutter=clutter});
            }
        }
        private static bool IsClutter(Renderer renderer)
        {
            foreach(Material m in renderer.sharedMaterials)if(m&&(HorizonClutterNames.Decoration(m.name)||HorizonClutterNames.Decoration(m.shader?m.shader.name:null)))return true;
            for(Transform t=renderer.transform;t;t=t.parent)if(HorizonClutterNames.Decoration(t.name))return true;
            return false;
        }
        private static bool IsAtmosphere(Renderer renderer)
        {
            bool transparent=false;foreach(Material m in renderer.sharedMaterials)if(m&&m.renderQueue>=3000)transparent=true;
            if(!transparent)return false;
            for(Transform t=renderer.transform;t;t=t.parent){
                string n=t.name.ToLowerInvariant();
                if(n.StartsWith("alienskies-")||n.Contains("cloud")||n.Contains("fog")||n.Contains("mist")||n=="low atmospheric wisps")return true;
            }
            return false;
        }
        private static void BeforeCamera(Camera camera)
        {
            if(scopes.Count==0&&HorizonVisual.Instances.Count==0)return;
            // Suspend an enclosing view before any nested reflection/probe/outside camera.
            if(scopes.Count>0){foreach(Change change in scopes[scopes.Count-1].Changes)change.Restore();foreach(Hidden hidden in scopes[scopes.Count-1].Hidden)hidden.Restore();}
            Scope scope=spareScopes.Count>0?spareScopes.Pop():new Scope();scope.Camera=camera;scope.Owner=null;scopes.Add(scope);
            try {
                if(!camera||camera.name!="Scene Camera"||camera.stereoEnabled||camera.orthographic)return;
                HorizonVisual owner=HorizonVisual.ForCamera(camera);if(!owner||!HorizonProjection.Supported)return;
                scope.Owner=owner;float weight=owner.VisualWeight(camera);if(weight<=.001f)return;
                Refresh();CollectGameplay();GeometryUtility.CalculateFrustumPlanes(camera,planes);LastCount=0;
                foreach(Surface surface in surfaces){
                    Renderer renderer=surface.Renderer;
                    if(!renderer||!renderer.enabled||renderer.forceRenderingOff||gameplay.Contains(renderer)||!GeometryUtility.TestPlanesAABB(planes,renderer.bounds))continue;
                    if(surface.Clutter&&weight>.05f){
                        Hidden hidden=spareHidden.Count>0?spareHidden.Pop():new Hidden();hidden.Renderer=renderer;hidden.Original=renderer.forceRenderingOff;
                        scope.Hidden.Add(hidden);hidden.Apply();continue;
                    }
                    // Once the camera is enclosed, transform all visible scenery, including
                    // the far skyline. Combat and camera membership keep their original radius.
                    for(int slot=0;slot<surface.Materials.Length;slot++){
                        Material material=surface.Materials[slot];if(!material)continue;
                        Change change=spareChanges.Count>0?spareChanges.Pop():new Change();change.Renderer=renderer;change.Slot=slot;
                        renderer.GetPropertyBlock(change.Original,slot);renderer.GetPropertyBlock(change.Tinted,slot);
                        // An indexed block overrides the renderer-wide block. Inherit that block
                        // when no indexed one exists; clearing our indexed block restores precedence.
                        if(change.Original.isEmpty)renderer.GetPropertyBlock(change.Tinted);
                        bool changed=false;
                        // These installed opaque terrain channel textures are albedo/height,
                        // not foliage alpha maps. Leave every original MainTex/cutoff untouched.
                        if(material.shader&&material.shader.name.ToLowerInvariant().Contains("triplanar"))
                            foreach(string name in terrainTextures)if(material.HasProperty(name)&&weight>.999f){change.Tinted.SetTexture(name,TerrainAlbedo());changed=true;}
                        foreach(string name in tints)if(material.HasProperty(name)){
                            Color original=change.Tinted.HasColor(name)?change.Tinted.GetColor(name):material.GetColor(name);
                            float variation=.88f+.12f*Mathf.Sin(renderer.bounds.center.x*.11f+renderer.bounds.center.z*.09f);
                            Color dark=surface.Atmosphere?new Color(.035f,.012f,.09f,original.a*.12f):new Color(.010f*variation,.006f*variation,.018f*variation,original.a);
                            change.Tinted.SetColor(name,Color.Lerp(original,dark,weight));changed=true;
                        }
                        foreach(string name in vectors)if(material.HasProperty(name)){
                            Vector4 original=change.Tinted.HasVector(name)?change.Tinted.GetVector(name):material.GetVector(name);
                            change.Tinted.SetVector(name,Vector4.Lerp(original,new Vector4(.010f,.006f,.018f,original.w),weight));changed=true;
                        }
                        foreach(string name in specular)if(material.HasProperty(name)){
                            float original=change.Tinted.HasFloat(name)?change.Tinted.GetFloat(name):material.GetFloat(name);
                            change.Tinted.SetFloat(name,Mathf.Lerp(original,.018f,weight));changed=true;
                        }
                        foreach(string name in smoothness)if(material.HasProperty(name)){
                            float original=change.Tinted.HasFloat(name)?change.Tinted.GetFloat(name):material.GetFloat(name);
                            change.Tinted.SetFloat(name,Mathf.Lerp(original,.22f,weight));changed=true;
                        }
                        foreach(string name in emissions)if(material.HasProperty(name)){
                            Color original=change.Tinted.HasColor(name)?change.Tinted.GetColor(name):material.GetColor(name);
                            change.Tinted.SetColor(name,Color.Lerp(original,new Color(0,0,0,original.a),weight));changed=true;
                        }
                        foreach(string name in powers)if(material.HasProperty(name)){
                            float original=change.Tinted.HasFloat(name)?change.Tinted.GetFloat(name):material.GetFloat(name);
                            change.Tinted.SetFloat(name,original*(1-weight));changed=true;
                        }
                        if(!changed){spareChanges.Push(change);continue;}
                        scope.Changes.Add(change);change.Apply();LastCount++;
                    }
                }
            } catch(Exception error){RestoreScope(scope);scope.Owner=null;if(!reported){reported=true;Plugin.Log.LogError("Event Horizon surface scope restored after failure: "+error);}}
        }
        // Draw only rigid opaque scenery against the real camera depth. This is a
        // coverage texture, not a second scene render: no replacement camera, layer
        // changes, fake normals, depth writes or unbounded player masks.
        internal static void RecordScenery(CommandBuffer commands,Camera camera,Material coverage)
        {
            Refresh();CollectGameplay();GeometryUtility.CalculateFrustumPlanes(camera,planes);
            foreach(Surface surface in surfaces){
                Renderer renderer=surface.Renderer;
                if(!(renderer is MeshRenderer)||!renderer||!renderer.enabled||renderer.forceRenderingOff||surface.Atmosphere||gameplay.Contains(renderer)||
                    (camera.cullingMask&(1<<renderer.gameObject.layer))==0||!GeometryUtility.TestPlanesAABB(planes,renderer.bounds))continue;
                for(int slot=0;slot<surface.Materials.Length;slot++){
                    Material material=surface.Materials[slot];if(!material||material.renderQueue>2500||!material.shader)continue;
                    string name=material.shader.name.ToLowerInvariant();
                    // Animated vegetation cannot use a rigid coverage vertex pass. Its native
                    // charcoal treatment remains, with all original cutout/deformation intact.
                    if(name.Contains("speedtree")||name.Contains("grass")||name.Contains("foliage")||name.Contains("nature/"))continue;
                    commands.DrawRenderer(renderer,coverage,slot,0);
                }
            }
        }
        private static void RestoreScope(Scope scope)
        {
            for(int i=scope.Changes.Count-1;i>=0;i--){Change change=scope.Changes[i];change.Restore();change.Renderer=null;spareChanges.Push(change);}
            scope.Changes.Clear();
            for(int i=scope.Hidden.Count-1;i>=0;i--){Hidden hidden=scope.Hidden[i];hidden.Restore();hidden.Renderer=null;spareHidden.Push(hidden);}
            scope.Hidden.Clear();
        }
        private static void Pop()
        {
            Scope scope=scopes[scopes.Count-1];RestoreScope(scope);
            scope.Camera=null;scope.Owner=null;scopes.RemoveAt(scopes.Count-1);spareScopes.Push(scope);
        }
        private static void AfterCamera(Camera camera)
        {
            int index=scopes.FindLastIndex(delegate(Scope s){return s.Camera==camera;});if(index<0)return;
            while(scopes.Count>index)Pop();
            if(scopes.Count>0){foreach(Change change in scopes[scopes.Count-1].Changes)change.Apply();foreach(Hidden hidden in scopes[scopes.Count-1].Hidden)hidden.Apply();}
        }
        internal static void ReleaseOwner(HorizonVisual owner)
        {if(scopes.Exists(delegate(Scope scope){return scope.Owner==owner;}))while(scopes.Count>0)Pop();}
        internal static void Shutdown()
        {
            Camera.onPreCull-=BeforeCamera;Camera.onPostRender-=AfterCamera;initialized=false;
            while(scopes.Count>0)Pop();surfaces.Clear();gameplay.Clear();children.Clear();spareChanges.Clear();spareHidden.Clear();spareScopes.Clear();nextScan=0;
            if(terrainAlbedo)UnityEngine.Object.Destroy(terrainAlbedo);terrainAlbedo=null;
        }
    }
}
