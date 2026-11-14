using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TinyGiantProblems
{
    // Development-only opt-in multi-process probe; normal players never execute it.
    public sealed class CrewSmokeProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();
            if(!Debug.isDebugBuild || !args.Contains("--tgp-probe")) yield break;
            string output=Path.Combine(Application.persistentDataPath,"crew-probe-"+System.Diagnostics.Process.GetCurrentProcess().Id+".txt");
            float until=Time.realtimeSinceStartup+30;
            while((!NetworkManager.Singleton || !NetworkManager.Singleton.IsConnectedClient) && Time.realtimeSinceStartup<until) yield return null;
            if(!NetworkManager.Singleton || !NetworkManager.Singleton.IsConnectedClient) { File.WriteAllText(output,"FAIL connection timeout"); Application.Quit(2); yield break; }
            var game=Expedition.Instance; game.player.automate=true;
            File.WriteAllText(output,"PASS connected client "+NetworkManager.Singleton.LocalClientId+"\n");
            yield return new WaitForSeconds(3);
            File.AppendAllText(output,"phase="+game.Phase+" comfort="+game.Comfort+"\n");
            float initialElapsed=game.Elapsed;
            if(args.Contains("--tgp-work"))
            {
                game.player.Warp(game.jobs[0].transform.position+new Vector3(0,.2f,-1));
                game.player.selected=CareTool.Vacuum; game.player.UseOverride=true;
            }
            yield return new WaitForSeconds(12);
            game.player.UseOverride=false;
            bool pass=NetworkManager.Singleton.IsConnectedClient && game.Elapsed>initialElapsed+8 && (!args.Contains("--tgp-work") || game.jobs[0].state.Complete && game.Comfort==58);
            File.AppendAllText(output,"progress="+game.Completed+" comfort="+game.Comfort+" elapsed="+game.Elapsed+"\n"+(pass?"PASS":"FAIL")+" synchronized state and shared care\n");
            Debug.Log("TGP_PROBE_COMPLETE "+output);
        }
    }
}
