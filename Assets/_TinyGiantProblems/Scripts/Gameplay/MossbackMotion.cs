using UnityEngine;
using System.Collections;

namespace TinyGiantProblems
{
    // Animator clips drive these pose controls; frame and skeleton consume the
    // same sampled values before the technician's movement update.
    public sealed class MossbackMotion : MonoBehaviour
    {
        public Animator animator;
        public ParticleSystem sneezePuff;
        public float headYaw,neckYaw,headNod,neckNod,headRoll,bodyRoll,jawOpen=2,earFlap,tailWhip,reactionWeight;
        Coroutine puffRoutine;
        public void Warn(string reaction,float elapsed=0)
        {
            if(!animator) return;
            string state=reaction=="SNEEZE"?"SneezeWindup":"ShakeWindup";
            if(elapsed>0) animator.Play(state,0,Mathf.Clamp01(elapsed/4));
            else animator.CrossFadeInFixedTime(state,.18f);
        }
        public void Impact(string reaction)
        {
            if(animator) animator.CrossFadeInFixedTime(reaction=="SNEEZE"?"Sneeze":"WetDogShake",.06f);
            if(puffRoutine!=null) StopCoroutine(puffRoutine);
            if(reaction=="SNEEZE" && sneezePuff) puffRoutine=StartCoroutine(PuffAfterSnap());
        }
        IEnumerator PuffAfterSnap(){yield return new WaitForSeconds(.12f);sneezePuff.Play(true);puffRoutine=null;}
        public void ResetMotion()
        {
            if(animator) {animator.Play("LookAround",0,0);animator.Update(0);}
            if(puffRoutine!=null) {StopCoroutine(puffRoutine);puffRoutine=null;}
            if(sneezePuff) sneezePuff.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
