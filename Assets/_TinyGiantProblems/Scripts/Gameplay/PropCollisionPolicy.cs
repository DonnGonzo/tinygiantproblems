using UnityEngine;
namespace TinyGiantProblems
{
    public sealed class PropCollisionPolicy : MonoBehaviour
    {
        public Collider[] surfaces;
        public bool traversalArt;
        void Awake(){Apply();}
        public void Apply()
        {
            bool worn=GetComponentInParent<TechnicianMotor>() || GetComponentInParent<TechnicianToolVisuals>();
            bool authoredTraversal=traversalArt && GetComponentInParent<ClimbableZone>();
            // Equipped visuals must never collide with their own technician.
            // Traversal already has fitted deck/climbing colliders.
            if(worn || authoredTraversal)foreach(var c in surfaces)if(c)c.enabled=false;
        }
    }
}
