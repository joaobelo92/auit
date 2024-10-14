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
   - See example scene (insert path to scene here) for a basic setup with one cube virtual object.
3. Create **one** game object and put the *Adaptation Manager* script & at least one *Optimization Trigger* inside (in example scenes it is always called *auit*)
4. To create an adaptive UI, add each of the two required components to the user interface (a game object) to adapt (note that some dependencies will be added automatically, such as the local objective handler):
- Adaptation Objectives (context source defines which context widget to use. A context source can be e.g. the player position, the camera view, etc.)
  - Field Of View Objective ()
  - Distance Interval Objective (position the game object in a range of distances from the context source)
  - Look Towards Objective ()
- Property Transitions (how the game object should transition to the new state)
  - Cubic bezier transition (the transition path is linearly interpreted, the speed is determined by a user defined cubic bezier function)

- Adaptation Manager (contains the Solver)
- Adaptation Trigger
  - 

## Multi objective optimization with paretto frontier & python
1. Make sure you have python and a package manager installed (for the purposes of this tutorial we will use conda)
2. Navigate into `./Python/` and make sure you have all packages in `environment.yml` installed (if you are using conda, simply run `conda env create -f ./environment.yml` to set up a new environment called **auit** with all necessaray packages installed. After the command has finished you should be able to run `conda activate auit` to activate the auit environment.)
3. Now run `python ./solver.py` to start the paretto frontier solver.
4. It should now prompt you on which port the server is listening (e.g. *Listening on port 5555...*)


## Navigating the source code
The components for AUIT described in our publication can be found in Assets > AUIT. All the components our toolkit currently supports are inside their correspondent directories ([AdaptationObjectives](./AUIT/Assets/AUIT/AdaptationObjectives/), [AdaptationTriggers](./AUIT/Assets/AUIT/AdaptationTriggers/), [Solvers](./AUIT/Assets/AUIT/Solvers/), and [PropertyTransitions](./AUIT/Assets/AUIT/PropertyTransitions/)), with the exception of ContextSources - these are currently directly accessed by AdaptationObjectives since at the moment we only use sources of context that are already available in Unity.

```
├── AUIT
│   └── Assets
│       └── AUIT
│           ├── AdaptationObjectives
│           ├── AdaptationTriggers
│           ├── PropertyTransitions
│           └── Solvers
├── Python
│   └── solver.py
├── .gitignore
└── README.md
```

## Troubleshooting
<details>
<summary>Some keyboard-inputs behave weirdly or you don't have the expected control?</summary>
In unity navigate to Edit > Project Settings > Input Manager > Axes (dropdown) and make sure the controls are bound correctly
<ul>
  <li><b>Horizontal</b> for left right movement</li>
  <li><b>Vertical</b> for forward backward movement</li>
  <li><b>UpDown</b> for upwards and downwards movement</li>
  <li><b>Optimization Request</b> to use the *On Request Optimization Trigger* with a hotkey</li>
</ul>
</details>
