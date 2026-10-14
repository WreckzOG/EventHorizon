using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace EventHorizon
{
    // Real meshes in meters at a fixed transform. Only the photon rim faces the viewer.
    // DrawMesh targets the owning camera; there are no scene-wide renderer toggles.
    internal sealed class HorizonBlackHole : IDisposable
    {
        internal readonly Transform Anchor;
        private readonly HorizonVisual owner;
        private readonly float radius;
        private readonly Quaternion diskRotation;
        private readonly Mesh sphere,disk,ring,halo;
        private readonly Material darkness,plasma,plasmaVeil,photon,glow;
        private readonly int colorPass;
        private readonly Texture2D flow;
        private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        internal HorizonBlackHole(HorizonVisual owner,Vector3 offset,float radius,Vector3 angles)
        {
            this.owner=owner;this.radius=radius;diskRotation=Quaternion.Euler(angles);
            GameObject root=new GameObject("Event Horizon - physical singularity");Anchor=root.transform;Anchor.SetParent(owner.transform,false);Anchor.localPosition=offset;
            try {
                darkness=Material("Perfect black physical event horizon",false,true);
                plasma=Material("Turbulent accretion disk",true,false);plasmaVeil=Material("Counter-flow plasma filaments",true,false);
                photon=Material("Violet-white photon rim",true,false);glow=Material("Soft photon atmosphere",true,false);
                // The installed Standard Unlit has a GrabPass and ShadowCaster before
                // its visible color pass. DrawMesh does NOT select that pass for us.
                colorPass=darkness.passCount-1;
                if(colorPass<0||darkness.GetPassName(colorPass)=="ShadowCaster")
                    throw new InvalidOperationException("Physical singularity has no visible color pass");
                Plugin.Log.LogInfo("Event Horizon singularity color pass="+colorPass+" / "+darkness.passCount+" shader="+darkness.shader.name+"; fixed world anchor="+Anchor.position);
                flow=PlasmaTexture();plasma.SetTexture("_MainTex",flow);plasmaVeil.SetTexture("_MainTex",flow);
                sphere=Sphere();disk=Disk();ring=Rim(false);halo=Rim(true);
                // MeshFilters make the authored physical components inspectable in the hierarchy.
                Attach("Black sphere",sphere);Attach("Tilted accretion disk",disk);Attach("Photon ring",ring);
            } catch {Dispose();throw;}
        }
        private void Attach(string name,Mesh mesh){GameObject child=new GameObject(name);child.transform.SetParent(Anchor,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;}
        private static Material Material(string name,bool additive,bool depth)
        {
            Material m=DomainVisual.CreateGlowMaterial();if(!m)throw new InvalidOperationException("Native unlit mesh material unavailable");
            m.name=name;m.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);m.SetInt("_DstBlend",(int)(additive?BlendMode.One:BlendMode.OneMinusSrcAlpha));
            m.SetInt("_ZWrite",depth?1:0);m.SetInt("_ZTest",(int)CompareFunction.LessEqual);m.SetInt("_Cull",(int)CullMode.Off);return m;
        }
        private static Mesh Make(string name,List<Vector3> vertices,List<Vector2> uv,List<Color> colors,List<int> triangles)
        {Mesh mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;}
        private static Mesh Sphere()
        {
            const int columns=128,rows=64;
            var v=new List<Vector3>();var uv=new List<Vector2>();var c=new List<Color>();var t=new List<int>();
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++){
                float p=Mathf.PI*y/rows,a=2*Mathf.PI*x/columns;v.Add(new Vector3(Mathf.Sin(p)*Mathf.Cos(a),Mathf.Cos(p),Mathf.Sin(p)*Mathf.Sin(a)));uv.Add(Vector2.zero);c.Add(new Color(0,0,0,1));
                if(x<columns&&y<rows){int i=y*(columns+1)+x,b=i+columns+1;t.Add(i);t.Add(b);t.Add(i+1);t.Add(i+1);t.Add(b);t.Add(b+1);}
            }
            return Make("Smooth 128x64 event horizon",v,uv,c,t);
        }
        private static Mesh Disk()
        {
            const int columns=256,rows=20;
            var v=new List<Vector3>();var uv=new List<Vector2>();var c=new List<Color>();var t=new List<int>();
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++){
                float q=(float)y/rows,a=2*Mathf.PI*x/columns,r=Mathf.Lerp(1.08f,2.9f,q);
                v.Add(new Vector3(Mathf.Cos(a)*r,.025f*Mathf.Sin(a*3)*Mathf.Sin(q*Mathf.PI),Mathf.Sin(a)*r));uv.Add(new Vector2((float)x/columns,q));c.Add(Color.white);
                if(x<columns&&y<rows){int i=y*(columns+1)+x,b=i+columns+1;t.Add(i);t.Add(i+1);t.Add(b);t.Add(i+1);t.Add(b+1);t.Add(b);}
            }
            return Make("Physical warped plasma annulus",v,uv,c,t);
        }
        private static Mesh Rim(bool soft)
        {
            const int columns=256,rows=12;
            var v=new List<Vector3>();var uv=new List<Vector2>();var c=new List<Color>();var t=new List<int>();
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++){
                float q=(float)y/rows,a=2*Mathf.PI*x/columns,r=soft?Mathf.Lerp(.997f,1.22f,q):Mathf.Lerp(1.005f,1.05f,q);
                float alpha=soft?Mathf.Pow(1-q,4)*.18f:Mathf.Pow(Mathf.Sin(q*Mathf.PI),2);
                v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0));uv.Add(Vector2.zero);c.Add(soft?new Color(.52f,.09f,1.2f,alpha):new Color(2.4f,1.35f,3.4f,alpha));
                if(x<columns&&y<rows){int i=y*(columns+1)+x,b=i+columns+1;t.Add(i);t.Add(i+1);t.Add(b);t.Add(i+1);t.Add(b+1);t.Add(b);}
            }
            return Make(soft?"Atmospheric photon glow":"Antialiased photon band",v,uv,c,t);
        }
        private static float Cell(int x,int y)
        {uint n=unchecked((uint)x*374761393u+(uint)y*668265263u);n=unchecked((n^(n>>13))*1274126177u);return ((n^(n>>16))&0x00ffffffu)/16777215f;}
        private static float FlowNoise(float x,float y)
        {
            int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);float u=x-ix,v=y-iy;u=u*u*(3-2*u);v=v*v*(3-2*v);
            return Mathf.Lerp(Mathf.Lerp(Cell(ix,iy),Cell(ix+1,iy),u),Mathf.Lerp(Cell(ix,iy+1),Cell(ix+1,iy+1),u),v);
        }
        private static Texture2D PlasmaTexture()
        {
            const int width=512,height=128;var pixels=new Color[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++){
                float q=(float)y/(height-1),a=2*Mathf.PI*x/width;
                float swirl=a+q*6,px=Mathf.Cos(swirl),py=Mathf.Sin(swirl);
                float coarse=FlowNoise(px*2.1f+7+q*4,py*2.1f+11);
                float fine=FlowNoise(px*11+q*29+3,py*11+19);
                float filament=Mathf.Pow(Mathf.Clamp01(.5f+.5f*Mathf.Sin(q*96+coarse*18+fine*8)),3);
                float edge=CastTimeline.Expansion(q,.045f)*(1-CastTimeline.Expansion(q-.58f,.42f));
                float energy=(.04f+.55f*coarse*coarse+.75f*filament*coarse)*edge*Mathf.Pow(1-q,1.6f);
                Color col=Color.Lerp(new Color(.16f,.12f,.62f),new Color(.82f,.09f,.64f),coarse);
                col=Color.Lerp(col,new Color(.76f,.60f,1),filament*Mathf.Pow(1-q,1.6f));col.a=energy;
                pixels[y*width+x]=col;
            }
            Texture2D texture=new Texture2D(width,height,TextureFormat.RGBA32,true,true){name="Periodic turbulent plasma",wrapModeU=TextureWrapMode.Repeat,wrapModeV=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=4};
            texture.SetPixels(pixels);texture.Apply(true,true);return texture;
        }
        internal void Record(CommandBuffer commands,Camera camera,float weight)
        {
            if(!Anchor||weight<=.001f)return;
            float formation=CastTimeline.Expansion(owner.RealmAge-owner.RealmOpening*.5f,owner.RealmOpening*.5f);
            float shrink=1-CastTimeline.Expansion(owner.CollapseProgress-.03f,.52f);
            float size=radius*formation*shrink;if(size<.01f)return;
            Vector3 position=Anchor.position;
            float age=owner.RealmAge,spin=age*4+owner.CollapseProgress*owner.CollapseProgress*220;
            properties.Clear();properties.SetColor("_Color",new Color(1,1,1,weight));properties.SetColor("_TintColor",new Color(1,1,1,weight));
            commands.DrawMesh(sphere,Matrix4x4.TRS(position,Quaternion.identity,Vector3.one*size),darkness,0,colorPass,properties);
            plasma.SetTextureOffset("_MainTex",new Vector2(-age*.035f-owner.CollapseProgress*.6f,0));
            plasmaVeil.SetTextureOffset("_MainTex",new Vector2(age*.017f+owner.CollapseProgress*.4f,.07f));
            properties.SetColor("_Color",new Color(1.6f,1.1f,2.0f,weight*.92f));properties.SetColor("_TintColor",new Color(1.6f,1.1f,2.0f,weight*.92f));
            commands.DrawMesh(disk,Matrix4x4.TRS(position,diskRotation*Quaternion.Euler(0,spin,0),Vector3.one*size),plasma,0,colorPass,properties);
            properties.SetColor("_Color",new Color(.55f,.75f,1.6f,weight*.24f));properties.SetColor("_TintColor",new Color(.55f,.75f,1.6f,weight*.24f));
            commands.DrawMesh(disk,Matrix4x4.TRS(position,diskRotation*Quaternion.Euler(0,-spin*.63f,0),Vector3.one*size*1.03f),plasmaVeil,0,colorPass,properties);
            Vector3 toward=camera.transform.position-position;float distance=toward.magnitude;
            if(distance<=size*1.1f)return;
            // Sphere silhouette perspective correction; only the thin rim faces the viewer.
            float rimScale=size*distance/Mathf.Sqrt(distance*distance-size*size);
            Quaternion facing=Quaternion.LookRotation(toward.normalized,Mathf.Abs(toward.normalized.y)>.98f?Vector3.forward:Vector3.up);
            properties.SetColor("_Color",new Color(1,1,1,weight));properties.SetColor("_TintColor",new Color(1,1,1,weight));
            Matrix4x4 rim=Matrix4x4.TRS(position,facing,Vector3.one*rimScale);
            commands.DrawMesh(halo,rim,glow,0,colorPass,properties);commands.DrawMesh(ring,rim,photon,0,colorPass,properties);
        }
        public void Dispose()
        {
            foreach(UnityEngine.Object item in new UnityEngine.Object[]{sphere,disk,ring,halo,darkness,plasma,plasmaVeil,photon,glow,flow})if(item)UnityEngine.Object.Destroy(item);
            if(Anchor)UnityEngine.Object.Destroy(Anchor.gameObject);
        }
    }
}
