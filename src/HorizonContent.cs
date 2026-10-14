using System.Collections;
using System.Reflection;
using HarmonyLib;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.Networking;
namespace EventHorizon
{
    internal sealed class HorizonContent:IContentPackProvider
    {
        internal static BuffDef Buff,Corruption;
        internal static float MoveReduction=.2f,AttackReduction=.15f;
        private readonly ContentPack pack=new ContentPack();
        public string identifier {get{return "wreckzog.eventhorizon";}}
        internal static void Register(ContentManager.AddContentPackProviderDelegate add){add(new HorizonContent());}
        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            Texture2D texture=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Singularity Attunement",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            Color[] pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){
                Vector2 p=new Vector2((x-31.5f)/32,(y-31.5f)/32);float r=p.magnitude,disk=new Vector2(p.x,p.y*3).magnitude;
                float a=Mathf.Exp(-Mathf.Abs(r-.40f)*65)+Mathf.Exp(-Mathf.Abs(disk-.70f)*25)*.7f;
                pixels[y*64+x]=r<.36f?new Color(.008f,.002f,.02f,1):new Color(.55f+a*.35f,.18f+a*.45f,.94f,Mathf.Clamp01(a));
            }
            texture.SetPixels(pixels);texture.Apply(false,false);
            Sprite icon=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),64);
            Buff=Create("SINGULARITY ATTUNEMENT",icon,false);Corruption=Create("VOID GRAVITATIONAL EXPOSURE",icon,true);
            pack.buffDefs.Add(new BuffDef[]{Buff,Corruption});args.ReportProgress(1);yield break;
        }
        private static BuffDef Create(string name,Sprite icon,bool debuff)
        {
            BuffDef b=ScriptableObject.CreateInstance<BuffDef>();b.name=name;b.iconSprite=icon;b.buffColor=Color.white;b.canStack=debuff;b.isDebuff=debuff;
            b.isHidden=false;b.ignoreGrowthNectar=true;b.flags=BuffDef.Flags.ExcludeFromNoxiousThorns|BuffDef.Flags.ExcludeFromDreamCrucible;return b;
        }
        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args){ContentPack.Copy(pack,args.output);args.ReportProgress(1);yield break;}
        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args){args.ReportProgress(1);yield break;}
        internal static void Sync(CharacterBody body,float remaining)
        {
            if(!NetworkServer.active||!body||!Buff||Buff.buffIndex==BuffIndex.None||remaining<=0)return;
            if(!body.HasBuff(Buff))body.AddTimedBuff(Buff,remaining);body.SetTimedBuffDurationIfPresent(Buff,remaining,true);
        }
        internal static void Remove(CharacterBody body){if(NetworkServer.active&&body&&Buff&&Buff.buffIndex!=BuffIndex.None)body.ClearTimedBuffs(Buff);}
        internal static void Exposure(CharacterBody body,int count)
        {
            if(!NetworkServer.active||!body||!Corruption||Corruption.buffIndex==BuffIndex.None)return;
            if(body.GetBuffCount(Corruption)!=count){body.SetBuffCount(Corruption.buffIndex,count);body.MarkAllStatsDirty();}
        }
        [HarmonyPatch(typeof(CharacterBody),"RecalculateStats")]
        private static class Stats
        {
            private static readonly PropertyInfo Move=AccessTools.Property(typeof(CharacterBody),"moveSpeed"),Attack=AccessTools.Property(typeof(CharacterBody),"attackSpeed");
            private static void Postfix(CharacterBody __instance)
            {
                if(Corruption){float exposure=Mathf.Clamp01(__instance.GetBuffCount(Corruption)/5f);if(exposure>0){Move.SetValue(__instance,__instance.moveSpeed*(1-MoveReduction*exposure),null);Attack.SetValue(__instance,__instance.attackSpeed*(1-AttackReduction*exposure),null);}}
                if(!Buff||!__instance.HasBuff(Buff))return;VoidFiendDomain domain=__instance.GetComponent<VoidFiendDomain>();
                if(domain&&domain.Attuned)Move.SetValue(__instance,__instance.moveSpeed*(1+domain.MoveBonus),null);
            }
        }
        [HarmonyPatch(typeof(CharacterMotor),"PreMove")]
        private static class AttunedFall
        {
            private static void Postfix(CharacterMotor __instance)
            {
                if(!__instance.hasEffectiveAuthority||__instance.isGrounded)return;
                VoidFiendDomain domain=__instance.GetComponent<VoidFiendDomain>();if(!domain||!domain.Attuned)return;
                Vector3 v=__instance.velocity;if(v.y < -domain.FallSpeed){v.y=-domain.FallSpeed;__instance.velocity=v;}
            }
        }
    }
}
