# Harness polish and Mossback idle motion

`MeadowPolish.Apply()` adds timber braces under all four access bridges, warm rope handrails with brass post collars, and continuous ladder side ropes tied into the landings. Rail openings preserve the existing branch routes. These details are visual rigging, not new blocking walls. Three shared generated meshes hold the new rigging. The decorator invokes this pass automatically.

Camp now has a worn earth clearing connected to the approach trail and irregular planting groups. Terrain weights use normalized SmoothStep inputs; the previous use of SmoothStep as an inverse-range function suppressed the dirt paint. The existing imported furniture, equipment and creature models remain in place.

Mossback now has a 7.5-second breathing cycle, slow weight shifts and small turns, a warning tremor, and a decaying reaction shudder. The entire creature frame moves together, including the mesh collider, walkways, rigging and tether sockets. The motion pivots at ground level. This replaces the previous broad sideways sliding motion and independent visual shaking that could separate the body from its platforms.

This is a procedural whole-body idle animation, not a skeletal walking cycle or separately articulated head/legs. The supplied GLB has no skeleton. Breathing amount, period and weight-shift angle are exposed on CreatureFrame.

Validation: all 17 expedition smoke checks passed with the new motion enabled, including 90-second attachment, complete ramp traversal, climbing and extraction. Successive live frame samples confirm both breathing scale and rotation change. All existing 262 scenery roots and eight anchors pass the alignment audit; all 14 added border plants also pass ground alignment. The solid Mossback collider remains enabled. The three rigging meshes contain 43,200 triangles in total.
