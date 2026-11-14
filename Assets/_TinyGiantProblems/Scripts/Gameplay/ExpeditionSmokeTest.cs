using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TinyGiantProblems
{
    // Invoked explicitly from the Editor or development build. Never runs in normal play.
    public sealed class ExpeditionSmokeTest : MonoBehaviour
    {
        public string Status = "Not started";
        readonly List<string> checks = new List<string>();
        public void Run() { StartCoroutine(Verify()); }
        void Check(bool pass, string name)
        {
            checks.Add((pass ? "PASS " : "FAIL ") + name);
            if (!pass) Debug.LogError("Smoke test failed: " + name);
        }
        IEnumerator Verify()
        {
            Status = "Running";
            var g = Expedition.Instance; var p = g.player;
            g.Begin(); g.nextReaction = 9999; p.automate = true;
            yield return new WaitForSeconds(.5f);
            var local = g.creature.transform.InverseTransformPoint(p.transform.position);
            var frame = Time.frameCount;
            Time.timeScale = 5;
            yield return new WaitForSeconds(90);
            Time.timeScale = 1;
            Check(Time.frameCount > frame + 100, "Live player loop advances");
            Check(Vector3.Distance(local,g.creature.transform.InverseTransformPoint(p.transform.position)) < .25f && p.recoveries == 0, "90-second moving-frame attachment drift under 0.25 creature-local units");
            p.yaw = g.creature.transform.eulerAngles.y; p.MoveOverride = Vector3.forward;
            yield return new WaitForSeconds(1.2f);
            p.MoveOverride = Vector3.zero;
            Check(g.creature.transform.InverseTransformPoint(p.transform.position).z > local.z + 4/g.creature.transform.lossyScale.z, "Motor walks forward more than four world metres");
            float endZ=g.jobs[g.jobs.Length-1].transform.localPosition.z+.7f, until=Time.time+25;
            p.MoveOverride=Vector3.forward;
            while(g.creature.transform.InverseTransformPoint(p.transform.position).z<endZ && Time.time<until)
            { p.yaw=g.creature.transform.eulerAngles.y; yield return null; }
            p.MoveOverride=Vector3.zero;
            yield return new WaitForSeconds(.2f);
            Check(g.creature.transform.InverseTransformPoint(p.transform.position).z>=endZ && p.grounded && p.recoveries==0,"Entire modular route is walkable across every ramp and seam");
            p.Warp(g.creature.transform.TransformPoint(new Vector3(5.5f+.6f/g.creature.transform.lossyScale.x,6,0)));
            p.yaw=g.creature.transform.eulerAngles.y-90; p.MoveOverride=Vector3.forward; p.BraceOverride=true;
            yield return new WaitForSeconds(2.5f);
            Check(g.creature.transform.InverseTransformPoint(p.transform.position).y>6+4/g.creature.transform.lossyScale.y,"Grip climbs four world metres on the authored flank");
            p.MoveOverride=Vector3.zero; p.BraceOverride=false; p.Warp(p.checkpoint.position);
            p.grace=0; p.braced=true; p.velocity=Vector3.zero; p.Launch(new Vector3(4,10,5));
            Check(p.velocity == Vector3.zero, "Brace rejects launch");
            p.braced=false; p.Launch(new Vector3(40,100,50));
            Check(p.velocity.magnitude <= 15.001f && p.velocity.y > 0, "Unbraced impulse is bounded");
            p.Recover();
            Check(p.grace >= 5 && p.recoveries == 1, "Safety recovery grants reaction grace");
            p.ToggleTether(); Check(p.tethered, "Nearby authored socket accepts tether"); p.ToggleTether();
            var first=g.jobs[0];
            g.ApplyCare(first,CareTool.Sprayer,0,.1f);
            Check(first.state.Progress == 0,"Wrong tool cannot progress job");
            g.ApplyCare(first,first.tool,10,.1f);
            Check(first.state.Progress == 0,"Out-of-range care rejected");
            g.Finish(); Check(g.Phase != ExpeditionPhase.Debrief,"Premature extraction rejected");
            foreach(var job in g.jobs)
            {
                p.Warp(job.transform.position + new Vector3(0,.2f,-1));
                yield return new WaitForSeconds(.1f);
                for(int i=0;i<70;i++) g.ApplyCare(job,job.tool,Vector3.Distance(p.transform.position,job.transform.position),.1f);
            }
            Check(g.Completed==3 && g.Comfort==98,"Three care jobs complete with expected comfort");
            g.ApplyCare(first,first.tool,0,.1f); Check(g.Comfort==98,"Completed job cannot duplicate reward");
            g.Finish(); Check(g.Phase==ExpeditionPhase.Debrief,"Successful care reaches debrief");
            g.Begin(); g.nextReaction=9999;
            Check(g.Completed==0 && g.Comfort==38 && p.recoveries==0,"Replay resets mission and recovery state");
            g.Warn(); Check(g.WarningRemaining==4 && g.Reaction.Length>0,"Reaction has four-second warning");
            g.Begin(); g.Phase=ExpeditionPhase.Briefing; p.automate=false;
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            Directory.CreateDirectory("TestResults"); File.WriteAllLines("TestResults/expedition-smoke.txt",checks);
            Status=checks.Exists(x=>x.StartsWith("FAIL"))?"FAILED":"PASSED";
            Debug.Log("Expedition smoke test " + Status + "\n" + string.Join("\n",checks));
        }
    }
}
