using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

namespace TinyGiantProblems.Editor
{
    public static class MossbackBuilder
    {
        const string Root = "Assets/_TinyGiantProblems/";
        static Material moss, dark, cream, wood, gold, coral, teal;
        [MenuItem("Tiny Giant Problems/Build Mossback First Playable")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "Art/Materials"); Directory.CreateDirectory(Root + "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            moss = Mat("Moss", new Color(.28f,.46f,.28f)); dark = Mat("DeepForest", new Color(.07f,.19f,.19f));
            cream = Mat("Cream", new Color(.91f,.83f,.60f)); wood = Mat("HarnessWood", new Color(.36f,.19f,.11f));
            gold = Mat("GuildGold", new Color(.98f,.63f,.16f)); coral = Mat("Bramblemite", new Color(.75f,.23f,.23f)); teal = Mat("SoothingMint", new Color(.3f,.77f,.66f));
            var frame = new GameObject("Mossback - stable creature frame").AddComponent<CreatureFrame>();
            var visual = new GameObject("Mossback visual breathing rig").transform; visual.SetParent(frame.transform); frame.visual = visual;
            Shape("Gentle giant body", PrimitiveType.Sphere, visual, new Vector3(0,6.5f,0), new Vector3(16,12,29), moss);
            Shape("Neck",PrimitiveType.Sphere,visual,new Vector3(0,8,13),new Vector3(11,9,12),moss);
            Shape("Head",PrimitiveType.Sphere,visual,new Vector3(0,8,19),new Vector3(11,8,10),moss);
            Shape("Soft muzzle",PrimitiveType.Sphere,visual,new Vector3(0,6.8f,23),new Vector3(8,4.5f,4.5f),cream);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Sleepy eye",PrimitiveType.Sphere,visual,new Vector3(side*4.5f,9.3f,21),new Vector3(.9f,1.2f,.7f),dark);
                Shape("Eye glint",PrimitiveType.Sphere,visual,new Vector3(side*4.62f,9.6f,21.25f),Vector3.one*.25f,cream);
                Shape("Floppy ear",PrimitiveType.Sphere,visual,new Vector3(side*6,9,16),new Vector3(5,1.1f,3.8f),moss).transform.localRotation = Quaternion.Euler(0,0,side*25);
                Shape("Nostril",PrimitiveType.Sphere,visual,new Vector3(side*1.8f,7.5f,24.9f),new Vector3(1.1f,.6f,.3f),dark);
                foreach (float z in new[] {-8f,8f})
                {
                    Shape("Pillar leg",PrimitiveType.Capsule,visual,new Vector3(side*5,-.2f,z),new Vector3(3.8f,5,4),moss);
                    Shape("Soft paw",PrimitiveType.Sphere,visual,new Vector3(side*5,-4.4f,z+1),new Vector3(4.5f,2,5.5f),dark);
                }
            }
            Shape("Tail",PrimitiveType.Capsule,visual,new Vector3(0,5,-16),new Vector3(2,4,2),moss).transform.localRotation = Quaternion.Euler(55,0,0);
            var zones = new Transform[5];
            string[] names = {"Harness", "Hindquarters", "Spine", "Shoulder", "Head"};
            float[] zs = {-12,-6,1,8,15};
            for (int i=0;i<5;i++)
            {
                var zone = new GameObject(names[i]).AddComponent<ClimbableZone>(); zone.zoneName=names[i]; zone.transform.SetParent(frame.transform,false); zones[i]=zone.transform;
                var platform = Shape(names[i]+" traversal",PrimitiveType.Cube,zone.transform,new Vector3(0,11.6f,zs[i]),new Vector3(i==0?8:10,1,7.5f),i==0?wood:moss,true);
                if (i==0)
                {
                    for(int p=0;p<12;p++) Shape("Harness plank",PrimitiveType.Cube,zone.transform,new Vector3(0,12.16f,-15.5f+p*.62f),new Vector3(8,.12f,.5f),wood);
                    Shape("Guild supply crate",PrimitiveType.Cube,zone.transform,new Vector3(-2.7f,12.8f,-13),Vector3.one*1.2f,gold,true);
                }
                for(int s=-1;s<=1;s+=2)
                    Shape("Climbable flank",PrimitiveType.Cube,zone.transform,new Vector3(s*5,8,zs[i]),new Vector3(1,7,7.5f),moss,true);
            }
            Random.InitState(73);
            for(int i=0;i<240;i++)
            {
                float x=Random.Range(-7f,7f), z=Random.Range(-13f,16f);
                if(Mathf.Abs(x)<2.2f) continue;
                float y=12.2f-Mathf.Max(0,Mathf.Abs(x)-4.5f)*.8f;
                var tuft=Shape("Moss tuft",PrimitiveType.Capsule,visual,new Vector3(x,y,z),new Vector3(.35f,Random.Range(.4f,1f),.4f),i%4==0?teal:moss);
                tuft.transform.localRotation=Quaternion.Euler(Random.Range(-25,25),0,Random.Range(-25,25));
            }
            // Eight authored safety sockets, distinct from decorative fur.
            var anchors = new Transform[8];
            for(int i=0;i<8;i++)
            {
                anchors[i]=Shape("AnchorSocket_"+i,PrimitiveType.Cylinder,frame.transform,new Vector3(i%2==0?-3.8f:3.8f,12.7f,-10+i*3.3f),new Vector3(.35f,.6f,.35f),gold).transform;
                Shape("Anchor cap",PrimitiveType.Sphere,anchors[i],new Vector3(0,1,0),new Vector3(1.8f,.5f,1.8f),cream);
            }
            for(int i=0;i<22;i++) Shape("Guild trail",PrimitiveType.Cube,frame.transform,new Vector3(0,12.14f,-10+i*1.2f),new Vector3(.22f,.08f,.6f),gold);
            var game = new GameObject("Expedition Authority").AddComponent<Expedition>(); game.creature=frame;
            game.gameObject.AddComponent<CrewSession>();
            game.gameObject.AddComponent<CrewSmokeProbe>();
            game.jobs=new[] {
                Job(zones[1],"Collect Bramblemites","Hindquarters",CareTool.Vacuum,new Vector3(-2.5f,12.2f,-5),coral),
                Job(zones[2],"Untangle the fur","Spine",CareTool.Groomer,new Vector3(2.5f,12.2f,3),gold),
                Job(zones[4],"Soothe the hot spot","Head",CareTool.Sprayer,new Vector3(-2.5f,12.2f,15),teal)
            };
            var extract=Shape("Extraction beacon",PrimitiveType.Cylinder,zones[0],new Vector3(1.5f,12.2f,-13),new Vector3(2.5f,.07f,2.5f),teal).transform; game.extraction=extract;
            var checkpoint = new GameObject("HarnessCheckpoint").transform; checkpoint.SetParent(frame.transform,false); checkpoint.localPosition=new Vector3(0,12.3f,-12);
            var player = new GameObject("Guild Technician"); player.transform.position=checkpoint.position;
            var cc=player.AddComponent<CharacterController>(); cc.height=1.65f; cc.radius=.32f; cc.center=new Vector3(0,.85f,0); cc.stepOffset=.35f; cc.slopeLimit=55; cc.skinWidth=.04f;
            var motor=player.AddComponent<TechnicianMotor>(); motor.checkpoint=checkpoint; motor.anchors=anchors; game.player=motor;
            motor.model=new GameObject("Technician visual").transform; motor.model.SetParent(player.transform,false);
            Shape("Jacket",PrimitiveType.Capsule,motor.model,new Vector3(0,.8f,0),new Vector3(.65f,.55f,.48f),coral);
            Shape("Round helmet",PrimitiveType.Sphere,motor.model,new Vector3(0,1.48f,0),new Vector3(.76f,.58f,.7f),gold);
            Shape("Visor",PrimitiveType.Cube,motor.model,new Vector3(0,1.45f,.3f),new Vector3(.52f,.25f,.12f),dark);
            Shape("Backpack",PrimitiveType.Cube,motor.model,new Vector3(0,.9f,-.3f),new Vector3(.6f,.6f,.3f),teal);
            for(int s=-1;s<=1;s+=2)
            {
                Shape("Boot",PrimitiveType.Capsule,motor.model,new Vector3(s*.19f,.25f,.05f),new Vector3(.25f,.3f,.32f),dark);
                Shape("Glove",PrimitiveType.Sphere,motor.model,new Vector3(s*.4f,.8f,.1f),Vector3.one*.27f,cream);
            }
            var ropeGO=new GameObject("Safety tether"); motor.rope=ropeGO.AddComponent<LineRenderer>(); motor.rope.positionCount=2; motor.rope.startWidth=motor.rope.endWidth=.045f; motor.rope.sharedMaterial=gold; motor.rope.enabled=false;
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag="MainCamera"; camera.fieldOfView=65; camera.nearClipPlane=.1f; camera.farClipPlane=500; camera.allowHDR=true;
            camera.transform.SetPositionAndRotation(new Vector3(12,20,-28),Quaternion.Euler(20,-20,0)); camera.gameObject.AddComponent<AudioListener>(); motor.orbitCamera=camera;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var sun=new GameObject("Warm afternoon sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=2.2f; sun.color=new Color(1,.91f,.76f); sun.shadows=LightShadows.Soft; sun.transform.rotation=Quaternion.Euler(43,-35,0);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.52f,.65f,.7f); RenderSettings.fog=true; RenderSettings.fogColor=new Color(.60f,.76f,.75f); RenderSettings.fogDensity=.004f;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.60f,.76f,.75f);
            var volume=new GameObject("Storybook atmosphere").AddComponent<Volume>(); volume.isGlobal=true;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>(); profile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;
            var bloom=profile.Add<Bloom>(true); bloom.intensity.value=.35f; bloom.threshold.value=1;
            profile.Add<Vignette>(true).intensity.value=.18f;
            SaveAsset(profile,Root+"Art/StorybookVolume.asset"); volume.sharedProfile=profile;
            UniversalRenderPipeline.asset.supportsHDR=true;
            QualitySettings.SetQualityLevel(QualitySettings.names.Length-1,true); QualitySettings.vSyncCount=1;
            var meadow=new GameObject("Migratory meadow").transform;
            Shape("Meadow floor",PrimitiveType.Cube,meadow,new Vector3(0,-6,0),new Vector3(400,1,400),Mat("Meadow",new Color(.39f,.52f,.34f)),true);
            for(int i=0;i<65;i++)
            {
                float x=Random.Range(-120f,120f),z=Random.Range(-100f,160f); if(Mathf.Abs(x)<18 && Mathf.Abs(z)<35) continue;
                float h=Random.Range(6f,18f);
                Shape("Tree trunk",PrimitiveType.Cylinder,meadow,new Vector3(x,-5+h*.3f,z),new Vector3(1.2f,h*.3f,1.2f),wood);
                Shape("Round canopy",PrimitiveType.Sphere,meadow,new Vector3(x,-5+h*.8f,z),new Vector3(h*.8f,h*.9f,h*.8f),i%3==0?teal:moss);
            }
            for(int i=0;i<14;i++)
                Shape("Distant rolling hill",PrimitiveType.Sphere,meadow,new Vector3(Mathf.Cos(i*2.4f)*150,-10,Mathf.Sin(i*2.4f)*150),new Vector3(100,Random.Range(30,70),100),moss);
            for(int i=0;i<15;i++)
                Shape("Cloud",PrimitiveType.Sphere,meadow,new Vector3(Random.Range(-130,130),Random.Range(50,80),Random.Range(40,180)),new Vector3(30,5,12),cream);
            var hud=new GameObject("Expedition HUD",typeof(RectTransform)).AddComponent<ExpeditionHUD>();
            hud.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (!hud.font) throw new System.Exception("Import TMP essentials before building.");
            game.music=game.gameObject.AddComponent<AudioSource>(); game.music.clip=Clip("Music/MossbackMeadow"); game.music.loop=true; game.music.volume=.22f; game.music.playOnAwake=true;
            game.effects=game.gameObject.AddComponent<AudioSource>(); game.effects.playOnAwake=false;
            game.sneeze=Clip("SFX/Sneeze"); game.shake=Clip("SFX/Shake"); game.complete=Clip("SFX/CareComplete"); game.spray=Clip("SFX/Spray");
            PlayerSettings.productName="Tiny Giant Problems"; PlayerSettings.companyName="Small Works Guild"; PlayerSettings.runInBackground=true;
            PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900; PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            EditorSceneManager.SaveScene(scene,Root+"Scenes/Mossback_FirstPlayable.unity");
            EditorBuildSettings.scenes=new[] {new EditorBuildSettingsScene(Root+"Scenes/Mossback_FirstPlayable.unity",true)};
            AssetDatabase.SaveAssets();
            Debug.Log("Mossback first playable assembled: five zones, eight sockets, three jobs.");
        }
        static AudioClip Clip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/"+path+".mp3");
        static CareJob Job(Transform parent,string title,string zone,CareTool tool,Vector3 position,Material mat)
        {
            var go=new GameObject(title); go.transform.SetParent(parent,false); go.transform.localPosition=position;
            var job=go.AddComponent<CareJob>(); job.title=title; job.zone=zone; job.tool=tool;
            job.ring=Shape("Care site ring",PrimitiveType.Cylinder,go.transform,Vector3.zero,new Vector3(2.6f,.06f,2.6f),gold).GetComponent<Renderer>();
            job.visual=Shape("Care target",tool==CareTool.Groomer?PrimitiveType.Capsule:PrimitiveType.Sphere,go.transform,Vector3.up*.65f,new Vector3(1.2f,.9f,1.2f),mat).transform;
            if(tool==CareTool.Vacuum)
                for(int i=0;i<6;i++) Shape("Bramblemite foot",PrimitiveType.Sphere,job.visual,new Vector3(Mathf.Cos(i)*.6f,-.2f,Mathf.Sin(i)*.6f),Vector3.one*.3f,dark);
            return job;
        }
        static Material Mat(string name,Color color)
        {
            var path=Root+"Art/Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
            mat.color=color; mat.SetFloat("_Smoothness",.25f); return mat;
        }
        static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material,bool collider=false)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        static void SaveAsset(Object asset,string path) { AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(asset,path); }
        [MenuItem("Tiny Giant Problems/Build Windows Player")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"Builds/Windows/TinyGiantProblems.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Windows build failed: "+report.summary.result);
            Debug.Log("Windows build succeeded: "+report.summary.totalSize+" bytes");
        }
    }
}
