using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TinyGiantProblems
{
    /// <summary>Direct-IP prototype adapter. Mission state stays host-owned; motors are client-predicted.</summary>
    public sealed class CrewSession : MonoBehaviour
    {
        [Serializable] public class Pose { public ulong id; public Vector3 position; public float yaw; public int tool; public bool braced, tethered; public int job=-1; public bool extract; }
        [Serializable] public class Snapshot { public Pose[] crew; public float[] progress; public float comfort, irritation, elapsed, clock, warning; public int phase, epoch; public string reaction; }
        NetworkManager manager;
        UnityTransport transport;
        Expedition game;
        ExpeditionHUD hud;
        readonly Dictionary<ulong,Pose> poses=new Dictionary<ulong,Pose>();
        readonly Dictionary<ulong,Transform> models=new Dictionary<ulong,Transform>();
        readonly Dictionary<ulong,float> lastCare=new Dictionary<ulong,float>();
        float sendAt, connectDeadline;
        int epoch, receivedEpoch=-1;
        bool connectedOnce;
        public bool Online => manager && manager.IsListening;
        public bool Host => Online && manager.IsServer;
        public int CrewCount => manager ? manager.ConnectedClientsIds.Count : 0;
        void Start()
        {
            game=Expedition.Instance; hud=FindAnyObjectByType<ExpeditionHUD>();
            game.MissionBegan=()=> { if(Host) epoch++; };
            var obj=new GameObject("Crew network transport");
            manager=obj.AddComponent<NetworkManager>(); transport=obj.AddComponent<UnityTransport>();
            manager.NetworkConfig=new NetworkConfig { NetworkTransport=transport, EnableSceneManagement=false, ConnectionApproval=true, TickRate=20 };
            manager.ConnectionApprovalCallback=(request,response)=> { response.Approved=manager.ConnectedClientsIds.Count<4; response.CreatePlayerObject=false; response.Pending=false; response.Reason="Crew is full (4 technicians)."; };
            manager.OnClientConnectedCallback+=OnConnected; manager.OnClientDisconnectCallback+=OnDisconnected;
            hud.HostRequested=StartHost; hud.JoinRequested=()=>StartClient(hud.address.text.Trim());
            var args=Environment.GetCommandLineArgs();
            if(args.Contains("--tgp-host")) StartHost();
            if(args.Contains("--tgp-client")) StartClient("127.0.0.1");
        }
        public void StartHost()
        {
            Shutdown(); transport.SetConnectionData("127.0.0.1",7777,"0.0.0.0");
            if(!manager.StartHost()) { hud.NetworkStatus="Host could not start"; return; }
            Register(); game.CareRequested=HostCare; game.IsRemote=false; epoch++; game.Begin();
            hud.NetworkStatus="Hosting crew · UDP 7777";
        }
        public void StartClient(string address)
        {
            if(!System.Net.IPAddress.TryParse(address,out _)) { hud.NetworkStatus="Enter the host's IPv4 address"; return; }
            Shutdown(); transport.SetConnectionData(address,7777);
            if(!manager.StartClient()) { hud.NetworkStatus="Could not start client"; return; }
            Register(); game.IsRemote=true; game.CareRequested=(_,__)=>{};
            game.ExtractionRequested=()=> { var p=LocalPose(); p.extract=true; Send("tgp.pose",NetworkManager.ServerClientId,p); };
            connectDeadline=Time.unscaledTime+12;
            hud.NetworkStatus="Connecting to "+address;
        }
        void Register()
        {
            manager.CustomMessagingManager.RegisterNamedMessageHandler("tgp.pose",OnPose);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("tgp.world",OnWorld);
        }
        void OnConnected(ulong id)
        {
            if(id==manager.LocalClientId) connectedOnce=true;
            if(!manager.IsServer && id==manager.LocalClientId) { game.Begin(true); hud.NetworkStatus="Connected to crew"; }
            Debug.Log("TGP_CREW_CONNECTED "+id);
        }
        void OnDisconnected(ulong id)
        {
            poses.Remove(id); lastCare.Remove(id);
            if(models.TryGetValue(id,out var model)) { Destroy(model.gameObject); models.Remove(id); }
            if(!manager.IsServer && id==manager.LocalClientId)
            {
                game.CareRequested=null; game.IsRemote=false; game.SetPause(false); game.Phase=ExpeditionPhase.Briefing;
                Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
                hud.NetworkStatus=connectedOnce?"Host disconnected · return to solo or reconnect":"Connection failed";
            }
        }
        public void Shutdown()
        {
            if(manager && manager.IsListening) manager.Shutdown();
            foreach(var model in models.Values) if(model) Destroy(model.gameObject);
            models.Clear(); poses.Clear(); lastCare.Clear(); connectedOnce=false; receivedEpoch=-1;
            if(game) { game.IsRemote=false; game.CareRequested=null; game.ExtractionRequested=null; }
        }
        void OnDestroy() { Shutdown(); if(manager) Destroy(manager.gameObject); }
        Pose LocalPose()
        {
            var p=game.player;
            return new Pose { id=manager.LocalClientId, position=game.creature.transform.InverseTransformPoint(p.transform.position), yaw=p.model.eulerAngles.y,
                tool=(int)p.selected, braced=p.braced, tethered=p.tethered,
                job=p.Using && game.Nearest && !p.braced && !p.climbing && !game.Paused?Array.IndexOf(game.jobs,game.Nearest):-1,
                extract=game.Phase==ExpeditionPhase.Extraction && p.InteractPressed };
        }
        void Update()
        {
            if(!Online) return;
            if(!Host && !manager.IsConnectedClient)
            {
                if(Time.unscaledTime>connectDeadline) { Shutdown(); hud.NetworkStatus="Connection timed out · check host IP"; }
                return;
            }
            if(Time.unscaledTime<sendAt) return;
            sendAt=Time.unscaledTime+.05f;
            var pose=LocalPose();
            if(Host)
            {
                poses[manager.LocalClientId]=pose;
                var snapshot=new Snapshot { crew=poses.Values.ToArray(), progress=game.jobs.Select(j=>j.state.Progress).ToArray(), comfort=game.Comfort,irritation=game.Irritation,elapsed=game.Elapsed,clock=game.creature.clock,warning=game.WarningRemaining,reaction=game.Reaction,phase=(int)game.Phase,epoch=epoch };
                foreach(var id in manager.ConnectedClientsIds) if(id!=manager.LocalClientId) Send("tgp.world",id,snapshot);
                hud.NetworkStatus="Crew "+manager.ConnectedClientsIds.Count+" / 4 · host";
            }
            else Send("tgp.pose",NetworkManager.ServerClientId,pose);
        }
        void HostCare(CareJob job,float dt) => game.ApplyCare(job,game.player.selected,Vector3.Distance(game.player.transform.position,job.transform.position),dt);
        void OnPose(ulong sender,FastBufferReader reader)
        {
            if(!Host) return;
            reader.ReadValueSafe(out string json); var pose=JsonUtility.FromJson<Pose>(json); if(pose==null) return;
            if(!float.IsFinite(pose.position.x)||!float.IsFinite(pose.position.y)||!float.IsFinite(pose.position.z)||pose.position.sqrMagnitude>40000) return;
            pose.id=sender; poses[sender]=pose; ShowPose(pose);
            if(pose.job>=0 && pose.job<game.jobs.Length && !pose.braced && pose.tool>=0 && pose.tool<4)
            {
                float now=Time.unscaledTime; float elapsed=lastCare.TryGetValue(sender,out var then)?Mathf.Min(.1f,now-then):.05f;
                var job=game.jobs[pose.job];
                game.ApplyCare(job,(CareTool)pose.tool,Vector3.Distance(game.creature.transform.TransformPoint(pose.position),job.transform.position),elapsed);
                lastCare[sender]=now;
            }
            else lastCare[sender]=Time.unscaledTime;
            if(pose.extract && Vector3.Distance(game.creature.transform.TransformPoint(pose.position),game.extraction.position)<3.5f) game.Finish();
        }
        void OnWorld(ulong sender,FastBufferReader reader)
        {
            if(Host || sender!=NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out string json); var s=JsonUtility.FromJson<Snapshot>(json); if(s==null||s.progress==null||s.progress.Length!=game.jobs.Length)return;
            if(receivedEpoch!=s.epoch) { receivedEpoch=s.epoch; game.Begin(true); }
            if(s.warning>0 && game.WarningRemaining<=0) { game.creature.GetComponent<MossbackMotion>()?.Warn(s.reaction,4-s.warning); game.Caption="[Creature warning] Hold F / B to brace!"; }
            if(s.warning<=0 && game.WarningRemaining>0) game.Impact();
            game.Comfort=s.comfort; game.Irritation=s.irritation; game.Elapsed=s.elapsed; game.creature.clock=s.clock; game.WarningRemaining=s.warning; game.Reaction=s.reaction;
            for(int i=0;i<s.progress.Length;i++)
            {
                if(s.progress[i]>=1 && !game.jobs[i].state.Complete) { game.effects.PlayOneShot(game.complete,.8f); game.Caption="Care complete: "+game.jobs[i].title; }
                game.jobs[i].state.Progress=s.progress[i];
            }
            var phase=(ExpeditionPhase)s.phase;
            if(phase==ExpeditionPhase.Debrief && game.Phase!=phase) game.Finish(true);
            game.Phase=phase;
            foreach(var p in s.crew) if(p.id!=manager.LocalClientId) ShowPose(p);
            foreach(var id in models.Keys.ToArray()) if(!s.crew.Any(p=>p.id==id)) { Destroy(models[id].gameObject); models.Remove(id); }
            hud.NetworkStatus="Crew "+s.crew.Length+" / 4 · connected";
        }
        void ShowPose(Pose pose)
        {
            if(!models.TryGetValue(pose.id,out var model))
            {
                model=Instantiate(game.player.model,game.creature.transform); model.name="Crewmate "+pose.id; models[pose.id]=model;
            }
            model.localPosition=pose.position; model.rotation=Quaternion.Euler(0,pose.yaw,0);
            var parentScale=game.creature.transform.lossyScale;
            model.localScale=new Vector3(1/parentScale.x,(pose.braced?.65f:1)/parentScale.y,1/parentScale.z);
            var tools=model.GetComponentInChildren<TechnicianToolVisuals>();
            if(tools) { tools.motor=null; tools.Show((CareTool)pose.tool); }
        }
        void Send(string name,ulong client,object value)
        {
            var json=JsonUtility.ToJson(value);
            using(var writer=new FastBufferWriter(16384,Allocator.Temp))
            { writer.WriteValueSafe(json); manager.CustomMessagingManager.SendNamedMessage(name,client,writer,NetworkDelivery.ReliableFragmentedSequenced); }
        }
    }
}
