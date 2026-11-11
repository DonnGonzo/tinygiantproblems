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
            yield return new WaitForSeconds(12);
            File.AppendAllText(output,"progress="+game.Completed+" comfort="+game.Comfort+" elapsed="+game.Elapsed+"\nPASS remained connected="+NetworkManager.Singleton.IsConnectedClient+"\n");
            Debug.Log("TGP_PROBE_COMPLETE "+output);
        }
    }
}
