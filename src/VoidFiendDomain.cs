using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
namespace EventHorizon
{
    public sealed class VoidFiendDomain:MonoBehaviour
    {
        internal static readonly List<VoidFiendDomain> Live=new List<VoidFiendDomain>();
        private CharacterBody body;
        private HorizonCharge account;
        private DomainMessage cast,terminal,shown;
        private HorizonVisual visual;
        private uint sequence,shownSequence;
        private float began,shownAt,nextSync,requestAfter,serverRequestAfter,localCharge,impactAge;
        private bool ended=true,natural,shocked,finishing;
        private UnityEngine.SceneManagement.Scene scene;
        internal float MoveBonus,FallSpeed,HoleSize,HoleHeight;
        private float damage,knockback,visualRadius;
        private Vector3 visualOffset,diskAngles;
        internal CharacterBody Body {get{return body;}}
        private bool Alive {get{return body&&body.healthComponent&&body.healthComponent.alive;}}
        internal bool Active {get{return isActiveAndEnabled&&NetworkServer.active&&Alive&&Run.instance&&cast!=null&&!cast.Ended&&scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene()&&HorizonRules.Active(Time.time-began,cast.Freeze,cast.Duration);}}
        internal Vector3 Center {get{return cast.Center;}}
        internal Vector3 PullCenter {get{return cast.Center+visualOffset;}}
        internal float BlackHoleRadius {get{return visualRadius;}}
        internal uint CastId {get{return cast==null?0:cast.CastId;}}
        internal float Radius {get{return cast.Radius;}}
        internal bool Attuned {get{return Alive&&Run.instance&&scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene()&&(NetworkServer.active?Active:!ended&&shown!=null&&HorizonRules.Active(Time.time-shownAt,shown.Freeze,shown.Duration));}}
        internal bool Contains(Vector3 p,TeamIndex team)
        {return Active&&body.teamComponent&&TeamManager.IsTeamEnemy(body.teamComponent.teamIndex,team);}
        private void Awake(){body=GetComponent<CharacterBody>();scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();Live.Add(this);}
        private HorizonCharge Account(){if(!account&&body&&body.master){account=body.master.GetComponent<HorizonCharge>();if(!account)account=body.master.gameObject.AddComponent<HorizonCharge>();}return account;}
        internal void AddCharge(float damage)
        {if(NetworkServer.active&&Alive&&cast==null&&Account())account.Fraction=ChargeRules.Add(account.Fraction,damage,body.baseDamage+body.levelDamage*Mathf.Max(0,body.level-1),HorizonSettings.Charge.Value);}
        internal void ActivateServer()
        {
            if(finishing||!NetworkServer.active||!Alive||!Run.instance||!Account()||!body.teamComponent||Time.unscaledTime<serverRequestAfter)return;
            if(cast!=null||account.Fraction<1)return;
            serverRequestAfter=Time.unscaledTime+.5f;EndServer(false);ClearPresentation();
            account.Fraction=0;
            terminal=null;natural=shocked=false;impactAge=0;
            MoveBonus=HorizonSettings.MoveBonus.Value;FallSpeed=HorizonSettings.FallSpeed.Value;
            HoleSize=HorizonSettings.HoleSize.Value;HoleHeight=HorizonSettings.HoleHeight.Value;
            damage=HorizonSettings.Damage.Value;knockback=HorizonSettings.Knockback.Value;
            visualRadius=HorizonSettings.VisualRadius.Value;
            visualOffset=new Vector3(HorizonSettings.VisualX.Value,Mathf.Max(HorizonSettings.VisualHeight.Value,visualRadius*3.4f+6),HorizonSettings.VisualZ.Value);
            diskAngles=new Vector3(HorizonSettings.DiskPitch.Value,HorizonSettings.DiskYaw.Value,HorizonSettings.DiskRoll.Value);
            began=Time.time;scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            cast=new DomainMessage{Body=body.netId,Center=body.footPosition,Radius=HorizonSettings.Radius.Value,Duration=HorizonSettings.Duration.Value,
                Freeze=HorizonSettings.Windup.Value,Expansion=HorizonSettings.Opening.Value,Collapse=HorizonSettings.Collapse.Value,CastId=++sequence};
            Send();HorizonCombat.Refresh();HorizonProjectiles.Refresh();nextSync=Time.time+.5f;Plugin.Log.LogInfo("Event Horizon activated: "+sequence+"; anchored="+cast.Center+"; map-wide; influence scale="+cast.Radius+"; active="+cast.Duration+"; expansion="+cast.Expansion+"; pull target="+PullCenter);
        }
        private void Update()
        {
            if(!Alive||!Run.instance||scene!=UnityEngine.SceneManagement.SceneManager.GetActiveScene()){
                if(NetworkServer.active)EndServer(false);ClearPresentation();return;
            }
            if(!body.hasEffectiveAuthority||Time.timeScale<=0||RoR2.UI.ConsoleWindow.IsShown()||RoR2.UI.ChatBox.IsActive||Cursor.visible||Time.unscaledTime<requestAfter)return;
            if(ended&&localCharge>=1&&Input.GetKeyDown(HorizonSettings.Key.Value)){requestAfter=Time.unscaledTime+.5f;HorizonSystem.Request(body);}
        }
        private void FixedUpdate()
        {
            if(!NetworkServer.active)return;
            if(!Alive||!Run.instance||scene!=UnityEngine.SceneManagement.SceneManager.GetActiveScene())EndServer(false);
            else {
                if(cast!=null){
                    if(Time.time-began>=cast.Freeze+cast.Duration)EndServer(true);
                    else if(Active)HorizonContent.Sync(body,began+cast.Freeze+cast.Duration-Time.time);
                }
                if(natural&&!shocked&&terminal!=null&&Time.time-began>=impactAge+terminal.Collapse*.42f){shocked=true;HorizonCombat.Shock(body,terminal.Center,terminal.Radius,knockback,damage);}
            }
            if(Time.time>=nextSync&&Account()){Send();nextSync=Time.time+.5f;}
        }
        private void Send()
        {
            DomainMessage data=cast??terminal??new DomainMessage{Body=body.netId,CastId=sequence,Ended=true};
            if(cast!=null||terminal!=null)data.Age=Time.time-began;
            HorizonSystem.Broadcast(this,new HorizonState{Cast=data,Charge=Account()?account.Fraction:0,MoveBonus=MoveBonus,FallSpeed=FallSpeed,
                HoleSize=HoleSize,HoleHeight=HoleHeight,ImpactAge=impactAge,Shattered=natural,Slow=HorizonSettings.Slow.Value,AttackSlow=HorizonSettings.AttackSlow.Value,VisualRadius=visualRadius,VisualOffset=visualOffset,DiskAngles=diskAngles});
        }
        internal void Show(HorizonState state)
        {
            DomainMessage data=state.Cast;if(data.CastId<shownSequence)return;
            localCharge=Mathf.Clamp01(state.Charge);MoveBonus=state.MoveBonus;FallSpeed=state.FallSpeed;
            HorizonContent.MoveReduction=state.Slow;HorizonContent.AttackReduction=state.AttackSlow;
            if(data.CastId>shownSequence){
                ClearPresentation();shownSequence=data.CastId;shown=data;shownAt=Time.time-data.Age;scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();ended=false;
                bool visible=!data.Ended||(state.Shattered&&data.Age-state.ImpactAge<data.Collapse);
                if(visible&&!Application.isBatchMode)try{visual=HorizonVisual.Create(data,state.HoleSize,state.HoleHeight,state.VisualOffset,state.VisualRadius,state.DiskAngles);}catch(Exception e){Plugin.Log.LogError("Event Horizon presentation: "+e);}
            }
            if(data.Ended){ended=true;if(visual){if(state.Shattered)visual.BeginCollapse(Mathf.Max(0,data.Age-state.ImpactAge));else{Destroy(visual.gameObject);visual=null;}}}
        }
        private void EndServer(bool expiry)
        {
            if(finishing)return;
            if(!expiry){natural=false;shocked=true;}
            if(cast==null){HorizonContent.Remove(body);return;}
            finishing=true;
            try{
                terminal=cast;terminal.Ended=true;impactAge=Time.time-began;natural=expiry;shocked=!expiry;cast=null;
                HorizonContent.Remove(body);HorizonCombat.Refresh();HorizonProjectiles.Refresh();Send();
            }finally{finishing=false;}
        }
        private void ClearPresentation(){ended=true;if(visual)Destroy(visual.gameObject);visual=null;shown=null;}
        private void OnDisable(){if(NetworkServer.active)EndServer(false);ClearPresentation();}
        private void OnDestroy(){Live.Remove(this);}
        private void OnGUI()
        {
            if(!Alive||!body.hasEffectiveAuthority)return;
            float age=Time.time-shownAt,opening=shown==null?0:shown.Freeze+shown.Expansion;
            float remaining=!ended&&shown!=null?Mathf.Max(0,shown.Duration-Mathf.Max(0,age-shown.Freeze)):0;
            string status=remaining>0?(age<shown.Freeze?"WIND-UP":age<opening?"REALITY DISTORTS":Mathf.CeilToInt(remaining)+"s | SINGULARITY ATTUNEMENT"):localCharge>=1?"READY ["+HorizonSettings.Key.Value+"]":Mathf.FloorToInt(localCharge*100)+"% - Deal damage";
            float x=Screen.width*.5f-220,y=Screen.height-170;GUI.Box(new Rect(x,y,440,50),"EVENT HORIZON | "+status);
            Color saved=GUI.color;GUI.color=new Color(.035f,.015f,.08f);GUI.DrawTexture(new Rect(x+12,y+29,416,10),Texture2D.whiteTexture);
            GUI.color=new Color(.62f,.20f,.95f);GUI.DrawTexture(new Rect(x+12,y+29,416*(remaining>0?Mathf.Clamp01(remaining/shown.Duration):localCharge),10),Texture2D.whiteTexture);GUI.color=saved;

        }
    }
}
