# Adaptive User Interfaces Toolkit
This repository contains AUIT, a toolkit to support the design of adaptive user interfaces in XR

We use multi-object optimization to combine different preferences and achieve optimal adaptations of 3D user interfaces.
For more information about each different component refer to the publication.

## To run and use AUIT
AUIT can be imported into an existing project by importing its respective Unity package in Unity.
The toolkit can be tested in the Unity Editor (v2022.3.39f1) and does not require additional software (see https://unity3d.com/get-unity/download/archive for installing Unity v2022.3.39f1).
However, running the scenes on a Microsoft HoloLens 2 requires the installation of certain compilation tools (see https://docs.microsoft.com/en-us/windows/mixed-reality/develop/install-the-tools).
This repository uses MRTK to simulate camera movement and prototyping with 3D UIs, but AUIT can be used without it (it has no dependencies).
Running the OculusExample scene requires the [Oculus Integration package](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022).

## Getting started
0. Have a compatible unity version installed (if paretto frontier optimization is wanted, a compatible python version is also necessary, but more on that later)
1. Clone or download the git repository.
2. Import AUIT in unity, either by just opening it with unity, or drag and dropping it into your current project.
3. See example scene for a basic setup with one cube virtual object.
4. To create an adaptive UI, add each of the two required components to the user interface (a game object) to adapt (note that some dependencies will be added automatically, such as the local objective handler):
- Adaptation Objectives (context source defines which context widget to use. A context source can be e.g. the player position, the camera view, etc.)
  - Field Of View Objective ()
  - Distance Interval Objective (position the game object in a range of distances from the context source)
  - Look Towards Objective ()
- Property Transitions
  - Cubic bezier transition

- Adaptation Manager (contains the Solver)
- Adaptation Trigger
  - 

For multiple UI element it is possible to add a Global Solver, so all the optimization runs in a single optimization loop. In this case, each UI element to be optimized must be referenced in a solver with the option "Global Solver" set to true.

## Navigating the source code
The components for AUIT described in our publication can be found in Assets > AUIT. All the components our toolkit currently supports are inside their correspondent directories (AdaptationObjectives, AdaptationTriggers, Solvers, and PropertyTransitions), with the exception of ContextSources - these are currently directly accessed by AdaptationObjectives since at the moment we only use sources of context that are already available in Unity.
