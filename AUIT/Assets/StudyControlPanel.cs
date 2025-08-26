using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public enum AdaptationType
{
    Manual,
    Adaptive,
}

public enum ScenarioType
{
    Stationary,
    SemiStationary,
    Moving
}

public class StudyControlPanel : MonoBehaviour
{
    public List<GameObject> taskKeys = new List<GameObject>();
    public GameObject keyChainTarget;

    public AdaptationType adaptationType = AdaptationType.Manual;
    public ScenarioType scenarioType = ScenarioType.Stationary;

    public List<GameObject> TaskPolygons = new List<GameObject>();
    public List<GameObject> DropBoxes = new List<GameObject>();

    private int currentTaskIndex = 0;
    private int correctLocatedTaskCount = 0;

    private int correctVisualTaskCount = 0;

    public AudioSource notificationSound;

    public AudioSource correctSound;
    public AudioSource incorrectSound;

    public AudioSource timeTicking;

    public GameObject onTheGoTask;

    private bool visualTaskRunning;

    public GameObject OneFace;
    public GameObject FourFaces;
    public GameObject FiveFaces;
    public GameObject SixFaces;
    public GameObject EightFace;

    // public GameObject PromptTaskCube;

    // public GameObject[] promptTaskComponents;
    public GameObject[] visualAttentionTaskComponents;

    public GameObject[] visualAttentionAnswerComponents;

    public TextMeshProUGUI taskInfoText;

    private StudyLocatedTask[] stationaryStudy = {
        new StudyLocatedTask(new (int, int)[] { (0, 1), (1, 2), (2, 2), (3, 1), (4, 0) }, 2, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 0), (1, 1), (2, 3), (3, 3), (4, 4) }, 4, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 4), (1, 3), (2, 2), (3, 1), (4, 0) }, 0, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 0), (6, 1), (7, 2), (8, 3), (9, 4) }, 1, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 2), (2, 2), (4, 2), (6, 2), (8, 2) }, 2, 0),
        new StudyLocatedTask(new (int, int)[] { (1, 0), (3, 1), (5, 2), (7, 3), (9, 4) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (2, 1), (4, 3), (6, 2), (8, 4), (0, 0) }, 4, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 0), (0, 1), (0, 2), (0, 3), (0, 4) }, 0, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 1), (1, 3), (6, 2), (2, 0), (7, 4) }, 3, 0),
        new StudyLocatedTask(new (int, int)[] { (3, 0), (4, 1), (5, 2), (6, 3), (7, 4) }, 2, 0)
    };

    private StudyNonLocatedTask[] stationaryNonLocatedStudy = {
        new StudyVisualAttentionTask(4, 2, new int[] { 1, 2, 3 }),
        new StudyPromptTask(Color.red),
        new StudyVisualAttentionTask(8, 2, new int[] { 2, 3, 4 }),
        new StudyPromptTask(Color.blue),
        new StudyVisualAttentionTask(1, 1, new int[] { 0, 1, 2 }),
        new StudyPromptTask(Color.blue),
        new StudyVisualAttentionTask(5, 2, new int[] { 1, 2, 3 }),
        new StudyPromptTask(Color.red),
        new StudyVisualAttentionTask(6, 4, new int[] { 2, 3, 4 }),
        new StudyPromptTask(Color.blue),
    };

    private StudyLocatedTask[] semiStationaryStudy = {
        new StudyLocatedTask(new (int, int)[] { (0, 3), (1, 1), (2, 4), (3, 2), (4, 0) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 2), (6, 2), (7, 2), (8, 2), (9, 2) }, 2, 1),
        new StudyLocatedTask(new (int, int)[] { (1, 0), (3, 1), (5, 2), (7, 3), (9, 4) }, 1, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 1), (2, 3), (4, 0), (6, 2), (8, 4) }, 0, 0),
        new StudyLocatedTask(new (int, int)[] { (1, 1), (2, 2), (3, 3), (4, 4), (5, 0) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 4), (3, 3), (1, 2), (7, 1), (9, 0) }, 2, 1),
        new StudyLocatedTask(new (int, int)[] { (0, 2), (1, 2), (2, 2), (3, 2), (4, 2) }, 2, 0),
        new StudyLocatedTask(new (int, int)[] { (9, 4), (8, 3), (7, 2), (6, 1), (5, 0) }, 4, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 0), (2, 2), (4, 4), (6, 1), (8, 3) }, 4, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 1), (1, 4), (6, 0), (2, 3), (7, 2) }, 2, 0)
    };

    private StudyNonLocatedTask[] semiStationaryNonLocatedStudy = {
        new StudyVisualAttentionTask(4, 3, new int[] { 1, 2, 3 }),
        new StudyPromptTask(Color.red),
        new StudyVisualAttentionTask(8, 7, new int[] { 5, 6, 7 }),
        new StudyPromptTask(Color.blue),
        new StudyVisualAttentionTask(1, 0, new int[] { 0, 1, 2 }),
        new StudyPromptTask(Color.blue),
        new StudyVisualAttentionTask(5, 3, new int[] { 1, 2, 3 }),
        new StudyPromptTask(Color.red),
        new StudyVisualAttentionTask(6, 2, new int[] { 1, 2, 3 }),
        new StudyPromptTask(Color.blue),
    };


    private StudyLocatedTask[] movingStudy = {
        new StudyLocatedTask(new (int, int)[] { (0, 0), (1, 2), (2, 4), (3, 1), (4, 3) }, 1, 1),
        new StudyLocatedTask(new (int, int)[] { (5, 0), (6, 1), (7, 1), (8, 0), (9, 0) }, 0, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 4), (2, 3), (4, 2), (6, 1), (8, 0) }, 4, 1),
        new StudyLocatedTask(new (int, int)[] { (1, 4), (3, 3), (5, 2), (7, 1), (9, 0) }, 2, 0),
        new StudyLocatedTask(new (int, int)[] { (0, 0), (0, 1), (1, 2), (2, 3), (3, 4) }, 2, 1),
        new StudyLocatedTask(new (int, int)[] { (9, 4), (7, 3), (5, 2), (3, 1), (1, 0) }, 4, 0),
        new StudyLocatedTask(new (int, int)[] { (1, 0), (2, 1), (3, 2), (4, 3), (5, 4) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (4, 4), (3, 3), (2, 2), (1, 1), (0, 0) }, 2, 1),
        new StudyLocatedTask(new (int, int)[] { (6, 0), (6, 1), (6, 2), (6, 3), (6, 4) }, 3, 1),
        new StudyLocatedTask(new (int, int)[] { (0, 1), (2, 3), (4, 0), (6, 2), (8, 4) }, 3, 0)
    };

    private StudyNonLocatedTask[] movingNonLocatedStudy = {
        new StudyVisualAttentionTask(4, 2, new int[] { 1, 2, 3 }),
        new StudyPromptTask(Color.red),
        new StudyVisualAttentionTask(8, 5, new int[] { 5, 6, 7 }),
        new StudyPromptTask(Color.blue),
        new StudyVisualAttentionTask(1, 1, new int[] { 0, 1, 2 }),
        new StudyPromptTask(Color.blue),
        new StudyVisualAttentionTask(5, 1, new int[] { 1, 2, 3 }),
        new StudyPromptTask(Color.red),
        new StudyVisualAttentionTask(6, 5, new int[] { 4, 5, 6 }),
        new StudyPromptTask(Color.blue),
    };




    private StudyLocatedTask[] task;

    private StudyNonLocatedTask[] nonLocatedTasks;

    // Start is called once before the first execution of Update after the MonSoBehaviour is created
    void Start()
    {
        // for (int i = 0; i < transform.childCount; i++)
        // {
        //     taskKeys.Add(transform.GetChild(i).gameObject);
        // }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartStudy()
    {
        currentTaskIndex = 0;
        correctLocatedTaskCount = 0;

        task = scenarioType switch
        {
            ScenarioType.Stationary => stationaryStudy,
            ScenarioType.SemiStationary => semiStationaryStudy,
            ScenarioType.Moving => movingStudy,
            _ => throw new System.Exception("Invalid scenario type")
        };
        nonLocatedTasks = scenarioType switch
        {
            ScenarioType.Stationary => stationaryNonLocatedStudy,
            ScenarioType.SemiStationary => semiStationaryNonLocatedStudy,
            ScenarioType.Moving => movingNonLocatedStudy,
            _ => throw new System.Exception("Invalid scenario type")
        };
        SetupTask(task[currentTaskIndex].taskKeyIndices, task[currentTaskIndex].targetKeyIndex, task[currentTaskIndex].targetBox);

        foreach (GameObject key in taskKeys)
        {
            key.GetComponent<PositionInitializer>().Respawn();
        }
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

        for (int i = 0; i < DropBoxes.Count; i++)
        {
            DropBoxes[i].SetActive(i == dropBox);
        }

        StartCoroutine(PrepareMovingTask());
    }

    // public void AnswerPrompt(string answer)
    // {
    //     foreach (GameObject go in promptTaskComponents)
    //     {
    //         go.SetActive(false);
    //     }

    //     if (nonLocatedTasks[currentTaskIndex] is StudyPromptTask promptTask)
    //     {
    //         if (answer == "blue" && promptTask.color == Color.blue ||
    //             answer == "red" && promptTask.color == Color.red)
    //         {
    //             correctPromptTaskCount++;
    //             Debug.Log($"Correct answer! Current correct count: {correctPromptTaskCount}");
    //         }
    //         else
    //         {
    //             Debug.LogWarning($"Incorrect answer! Expected color: {promptTask.color}, but got: {answer}");
    //         }
    //         {
    //         }
    //     } else {
    //         Debug.LogError("Current task is not a prompt task.");
    //     }



    // }

    
    public void AnswerVisualTask(int answer)
    {
        
        visualTaskRunning = false;
        timeTicking.Stop();
        foreach (GameObject go in visualAttentionTaskComponents)
        {
            go.SetActive(false);
        }

        if (nonLocatedTasks[currentTaskIndex] is StudyVisualAttentionTask visualAttentionTask)
        {
            if (answer >= 0 &&visualAttentionTask.possibleAnswers[answer] == visualAttentionTask.correctColorFaces)
            {
                correctVisualTaskCount++;
                correctSound.Play();
                Debug.Log($"Correct answer! Current correct count: {correctVisualTaskCount}");
            }
            else
            {
                incorrectSound.Play();
                Debug.LogWarning($"Incorrect answer! Expected color faces: {visualAttentionTask.correctColorFaces}, but got: {answer}");
            }

            foreach (GameObject go in visualAttentionTaskComponents)
            {
                go.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("Current task is not a visual attention task.");
        }
    }


    private IEnumerator PrepareMovingTask()
    {
        if (nonLocatedTasks[currentTaskIndex] is StudyPromptTask) // no longer for this study
            yield break;
        yield return new WaitForSeconds(UnityEngine.Random.Range(5f, 10f));
        onTheGoTask.SetActive(true);
        notificationSound.Play();
        // foreach (GameObject go in promptTaskComponents)
        // {
        //     go.SetActive(nonLocatedTasks[currentTaskIndex] is StudyPromptTask);
        // }
        foreach (GameObject go in visualAttentionTaskComponents)
        {
            go.SetActive(true);
        }

        if (nonLocatedTasks[currentTaskIndex] is StudyVisualAttentionTask visualAttentionTask)
        {
            SetupVisualAttentionTask(visualAttentionTask);
        }
        // else if (nonLocatedTasks[currentTaskIndex] is StudyPromptTask promptTask)
        // {
        //     SetupPromptTask(promptTask);
        // }


    }

    // private void SetupPromptTask(StudyPromptTask promptTask)
    // {
    //     PromptTaskCube.GetComponent<Renderer>().material.color = promptTask.color;
    // }

    private void SetupVisualAttentionTask(StudyVisualAttentionTask visualAttentionTask)
    {
        bool[] randomizedFaces = RandomizeFaces(visualAttentionTask.faces, visualAttentionTask.correctColorFaces);
        OneFace.SetActive(false);
        FourFaces.SetActive(false);
        FiveFaces.SetActive(false);
        SixFaces.SetActive(false);
        EightFace.SetActive(false);
        for (int i = 0; i < visualAttentionAnswerComponents.Length; i++)
        {
            visualAttentionAnswerComponents[i].GetComponent<TextMeshProUGUI>().text = visualAttentionTask.possibleAnswers[i].ToString();
        }
        visualTaskRunning = true;
        timeTicking.Play();
        StartCoroutine(VisualTaskDeadline());
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
                EightFace.SetActive(true);
                MeshRenderer[] faces8 = EightFace.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < faces8.Length; i++)
                {
                    faces8[i].material.color = randomizedFaces[i] ? Color.red : Color.blue;
                }
                break;
            default:
                throw new ArgumentException("Invalid number of faces for the task.");
        }
    }

    private IEnumerator VisualTaskDeadline()
    {
        yield return new WaitForSeconds(20f);
        if (visualTaskRunning)
        {
            AnswerVisualTask(-1); // -1 indicates timeout
            Debug.Log("Visual task timed out.");
        }
    }

    public void AdvanceTask(GameObject key, int boxIndex)
    {
        if (taskKeys.Contains(key))
        {
            int keyIndex = taskKeys.IndexOf(key);
            if (keyIndex == task[currentTaskIndex].targetKeyIndex && boxIndex == task[currentTaskIndex].targetBox)
            {
                correctLocatedTaskCount++;
                correctSound.Play();

                Debug.Log($"Correct key selected! Current correct count: {correctLocatedTaskCount}");
            }
            else
            {
                incorrectSound.Play();
                Debug.LogWarning($"Incorrect key selected! Expected index: {task[currentTaskIndex].targetKeyIndex}, but got: {keyIndex}");
            }
            currentTaskIndex++;
            taskInfoText.text = $"Task {currentTaskIndex + 1}/{task.Length} - Correct: {correctLocatedTaskCount}";

            key.GetComponent<PositionInitializer>().Respawn();
        }
        else
        {
            Debug.LogError("Key not found in task keys.");
        }
        if (currentTaskIndex < task.Length)
        {
            SetupTask(task[currentTaskIndex].taskKeyIndices, task[currentTaskIndex].targetKeyIndex, task[currentTaskIndex].targetBox);
        }
        else
        {
            Debug.Log("All tasks completed!");
            // Handle end of study logic here
            // e.g., show results, reset study, etc.
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


}

[CustomEditor(typeof(StudyControlPanel))]
public class KeyPickerLogicEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StudyControlPanel keyPickerLogic = (StudyControlPanel)target;
        if (GUILayout.Button("Start Study"))
        {
            keyPickerLogic.StartStudy();
        }
    }
}
