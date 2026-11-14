using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TinyGiantProblems
{
    public sealed class MossbackReactionSmokeTest : MonoBehaviour
    {
        public string Status="Not started";
        readonly List<string> checks=new List<string>();
        void Check(bool pass,string message){checks.Add((pass?"PASS ":"FAIL ")+message);if(!pass)Debug.LogError(message);}
        public void Run(){StartCoroutine(Verify());}
        IEnumerator Verify()
        {
            Status="Running";var g=Expedition.Instance;var rig=g.creature.GetComponentInChildren<MossbackSkeleton>();var m=g.creature.GetComponent<MossbackMotion>();var p=g.player;
            g.Begin();g.nextReaction=9999;p.automate=true;
            float min=100,max=-100,until=Time.time+16;int frame=Time.frameCount;Time.timeScale=4;
            while(Time.time<until){min=Mathf.Min(min,m.headYaw+m.neckYaw);max=Mathf.Max(max,m.headYaw+m.neckYaw);yield return null;}
            Time.timeScale=1;Check(min<-24 && max>24 && Time.frameCount>frame+100,"Live idle visibly turns head both left and right");
            g.Begin();g.nextReaction=9999;p.automate=true;p.BraceOverride=true;
            g.Warn();g.Reaction="SNEEZE";m.Warn(g.Reaction);
            yield return new WaitForSeconds(3.6f);
            Check(g.WarningRemaining>.15f && m.headNod<-5,"Sneeze inhales during the four-second warning");
            yield return new WaitForSeconds(.6f);
            Check(m.animator.GetCurrentAnimatorStateInfo(0).IsName("Sneeze") && m.headNod>5,"Sneeze snaps forward at impact");
            Check(m.sneezePuff.particleCount>0,"Nose puff emits on sneeze release");
            yield return new WaitForSeconds(2);
            Check(m.animator.GetCurrentAnimatorStateInfo(0).IsName("LookAround") && m.reactionWeight<.01f,"Sneeze settles back to idle");
            g.Begin();g.nextReaction=9999;p.automate=true;p.BraceOverride=true;
            g.Warn();g.Reaction="FULL-BODY SHAKE";m.Warn(g.Reaction);
            yield return new WaitForSeconds(4.2f);
            Check(m.animator.GetCurrentAnimatorStateInfo(0).IsName("WetDogShake"),"Shake warning transitions into distinct wet-dog animation");
            min=100;max=-100;float footError=0;until=Time.time+2.3f;var local=g.creature.transform.InverseTransformPoint(p.transform.position);
            while(Time.time<until)
            {
                min=Mathf.Min(min,m.bodyRoll);max=Mathf.Max(max,m.bodyRoll);
                for(int i=0;i<4;i++){var foot=rig.joints[11+i*3];float ground=Terrain.activeTerrain.SampleHeight(foot.position)+Terrain.activeTerrain.transform.position.y;footError=Mathf.Max(footError,Mathf.Abs(foot.position.y-ground-.27f*g.creature.transform.lossyScale.y));}
                yield return null;
            }
            Check(max-min>5,"Full body oscillates in both directions during shake");
            Check(footError<.1f,"Four hoof targets remain planted during shake");
            Check(p.recoveries==0 && Vector3.Distance(local,g.creature.transform.InverseTransformPoint(p.transform.position))<.35f,"Braced technician stays attached through shake");
            var mesh=new Mesh();rig.physicsSkin.BakeMesh(mesh,true);
            Check(Vector3.Distance(mesh.bounds.center,rig.bodyCollider.sharedMesh.bounds.center)<.02f,"Animated collision tracks the shaking pose");Destroy(mesh);
            Check(g.nextReaction>23 && g.nextReaction<=43,"Next random reaction is scheduled after recovery");
            yield return new WaitForSeconds(1.3f);
            Check(m.animator.GetCurrentAnimatorStateInfo(0).IsName("LookAround"),"Wet-dog shake returns to idle");
            g.SetPause(true);float timer=g.nextReaction;yield return new WaitForSecondsRealtime(.25f);Check(g.nextReaction==timer,"Random reaction countdown respects pause");g.SetPause(false);
            var randomState=Random.state;bool sneeze=false,shake=false;
            for(int i=0;i<64;i++){g.WarningRemaining=0;g.Warn();sneeze|=g.Reaction=="SNEEZE";shake|=g.Reaction=="FULL-BODY SHAKE";}
            Random.state=randomState;Check(sneeze && shake,"Random selection can choose both behaviors");
            m.Impact("FULL-BODY SHAKE");yield return new WaitForSeconds(.3f);g.Begin();yield return null;
            Check(g.WarningRemaining==0 && m.reactionWeight<.01f && m.animator.GetCurrentAnimatorStateInfo(0).IsName("LookAround"),"Replay cancels an active shake and restores idle");
            g.Phase=ExpeditionPhase.Briefing;p.automate=false;p.BraceOverride=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            Directory.CreateDirectory("TestResults");File.WriteAllLines("TestResults/mossback-reactions.txt",checks);Status=checks.Exists(x=>x.StartsWith("FAIL"))?"FAILED":"PASSED";Debug.Log("Mossback reaction tests "+Status+"\n"+string.Join("\n",checks));
        }
    }
}
