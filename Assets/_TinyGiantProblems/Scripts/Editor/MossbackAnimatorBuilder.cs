using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TinyGiantProblems.Editor
{
    public static class MossbackAnimatorBuilder
    {
        const string Folder="Assets/_TinyGiantProblems/Art/MossbackRig/Animation";
        static readonly string[] Fields={"headYaw","neckYaw","headNod","neckNod","headRoll","bodyRoll","jawOpen","earFlap","tailWhip","reactionWeight"};
        static void Curve(AnimationClip clip,string field,params float[] pairs)
        {
            var keys=new Keyframe[pairs.Length/2];for(int i=0;i<keys.Length;i++)keys[i]=new Keyframe(pairs[i*2],pairs[i*2+1]);
            var curve=new AnimationCurve(keys);
            for(int i=0;i<keys.Length;i++) {AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(MossbackMotion),field),curve);
        }
        static void Wave(AnimationClip clip,string field,float amplitude,float phase,float length)
        {
            var data=new float[194];for(int i=0;i<=96;i++) {float t=i*length/96;float fade=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.35f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((length-t)/.65f));data[i*2]=t;data[i*2+1]=Mathf.Sin(t*Mathf.PI*2*3.1f+phase)*amplitude*fade;}Curve(clip,field,data);
        }
        static AnimationClip Clip(string name,float duration,bool loop=false)
        {
            var path=Folder+"/"+name+".anim";var c=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(!c) {c=new AnimationClip();AssetDatabase.CreateAsset(c,path);} else c.ClearCurves();
            c.name=name;c.frameRate=60;
            foreach(var field in Fields) Curve(c,field,0,field=="jawOpen"?2:0,duration,field=="jawOpen"?2:0);
            var settings=AnimationUtility.GetAnimationClipSettings(c);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(c,settings);return c;
        }
        public static void Apply()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var idle=Clip("LookAround",16,true);
            Curve(idle,"headYaw",0,0,2,18,4,20,6,0,8,-20,10,-18,12,0,14,8,16,0);
            Curve(idle,"neckYaw",0,0,2,5,4,6,6,0,8,-6,10,-5,12,0,14,2,16,0);
            Curve(idle,"headNod",0,0,4,-3,8,1,12,-2,16,0);
            Curve(idle,"jawOpen",0,2,1,4,2,1,3,4,4,2,6,1,8,3,10,1,12,4,14,1,16,2);
            var inhale=Clip("SneezeWindup",4);
            Curve(inhale,"neckNod",0,0,1,-2,2.7f,-7,3.4f,-6,4,-10);
            Curve(inhale,"headNod",0,0,1.4f,-3,2.6f,-9,3,-5,3.5f,-11,4,-13);
            Curve(inhale,"jawOpen",0,2,1.5f,3,2.5f,5,3,3,4,7);
            Curve(inhale,"earFlap",0,0,2,3,4,9);Curve(inhale,"reactionWeight",0,0,.3f,1,4,1);
            var sneeze=Clip("Sneeze",1.65f);
            Curve(sneeze,"neckNod",0,-10,.16f,10,.33f,4,.65f,-3,1.65f,0);
            Curve(sneeze,"headNod",0,-13,.14f,23,.3f,12,.55f,-6,1.05f,3,1.65f,0);
            Curve(sneeze,"jawOpen",0,7,.13f,8,.3f,3,.7f,2,1.65f,2);
            Curve(sneeze,"earFlap",0,9,.16f,-14,.32f,10,.55f,-5,.9f,2,1.65f,0);
            Curve(sneeze,"tailWhip",0,0,.3f,10,.7f,-5,1.65f,0);Curve(sneeze,"reactionWeight",0,1,1,1,1.65f,0);
            var ready=Clip("ShakeWindup",4);
            Curve(ready,"neckNod",0,0,2,3,4,5);Curve(ready,"headNod",0,0,2,4,4,7);
            Curve(ready,"headRoll",0,0,2.5f,-3,3.5f,4,4,-5);Curve(ready,"earFlap",0,0,2,-4,4,-8);
            Curve(ready,"reactionWeight",0,0,.4f,1,4,1);
            var shake=Clip("WetDogShake",3.4f);
            Wave(shake,"bodyRoll",3.1f,0,3.4f);Wave(shake,"neckYaw",6,.6f,3.4f);
            Wave(shake,"headRoll",19,1,3.4f);Wave(shake,"headYaw",11,1.2f,3.4f);
            Wave(shake,"earFlap",19,-.7f,3.4f);Wave(shake,"tailWhip",16,-1.6f,3.4f);
            Curve(shake,"headNod",0,7,.35f,4,2.7f,3,3.4f,0);Curve(shake,"reactionWeight",0,1,2.8f,1,3.4f,0);
            var controllerPath=Folder+"/Mossback.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine=controller.layers[0].stateMachine;
            foreach(var clip in new[]{idle,inhale,sneeze,ready,shake})
            {
                var state=machine.states.FirstOrDefault(s=>s.state.name==clip.name).state;
                if(!state)state=machine.AddState(clip.name);state.motion=clip;state.writeDefaultValues=true;
                foreach(var transition in state.transitions)state.RemoveTransition(transition);
                EditorUtility.SetDirty(clip);
            }
            var idleState=machine.states.First(s=>s.state.name=="LookAround").state;machine.defaultState=idleState;
            foreach(string name in new[]{"Sneeze","WetDogShake"})
            {
                var state=machine.states.First(s=>s.state.name==name).state;var transition=state.AddTransition(idleState);
                transition.hasExitTime=true;transition.exitTime=1;transition.hasFixedDuration=true;transition.duration=.25f;
            }
            var frame=Object.FindFirstObjectByType<CreatureFrame>();var motion=frame.GetComponent<MossbackMotion>();if(!motion)motion=frame.gameObject.AddComponent<MossbackMotion>();
            var animator=frame.GetComponent<Animator>();if(!animator)animator=frame.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;motion.animator=animator;
            AddPuff(motion,frame.GetComponentInChildren<MossbackSkeleton>());
            EditorUtility.SetDirty(controller);EditorUtility.SetDirty(motion);EditorUtility.SetDirty(animator);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(frame.gameObject.scene);EditorSceneManager.SaveOpenScenes();
        }
        static void AddPuff(MossbackMotion motion,MossbackSkeleton rig)
        {
            var old=rig.joints[2].Find("Sneeze pollen puff");if(old)Object.DestroyImmediate(old.gameObject);
            var go=new GameObject("Sneeze pollen puff");go.transform.SetParent(rig.joints[2],false);go.transform.localPosition=new Vector3(4.78f,-.16f,2.19f);go.transform.localRotation=Quaternion.LookRotation(new Vector3(.98f,0,.2f));
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.playOnAwake=false;main.duration=.2f;main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,.9f);main.startSpeed=new ParticleSystem.MinMaxCurve(18,30);main.startSize=new ParticleSystem.MinMaxCurve(.7f,1.6f);main.startColor=new Color(.83f,.88f,.65f,.6f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;main.maxParticles=60;
            var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,40)});
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=14;shape.radius=.14f;
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.6f,0),new GradientAlphaKey(0,1)});color.color=gradient;
            var texturePath=Folder+"/SoftPuff.png";
            if(!File.Exists(texturePath)) {var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);for(int y=0;y<32;y++)for(int x=0;x<32;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f;tex.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2)));}tex.Apply();File.WriteAllBytes(texturePath,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(texturePath);}
            var matPath=Folder+"/Sneeze mist.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(mat,matPath);}
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);mat.SetFloat("_SrcBlend",5);mat.SetFloat("_DstBlend",10);mat.SetFloat("_ZWrite",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=mat;EditorUtility.SetDirty(mat);motion.sneezePuff=ps;
        }
        public static void Sample(string state,float time)
        {
            var frame=Object.FindFirstObjectByType<CreatureFrame>();var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/"+state+".anim");clip.SampleAnimation(frame.gameObject,time);
        }
        public static void Preview(string state,float time,string suffix,int width=1400,int height=1000)
        {
            var frame=Object.FindFirstObjectByType<CreatureFrame>();var rig=frame.GetComponentInChildren<MossbackSkeleton>();var motion=frame.GetComponent<MossbackMotion>();
            var matrix=frame.transform.localToWorldMatrix;var position=frame.transform.position;var rotation=frame.transform.rotation;
            try
            {
                Sample(state,time);var pivot=frame.transform.TransformPoint(new Vector3(0,-5.5f,0));
                var turn=rotation*Quaternion.AngleAxis(motion.bodyRoll,new Vector3(.38f,0,.92f));
                frame.transform.SetPositionAndRotation(pivot-turn*Vector3.Scale(new Vector3(0,-5.5f,0),frame.transform.localScale),turn);
                rig.Pose(time);
                if(state=="Sneeze" && time>=.12f && time<.85f)motion.sneezePuff.Simulate(time-.12f,true,true,true);
                MossbackRigBuilder.Preview(time,suffix,width,height,matrix);
            }
            finally
            {
                motion.sneezePuff.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                frame.transform.SetPositionAndRotation(position,rotation);Sample("LookAround",0);rig.ResetPose();
            }
        }
    }
}
