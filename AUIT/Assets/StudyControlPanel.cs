using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.ContextSources;
using AUIT.PropertyTransitions;
using Meta.XR.Movement.Retargeting;
using Oculus.Interaction;
using TMPro;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

public enum AdaptationType
{
    Manual,
    Adaptive,
}

public enum StudyStage
{
    Practice,
    Performance
}

public enum ScenarioType
{
    Stationary,
    SemiStationary,
    Moving
}

public class StudyUILoggingEvent
{
    public string uiName;
    public string eventType;

    public CoordinateSystem coordinateSystem;

    public float? duration;

    public bool? correct;

    public float timestamp;

    public StudyUILoggingEvent(string name, string type, CoordinateSystem coordinateSystem)
    {
        uiName = name;
        eventType = type;
        this.coordinateSystem = coordinateSystem;
        timestamp = Time.time;
    }

    public StudyUILoggingEvent(string name, string type, CoordinateSystem coordinateSystem, float? duration)
    {
        uiName = name;
        eventType = type;
        this.coordinateSystem = coordinateSystem;
        this.duration = duration;
        timestamp = Time.time;
    }
    public StudyUILoggingEvent(string name, string type, CoordinateSystem coordinateSystem, float? duration, bool? correct)
    {
        uiName = name;
        eventType = type;
        this.coordinateSystem = coordinateSystem;
        this.duration = duration;
        this.correct = correct;
        timestamp = Time.time;
    }
}

[Serializable]
public struct SerializableJoint
{
    public Vector3 position;
    public Quaternion rotation;
}

[Serializable]
public class SerializableFrame
{
    public float timestamp;
    public bool isValid;
    public string manifestation;
    public List<SerializableJoint> joints;

    public SerializableJoint poseHybridTask;

    public SerializableJoint poseVisualTask;

    public SerializableJoint poseControlTask;

    public CoordinateSystem coordinateSystemHybridUI;

    public CoordinateSystem coordinateSystemVisualUI;

    public CoordinateSystem coordinateSystemControlUI;

    public SerializableJoint poseHybridTaskUserCS;

    public SerializableJoint poseVisualTaskUserCS;

    public SerializableJoint poseControlTaskUserCS;

}

[Serializable]
public class SerializableFrameList
{
    public int captureRateHz;
    public List<SerializableFrame> frames = new List<SerializableFrame>();
}


public class StudyControlPanel : MonoBehaviour
{
    private List<StudyUILoggingEvent> loggingEvents = new List<StudyUILoggingEvent>();
    
    private List<StudyUILoggingEvent> loggingUIMovementDetails = new List<StudyUILoggingEvent>();
    public List<GameObject> taskKeys = new List<GameObject>();

    public GameObject keyTaskUI;
    public GameObject visualTaskUI;
    public GameObject controlTaskUI;

    public CoordinateSystemTransition keyTaskUserCoordinateSystemTransition;
    public CoordinateSystemTransition visualTaskUserCoordinateSystemTransition;
    public CoordinateSystemTransition controlTaskUserCoordinateSystemTransition;
    
    public TransformContextSource userTorsoContextSource;
    public GameObject keyChainTarget;

    public GameObject[] keyTaskUIs;
    public GameObject[] visualTaskUIs;

    public GameObject[] boxLids;

    public List<GameObject> DropBoxes = new List<GameObject>();

    public CoordinateSystemTransition keyTaskCoordinateSystemTransition;
    public CoordinateSystemTransition visualTaskCoordinateSystemTransition;
    public CoordinateSystemTransition controlTaskCoordinateSystemTransition;


    public MetaSourceDataProvider _provider;

    private int currentTaskIndex = 0;
    private int correctLocatedTaskCount = 0;
    private int correctVisualTaskCount = 0;

    private bool _isRecordingMotion = false;

    public AudioSource notificationSound;

    public AudioSource correctSound;
    public AudioSource incorrectSound;

    public AudioSource timeTicking;
    public AudioSource controlMusic;
    public AudioSource controlCall;

    public GameObject onTheGoTask;

    public GameObject OneFace;
    public GameObject FourFaces;
    public GameObject FiveFaces;
    public GameObject SixFaces;
    public GameObject EightFaces;

    private float keyTaskStartTime;
    private float visualTaskStartTime;
    private float controlTaskStartTime;

    public TextMeshProUGUI taskInfoText;
    private SerializableFrameList _recordedBodyPoses = new SerializableFrameList();

    private StudyLocatedTask[] keyTasks1 = {
        new StudyLocatedTask(new (int, int)[] { (0, 1), (1, 2), (2, 2), (3, 1), (4, 0) }, 2, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 0), (1, 1), (2, 3), (3, 3), (4, 4) }, 4, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 4), (1, 3), (2, 2), (3, 1), (4, 0) }, 0, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 0), (6, 1), (7, 2), (8, 3), (9, 4) }, 1, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 2), (2, 2), (4, 2), (6, 2), (8, 2) }, 2, 0),
        new StudyLocatedTask(new (int, int)[] { (1, 0), (3, 1), (5, 2), (7, 3), (9, 4) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (2, 1), (4, 3), (6, 2), (8, 4), (0, 0) }, 4, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 0), (0, 1), (0, 2), (0, 3), (0, 4) }, 0, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 1), (1, 3), (6, 2), (2, 0), (7, 4) }, 3, 0),
        new StudyLocatedTask(new (int, int)[] { (3, 0), (4, 1), (5, 2), (6, 3), (7, 4) }, 2, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 4), (8, 3), (8, 4), (8, 1), (4, 4) }, 1, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 0), (6, 1), (7, 1), (8, 0), (9, 0) }, 0, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 4), (2, 3), (4, 2), (6, 1), (8, 0) }, 4, 1),
        new StudyLocatedTask(new (int, int)[] { (1, 4), (3, 3), (5, 2), (7, 1), (9, 0) }, 2, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 0), (0, 1), (1, 2), (2, 3), (3, 4) }, 2, 1),
        new StudyLocatedTask(new (int, int)[] { (9, 4), (7, 3), (5, 2), (3, 1), (1, 0) }, 4, 0),
    };

    private StudyVisualAttentionTask[] visualTasks1 = {
        new StudyVisualAttentionTask(4, 2),
        new StudyVisualAttentionTask(6, 2),
        new StudyVisualAttentionTask(1, 1),
        new StudyVisualAttentionTask(8, 2),
        new StudyVisualAttentionTask(1, 0),
        new StudyVisualAttentionTask(5, 3),
        new StudyVisualAttentionTask(5, 2),
        new StudyVisualAttentionTask(8, 0),
        new StudyVisualAttentionTask(6, 4),
        new StudyVisualAttentionTask(4, 1),
        new StudyVisualAttentionTask(1, 0),
        new StudyVisualAttentionTask(5, 1),
        new StudyVisualAttentionTask(8, 7),
        new StudyVisualAttentionTask(1, 0),
        new StudyVisualAttentionTask(8, 1),
        new StudyVisualAttentionTask(4, 3),
    };

    private StudyLocatedTask[] keyTasks2 = {
        new StudyLocatedTask(new (int, int)[] { (0, 3), (1, 1), (2, 4), (3, 2), (4, 0) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 2), (6, 2), (7, 2), (8, 2), (9, 2) }, 2, 1),
        new StudyLocatedTask(new (int, int)[] { (1, 0), (3, 1), (5, 2), (7, 3), (9, 4) }, 1, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 1), (2, 3), (4, 0), (6, 2), (8, 4) }, 0, 0),
        new StudyLocatedTask(new (int, int)[] { (1, 1), (2, 2), (3, 3), (4, 4), (5, 0) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 4), (3, 3), (1, 2), (7, 1), (9, 0) }, 2, 1),
        new StudyLocatedTask(new (int, int)[] { (0, 2), (1, 2), (2, 2), (3, 2), (4, 2) }, 2, 0),
        new StudyLocatedTask(new (int, int)[] { (9, 4), (8, 3), (7, 2), (6, 1), (5, 0) }, 4, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 0), (2, 2), (4, 4), (6, 1), (8, 3) }, 4, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 1), (1, 4), (6, 0), (2, 3), (7, 2) }, 2, 0),
        new StudyLocatedTask(new (int, int)[] { (1, 0), (3, 1), (5, 2), (7, 3), (9, 4) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (2, 1), (4, 3), (6, 2), (8, 4), (0, 0) }, 4, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 0), (0, 1), (0, 2), (0, 3), (0, 4) }, 0, 1),
        new StudyLocatedTask(new (int, int)[] { (1, 0), (2, 1), (3, 2), (4, 2), (3, 4) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (4, 4), (3, 3), (2, 2), (1, 1), (0, 0) }, 2, 1),
        new StudyLocatedTask(new (int, int)[] { (6, 0), (6, 1), (1, 2), (3, 3), (6, 4) }, 3, 1),
    };

    private StudyVisualAttentionTask[] visualTasks2 = {
        new StudyVisualAttentionTask(8, 1),
        new StudyVisualAttentionTask(4, 3),
        new StudyVisualAttentionTask(6, 6),
        new StudyVisualAttentionTask(1, 0),
        new StudyVisualAttentionTask(5, 1),
        new StudyVisualAttentionTask(8, 7),
        new StudyVisualAttentionTask(1, 0),
        new StudyVisualAttentionTask(5, 3),
        new StudyVisualAttentionTask(6, 2),
        new StudyVisualAttentionTask(6, 5),
        new StudyVisualAttentionTask(6, 2),
        new StudyVisualAttentionTask(1, 1),
        new StudyVisualAttentionTask(8, 2),
        new StudyVisualAttentionTask(1, 0),
        new StudyVisualAttentionTask(5, 4),
        new StudyVisualAttentionTask(8, 8),
    };


    private StudyLocatedTask[] keyTasks3 = {
        new StudyLocatedTask(new (int, int)[] { (0, 0), (1, 2), (2, 4), (3, 1), (4, 3) }, 1, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 0), (6, 1), (7, 1), (8, 0), (9, 0) }, 0, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 4), (2, 3), (4, 2), (6, 1), (8, 0) }, 4, 1),
        new StudyLocatedTask(new (int, int)[] { (1, 4), (3, 3), (5, 2), (7, 1), (9, 0) }, 2, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 0), (0, 1), (1, 2), (2, 3), (3, 4) }, 2, 1),
        new StudyLocatedTask(new (int, int)[] { (9, 4), (7, 3), (5, 2), (3, 1), (1, 0) }, 4, 0),
        new StudyLocatedTask(new (int, int)[] { (1, 0), (2, 1), (3, 2), (4, 3), (5, 4) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (4, 4), (3, 3), (2, 2), (1, 1), (0, 0) }, 2, 1),
        new StudyLocatedTask(new (int, int)[] { (6, 0), (6, 1), (6, 2), (6, 3), (6, 4) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (0, 1), (2, 3), (4, 0), (6, 2), (8, 4) }, 3, 0),
        new StudyLocatedTask(new (int, int)[] { (5, 2), (6, 2), (7, 2), (8, 2), (9, 2) }, 2, 1),
        new StudyLocatedTask(new (int, int)[] { (1, 0), (3, 1), (5, 2), (7, 3), (9, 4) }, 1, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 1), (2, 3), (4, 0), (6, 2), (8, 4) }, 0, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 0), (0, 1), (0, 2), (0, 3), (0, 4) }, 0, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 1), (1, 3), (6, 2), (2, 0), (7, 4) }, 3, 0),
        new StudyLocatedTask(new (int, int)[] { (3, 0), (4, 1), (5, 2), (6, 3), (7, 4) }, 2, 0),
    };

    
    private StudyVisualAttentionTask[] visualTasks3 = {
        new StudyVisualAttentionTask(1, 1),
        new StudyVisualAttentionTask(5, 4),
        new StudyVisualAttentionTask(8, 8),
        new StudyVisualAttentionTask(1, 0),
        new StudyVisualAttentionTask(6, 3),
        new StudyVisualAttentionTask(8, 4),
        new StudyVisualAttentionTask(4, 1),
        new StudyVisualAttentionTask(6, 5),
        new StudyVisualAttentionTask(5, 2),
        new StudyVisualAttentionTask(6, 4),
        new StudyVisualAttentionTask(4, 3),
        new StudyVisualAttentionTask(6, 6),
        new StudyVisualAttentionTask(1, 0),
        new StudyVisualAttentionTask(5, 3),
        new StudyVisualAttentionTask(5, 2),
        new StudyVisualAttentionTask(8, 0),
    };

    private bool keyTaskDone = false;
    private bool visualTaskDone = false;
    private bool controlTaskDone = false;

    public GameObject visualAttentionTaskObject;


    [HideInInspector]
    public int currentVisualAnswer = -1;

    private StudyLocatedTask[] keyTask;

    private StudyVisualAttentionTask[] visualTask;

    [Header("Study Configuration")]

    [HideInInspector]
    public AdaptationType adaptationType = AdaptationType.Manual;
    public StudyStage studyStage = StudyStage.Practice;
    public ScenarioType scenarioType = ScenarioType.Stationary;

    public int trialNumber = 1;

    public string participantId;

    private bool _startWithVisualTaskMoving = false;

    [SerializeField] private int captureRateHz = 15;
    private float _nextCaptureTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    void Update()
    {
        if (!_isRecordingMotion || _provider == null) return;
 
        if (Time.time < _nextCaptureTime) return; // skip until next frame

        _nextCaptureTime = Time.time + 1f / captureRateHz;

        var pose = _provider.GetSkeletonPose();
        var frame = new SerializableFrame
        {
            timestamp = Time.time,
            isValid = _provider.IsPoseValid(),
            manifestation = _provider.GetManifestation(),
            joints = ConvertPose(pose),
            poseHybridTask = new SerializableJoint
            {
                position = keyTaskUI.transform.position,
                rotation = keyTaskUI.transform.rotation,
            },
            coordinateSystemHybridUI = keyTaskUserCoordinateSystemTransition.CurrentCoordinateSystem,
            poseHybridTaskUserCS = new SerializableJoint
            {
                position = userTorsoContextSource.GetValue().InverseTransformPoint(keyTaskUI.transform.position),
                rotation = Quaternion.Inverse(userTorsoContextSource.GetValue().rotation) * keyTaskUI.transform.rotation,
            },
            poseVisualTask = new SerializableJoint
            {
                position = visualTaskUI.transform.position,
                rotation = visualTaskUI.transform.rotation,
            },
            coordinateSystemVisualUI = visualTaskUserCoordinateSystemTransition.CurrentCoordinateSystem,
            poseVisualTaskUserCS = new SerializableJoint
            {
                position = userTorsoContextSource.GetValue().InverseTransformPoint(visualTaskUI.transform.position),
                rotation = Quaternion.Inverse(userTorsoContextSource.GetValue().rotation) * visualTaskUI.transform.rotation,
            },
            poseControlTask = new SerializableJoint
            {
                position = controlTaskUI.transform.position,
                rotation = controlTaskUI.transform.rotation,
            },
            coordinateSystemControlUI = controlTaskUserCoordinateSystemTransition.CurrentCoordinateSystem,
            poseControlTaskUserCS = new SerializableJoint
            {
                position = userTorsoContextSource.GetValue().InverseTransformPoint(controlTaskUI.transform.position),
                rotation = Quaternion.Inverse(userTorsoContextSource.GetValue().rotation) * controlTaskUI.transform.rotation,
            },
        };
        _recordedBodyPoses.frames.Add(frame);

        if (pose.IsCreated) pose.Dispose();
    }

    public void StartStudy()
    {
        correctLocatedTaskCount = 0;
        correctVisualTaskCount = 0;

        keyTaskDone = false;
        visualTaskDone = false;
        controlTaskDone = false;

        switch (trialNumber)
        {
            case 1:
                keyTask = keyTasks1;
                visualTask = visualTasks1;
                break;
            case 2:
                keyTask = keyTasks2;
                visualTask = visualTasks2;
                break;
            case 3:
                keyTask = keyTasks3;
                visualTask = visualTasks3;
                break;
            default:
                throw new Exception("Invalid trial number");
        }

        currentTaskIndex = studyStage == StudyStage.Practice ? 10 : 0;

        InitiateStudy();
        _isRecordingMotion = true;
        _recordedBodyPoses = new SerializableFrameList { captureRateHz = captureRateHz };

        loggingEvents = new List<StudyUILoggingEvent>();
        loggingUIMovementDetails = new List<StudyUILoggingEvent>();
    }

    public void InitiateStudy()
    {
        SetupTask(keyTask[currentTaskIndex].taskKeyIndices, keyTask[currentTaskIndex].targetKeyIndex, keyTask[currentTaskIndex].targetBox);

        foreach (GameObject key in taskKeys)
        {
            key.GetComponent<PositionInitializer>().Respawn();
        }
        keyTaskStartTime = Time.time;
    }

    public void SetupTask((int, int)[] taskKeyIndices, int targetKeyIndex, int dropBox)
    {
        for (int i = 0; i < taskKeys.Count; i++)
        {
            if (i < taskKeyIndices.Length)
            {
                taskKeys[i].transform.GetChild(0).gameObject.GetComponent<KeyLogic>().SetupKey(taskKeyIndices[i]);
            }
            else
            {
                throw new Exception("Not enough task keys provided for the task key indices.");
            }
        }

        keyChainTarget.GetComponent<KeyLogic>().SetupKey(taskKeyIndices[targetKeyIndex]);

        foreach (GameObject ui in keyTaskUIs)
        {
            ui.SetActive(true);
        }

        for (int i = 0; i < DropBoxes.Count; i++)
        {
            DropBoxes[i].SetActive(i == dropBox);
        }

        if (UnityEngine.Random.value > 0.5f) // 50% chance to start with music or visual task
        {
            StartCoroutine(QueueControlTask());
            _startWithVisualTaskMoving = false;
        }
        else
        {
            StartCoroutine(QueueVisualTask());
            _startWithVisualTaskMoving = true;
        }
    }


    public void AnswerVisualTask(bool correctAnswer)
    {
        timeTicking.Stop();

        if (currentVisualAnswer >= 0)
        {
            if (correctAnswer)
            {
                correctVisualTaskCount++;
                correctSound.Play();
                Debug.Log($"Correct answer! Current correct count: {correctVisualTaskCount}");
            }
            else
            {
                incorrectSound.Play();
                Debug.LogWarning($"Incorrect answer!");
            }
            visualAttentionTaskObject.SetActive(false);
            loggingEvents.Add(new StudyUILoggingEvent("VisualTask", "TaskCompletion", visualTaskCoordinateSystemTransition.CurrentCoordinateSystem, Time.time - visualTaskStartTime, correctAnswer));
            visualTaskDone = true;
            if (_startWithVisualTaskMoving)
            {
                StartCoroutine(QueueControlTask());
            }
            else
            {
                nextTaskCheck();
            }
        }
        else
        {
            Debug.LogError("Current task is not a visual attention task.");
        }

    }

    private void nextTaskCheck()
    {
        if (keyTaskDone && visualTaskDone && controlTaskDone && studyStage == StudyStage.Performance)
        {
            nextTask();
        }
        
    }

    public void nextTask()
    {
        keyTaskDone = false;
        visualTaskDone = false;
        controlTaskDone = false;
        
        currentTaskIndex++;
        taskInfoText.text = $"Task {currentTaskIndex + 1}/{keyTask.Length} - Correct: {correctLocatedTaskCount}";

        foreach (GameObject lid in boxLids)
        {
            // Grabbable g = lid.GetComponent<Grabbable>();
            // OneGrabRotateTransformer t = lid.GetComponent<OneGrabRotateTransformer>();
            // g.enabled = false;
            // t.enabled = false;
            lid.transform.localRotation = Quaternion.identity;
            // g.enabled = true;
            // t.enabled = true;
        }

        if (currentTaskIndex >= 7 && studyStage == StudyStage.Performance)
        {
            SaveToCSV();
            keyTaskUI.SetActive(false);
            visualTaskUI.SetActive(false);
            controlTaskUI.SetActive(false);
        }
        else
        {
            SetupTask(keyTask[currentTaskIndex].taskKeyIndices, keyTask[currentTaskIndex].targetKeyIndex, keyTask[currentTaskIndex].targetBox);
        }
    }

    private void SetupVisualAttentionTask(StudyVisualAttentionTask visualAttentionTask)
    {
        bool[] randomizedFaces = RandomizeFaces(visualAttentionTask.faces, visualAttentionTask.correctColorFaces);
        OneFace.SetActive(false);
        FourFaces.SetActive(false);
        FiveFaces.SetActive(false);
        SixFaces.SetActive(false);
        EightFaces.SetActive(false);
        timeTicking.Play();
        // StartCoroutine(VisualTaskDeadline());
        currentVisualAnswer = visualAttentionTask.correctColorFaces;
        visualAttentionTaskObject.SetActive(true);
        keyTaskStartTime = Time.time;

        foreach (GameObject ui in visualTaskUIs)
        {
            ui.SetActive(true);
        }

        switch (visualAttentionTask.faces)
        {
            case 1:
                OneFace.SetActive(true);
                OneFace.GetComponent<MeshRenderer>().material.color = randomizedFaces[0] ? Color.red : Color.blue;
                break;
            case 4:
                FourFaces.SetActive(true);
                MeshRenderer[] faces4 = FourFaces.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < faces4.Length; i++)
                {
                    faces4[i].material.color = randomizedFaces[i] ? Color.red : Color.blue;
                }
                break;
            case 5:
                FiveFaces.SetActive(true);
                MeshRenderer[] faces5 = FiveFaces.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < faces5.Length; i++)
                {
                    faces5[i].material.color = randomizedFaces[i] ? Color.red : Color.blue;
                }
                break;
            case 6:
                SixFaces.SetActive(true);
                MeshRenderer[] faces6 = SixFaces.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < faces6.Length; i++)
                {
                    faces6[i].material.color = randomizedFaces[i] ? Color.red : Color.blue;
                }
                break;
            case 8:
                EightFaces.SetActive(true);
                MeshRenderer[] faces8 = EightFaces.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < faces8.Length; i++)
                {
                    faces8[i].material.color = randomizedFaces[i] ? Color.red : Color.blue;
                }
                break;
            default:
                throw new ArgumentException("Invalid number of faces for the task.");
        }
    }

    // private IEnumerator VisualTaskDeadline()
    // {
    //     yield return new WaitForSeconds(25f);
    //     if (currentVisualAnswer >= 0)
    //     {
    //         AnswerVisualTask(false);
    //         Debug.Log("Visual task timed out.");
    //         visualTaskDone = true;
    //         if (_startWithVisualTaskMoving)
    //         {
    //             StartCoroutine(QueueControlTask());
    //         }
    //         else
    //         {
    //             nextTaskCheck();
    //         }
    //     }
    //     currentVisualAnswer = -1;
    //     visualAttentionTaskObject.SetActive(false);
        
    //     foreach (GameObject ui in visualTaskUIs)
    //     {
    //         ui.SetActive(false);
    //     }
    // }

    public void StopMusic()
    {
        if (controlMusic.isPlaying || controlCall.isPlaying)
        {
            controlMusic.Stop();
            controlCall.Stop();
            controlTaskDone = true;
            loggingEvents.Add(new StudyUILoggingEvent("ControlTask", "TaskCompletion", controlTaskCoordinateSystemTransition.CurrentCoordinateSystem, Time.time - controlTaskStartTime, true));
            if (!_startWithVisualTaskMoving)
            {
                StartCoroutine(QueueVisualTask());
            }
            else
            {
                nextTaskCheck();
            }
        }
    }

    private IEnumerator QueueVisualTask()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(5f, 15f));
        notificationSound.Play();
        SetupVisualAttentionTask(visualTask[currentTaskIndex]);
        visualTaskStartTime = Time.time;
    }

    private IEnumerator QueueControlTask()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(5f, 15f));
        if (UnityEngine.Random.value > 0.5f)
        {
            controlMusic.Play();
        } else
        {
            controlCall.Play();
        }
        controlTaskStartTime = Time.time;
    }


    public void AdvanceTask(GameObject key, int boxIndex)
    {
        if (taskKeys.Contains(key) && !keyTaskDone)
        {
            int keyIndex = taskKeys.IndexOf(key);
            bool taskCorrect = keyIndex == keyTask[currentTaskIndex].targetKeyIndex && boxIndex == keyTask[currentTaskIndex].targetBox;
            if (taskCorrect)
            {
                correctLocatedTaskCount++;
                correctSound.Play();

                Debug.Log($"Correct key selected! Current correct count: {correctLocatedTaskCount}");
            }
            else
            {
                incorrectSound.Play();
                Debug.LogWarning($"Incorrect key selected! Expected index: {keyTask[currentTaskIndex].targetKeyIndex}, but got: {keyIndex}");
            }

            foreach (GameObject ui in keyTaskUIs)
            {
                ui.SetActive(false);
            }

            key.GetComponent<PositionInitializer>().Respawn();
            loggingEvents.Add(new StudyUILoggingEvent("KeyTask", "TaskCompletion", keyTaskCoordinateSystemTransition.CurrentCoordinateSystem, Time.time - keyTaskStartTime, taskCorrect));
            keyTaskDone = true;
            nextTaskCheck();
        }
    }

    private bool[] RandomizeFaces(int size, int trueCount)
    {
        if (trueCount > size)
        {
            Debug.LogError("trueCount cannot be greater than the array size.");
            return null;
        }

        bool[] result = new bool[size];

        int assigned = 0;
        while (assigned < trueCount)
        {
            int index = UnityEngine.Random.Range(0, size);
            if (!result[index])
            {
                result[index] = true;
                assigned++;
            }
        }

        print(result);

        return result;
    }

    public void VisualAnswer(bool correct)
    {
        AnswerVisualTask(correct);
        currentVisualAnswer = -1;
        visualAttentionTaskObject.SetActive(false);
        
        foreach (GameObject ui in visualTaskUIs)
        {
            ui.SetActive(false);
        }

        // if (_startWithVisualTaskMoving)
        // {
        //     StartCoroutine(QueueControlTask());
        // }
        // else
        // {
        //     nextTaskCheck();
        // }

        // if (!_startWithVisualTaskMoving)
        // {
        //     nextTaskCheck();
        // }
    }

    public void LogEvent(string uiName, string eventType, CoordinateSystem coordinateSystem, bool movementDetail = false, float? duration = null)
    {
        if (movementDetail)
        {
            loggingUIMovementDetails.Add(new StudyUILoggingEvent(uiName, eventType, coordinateSystem, duration));
        }
        else
        {
            loggingEvents.Add(new StudyUILoggingEvent(uiName, eventType, coordinateSystem, duration));
        }
    }

    public void SaveToCSV()
    {
        // Root folder for all study logs
        string baseFolder = Path.Combine(Application.dataPath, "StudyLogs");
        
        // Subfolder per participant
        string participantFolder = Path.Combine(baseFolder, $"Participant_{participantId}");
        Directory.CreateDirectory(participantFolder);

        // ---- UI Log ----
        string uiLogPath = Path.Combine(participantFolder, $"StudyUILog_{participantId}_{scenarioType}.csv");
        using (StreamWriter sw = new StreamWriter(uiLogPath, false)) // overwrite
        {
            sw.WriteLine("UI Name,Event Type,Coordinate System,Correct,Timestamp,Duration");

            foreach (var logEvent in loggingEvents)
            {
                sw.WriteLine(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5}",
                    logEvent.uiName, logEvent.eventType, logEvent.coordinateSystem,
                    logEvent.correct.HasValue ? logEvent.correct.Value.ToString() : "",
                    logEvent.timestamp.ToString("F2"),
                    logEvent.duration.HasValue ? logEvent.duration.Value.ToString("F2") : ""
                ));
            }
        }

        // ---- UI Positioning Log ----
        string uiPosPath = Path.Combine(participantFolder, $"StudyUIPositioningLog_{participantId}_{scenarioType}.csv");
        using (StreamWriter sw = new StreamWriter(uiPosPath, false))
        {
            sw.WriteLine("UI Name,Event Type,Coordinate System,Duration,Timestamp");

            foreach (var logEvent in loggingUIMovementDetails)
            {
                sw.WriteLine(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4}",
                    logEvent.uiName, logEvent.eventType, logEvent.coordinateSystem,
                    logEvent.duration.HasValue ? logEvent.duration.Value.ToString("F2") : "",
                    logEvent.timestamp.ToString("F2")
                ));
            }
        }

        // ---- JSON Pose/Motion Log ----
        string jsonPath = Path.Combine(participantFolder, $"StudyMotionLog_{participantId}_{scenarioType}.json");
        string json = JsonUtility.ToJson(_recordedBodyPoses, true);
        File.WriteAllText(jsonPath, json);

        Debug.Log($"Logs saved to: {participantFolder}");
    }

    private List<SerializableJoint> ConvertPose(NativeArray<NativeTransform> pose)
    {
        var joints = new List<SerializableJoint>(pose.Length);
        for (int i = 0; i < pose.Length; i++)
        {
            var t = pose[i];
            joints.Add(new SerializableJoint
            {
                position = t.Position,
                rotation = t.Orientation,
                // scale = t.Scale
            });
        }
        return joints;
    }

    internal void ResetUIs()
    {
        keyTaskUI.transform.position = new Vector3(-0.0825000033f, 1.05673003f, 0.483770013f);
        keyTaskUI.transform.rotation = Quaternion.identity;
        visualTaskUI.transform.position = new Vector3(-0.33950001f, 1.05673003f, 0.483770013f);
        visualTaskUI.transform.rotation = Quaternion.identity;
        controlTaskUI.transform.position = new Vector3(0.207599998f, 1.05673003f, 0.483770013f);
        controlTaskUI.transform.rotation = Quaternion.identity;
        keyTaskUI.GetComponent<ReferenceFrameControls>().SelectWorldAsReferenceFrame();
        visualTaskUI.GetComponent<ReferenceFrameControls>().SelectWorldAsReferenceFrame();
        controlTaskUI.GetComponent<ReferenceFrameControls>().SelectWorldAsReferenceFrame();
    }
}

[CustomEditor(typeof(StudyControlPanel))]
public class KeyPickerLogicEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StudyControlPanel studyControlPanel = (StudyControlPanel)target;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Study Control Panel", EditorStyles.boldLabel);

        if (GUILayout.Button("Start Study"))
        {
            studyControlPanel.StartStudy();
        }

        
        if (GUILayout.Button("Next Task"))
        {
            studyControlPanel.nextTask();
        }

        if (GUILayout.Button("Reset UIs"))
        {
            studyControlPanel.ResetUIs();
        }

        GUILayout.Space(10);

        EditorGUILayout.LabelField("Visual Task", EditorStyles.boldLabel);


        GUILayout.Space(5);


        EditorGUILayout.LabelField("Correct Answer: ", studyControlPanel.currentVisualAnswer.ToString());

        if (GUILayout.Button("Correct Visual Answer"))
        {
            studyControlPanel.VisualAnswer(true);
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Wrong Visual Answer"))
        {
            studyControlPanel.VisualAnswer(false);
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Stop Music"))
        {
            studyControlPanel.StopMusic();
        }
        
        GUILayout.Space(5);

        if (GUILayout.Button("Log To CSV"))
        {
            studyControlPanel.SaveToCSV();
        }
    }
}
