# ProceduralAnimation
Test procedural animation

Install package
- Install Animation Rigging package from unity package manager

Renderize bones
- Put quadruped in scene
- Select armature (skeleton parent) and from topbar select Animation Rigging > Bone Renderer Setup
	- this add Bone Renderer script to the object

Add IK
- select armature again, and from topbar select Animation Rigging > Rig Setup
	- this add Animator and Rig Builder script to the object
	- this add also Rig 1 gameObject child of Armature, and reference it in the Rig Builder script
- now select Rig 1 and add an empty object as child. Name it Foot.001-IK and it will be the controller for our first foot
- add Two Bone IK Constraint script to this object we just created
- set the tip reference in this script: armature > hips > Thigh.001 > Leg.001 > Put Leg.001_end
- right click the script and select "Auto setup from tip Transform"
	- this add also two childs: target and hint
	- by selecting target and hint objects, we can use the tool in the bottom right corner of the scene view to make them more visible. (Target: Green, BoxEffector, Size 0.5. Hint: Red, LocatorEffector, Size 2)
- do again from point 2 (add child to Rig 1, add script, set tip reference, click auto setup) for the other three legs

You can set target and hint in a more user-friendly position and rotation:
- rotations at 0, 0, 0
- scales are already at 0.1, 0.1, 0.1
- positions:
	- target 1: 2.09, 0, 1.45
	- target 2: -2.09, 0, -1.45
	- target 3: 2.09, 0, -1.45
	- target 4: -2.09, 0, 1.45
	- hint 1: 2.5, 2.5, 2
	- hint 2: -2.5, 2.5, -2
	- hint 3: 2.5, 2.5, -2
	- hint 4: -2.5, 2.5, 2

The idea is that the  target  is the locator we'll need to move to update the position of the bones up the leg chain, and the "hint" is an additional marker to help the rig bend the leg the right way.
The main idea will be to consider the rest position of our skeleton and offset it by the current transform of the body.
This will allow us to compute the "desired" position of each feet and check if any foot is really too far away from this target point. If the distance is too great, then we'll trigger a step of that foot.
We ll also setup some reset logic so that, if the body is not moving anymore, the limbs of our skeleton come back to their rest position and restore the equilibrium of the avatar.

Script:
- on parent, add ProceduralAnimator.cs and ClickMovement.cs
- create a layer Ground
- set environment layers to Ground
- set it in ClickMovement 
	- this just check if click on ground. Rotate and move transform to that position
- set it also in ProceduralAnimator
- in ProceduralAnimator:
	- set targets (the objects we created before with "Auto setup from tip Transform". Obv only the targets and not the hints)
	- set variables as needed