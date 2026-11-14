# Mossback skeletal animation

The stronger head turns and distinct sneeze/shake Animator states are described in [Mossback Animator Behaviors](Mossback-Animator-Behaviors.md), which supersedes the initial small-motion tuning below.

The supplied GLB is preserved. `MossbackRigBuilder.Apply()` creates a 21-joint skeleton and two skinned mesh assets from the imported geometry: the original detailed surface (467,526 imported vertices) and the simplified physics surface. All skin weights are normalized with at most four influences per vertex. The static renderer and original static body collider are disabled; the animated renderer and collider replace them.

Joints: spine, neck, head, jaw, two ears, three tail joints, and hip/knee/foot chains for all four legs. Joint placement follows the model's staggered stance and turned head. The upper back remains rooted to preserve the authored harness and traversal surfaces.

`MossbackSkeleton` drives slow head looks and nods, subtle closed-mouth chewing, staggered ear flicks, tail sway, and one-hoof-at-a-time lifts and settling. Two-bone leg positioning adjusts the feet to the meadow height. This is a living stationary idle, not a traveling walk. The closed source mouth has no modeled oral cavity, so the jaw movement is restrained rather than exposing a nonexistent interior.

The physics mesh is skinned and baked at 20 Hz before technician movement; the render mesh uses normal GPU skinning. BakeMesh scale compensation preserves the existing 3x creature scale. Existing whole-frame breathing and reactions continue around the articulated motion.

Reapply with `MossbackRigBuilder.Apply()`; the standard model application also invokes it. Verify with `MossbackRigBuilder.Validate()` and `ValidateMotion()`. Six sampled poses confirm head, jaw and all four feet deform, with less than 0.00002 local units of drift in fully rooted back vertices. The maximum sampled edge stretch is 1.815x; the ankle transition was widened to remove a sharper stretch found in the initial pass.

All 17 expedition smoke checks passed in Play mode with skeletal animation and collision updates enabled. Successive live samples verified changing head/jaw rotations and advancing frame counts. Animated collider raycasts hit both flanks, front, rear and top. The 16-second `Screenshots/Mossback-Skeleton-Preview.mp4` shows the skeletal idle cycle at 16 fps; it is an offline pose preview, while the game evaluates poses every frame.

The PC render pipeline shadow distance is increased from 50 to 220 world units so Mossback casts a grounding shadow at the game's giant scale and overview camera distance.

The updated Windows player is built at `Builds/Windows/TinyGiantProblems.exe`.
