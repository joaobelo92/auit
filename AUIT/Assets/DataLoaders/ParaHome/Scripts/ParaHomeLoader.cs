using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using Dummiesman;
using UnityEditor;

public class ParaHomeLoader : MonoBehaviour
{
    #region Static Fields

    public static readonly string[] OBJ_PARTS = { "base", "part1", "part2" };

    private enum AvatarGender { male, female, neutral };

    private static Matrix4x4 C_MAT =new Matrix4x4(
            new Vector4(0,0,1,0),
            new Vector4(1,0,0,0),
            new Vector4(0,1,0,0),
            new Vector4(0,0,0,1));
    
    #endregion

    #region Public Fields

    [Header("References")]
    public ParaHomeAvatar m_avatar;
    public Transform m_environment;
    public Material m_objMaterial;

    [Header("Settings")]
    public string m_rootDir = "ParaHome";
    public string m_scanDir = "data/scan";
    public string m_seqDir = "data/seq";
    public string m_seq = "s1";

    #endregion

    #region Private Fields

    private ParaHomeScene[] m_sceneObjects;

    private ParaHomeAvatarPose[] m_poses;

    private int m_currentFrame;

    private Coroutine m_sequenceCoroutine;

    #endregion

    #region Class Methods 
    public static Matrix4x4 ParseMat4x4(float[] values)
    {
        Matrix4x4 tMat = new Matrix4x4();
        tMat.SetRow(0, new Vector4(values[0], values[1], values[2], values[3]));
        tMat.SetRow(1, new Vector4(values[4], values[5], values[6], values[7]));
        tMat.SetRow(2, new Vector4(values[8], values[9], values[10], values[11]));
        tMat.SetRow(3, new Vector4(values[12], values[13], values[14], values[15]));
        return C_MAT * tMat;
    }

    #endregion

    #region Private Methods 

    private GameObject LoadEnvironmentObject(string path)
    {
        if (File.Exists(path))
        {
            GameObject objScene = new OBJLoader().Load(path);
            GameObject obj = objScene.transform.GetChild(0).gameObject;
            obj.GetComponent<Renderer>().sharedMaterial = new Material(m_objMaterial);
            obj.transform.SetParent(null);
            obj.transform.localScale = Vector3.one;
            DestroyImmediate(objScene);
            
            return obj;
        }

        return null;
    }

    private bool LoadSequencePoses()
    {
        string seqDir = Path.Combine(Application.streamingAssetsPath, m_rootDir, m_seqDir, m_seq);
        string avatarPath = Path.Combine(seqDir, "avatar.json");
        if (!File.Exists(avatarPath))
        {
            Debug.LogError("ParaHomeLoader.LoadObjTransforms(): Poses file not found: " + avatarPath);
            return false;
        }

        string avatarJson = File.ReadAllText(avatarPath);
        ParaHomeAvatarPoseInfo[] avatarPoseInfo = JsonConvert.DeserializeObject<ParaHomeAvatarPoseInfo[]>(avatarJson);
        int numPoses = avatarPoseInfo.Length;
        m_poses = new ParaHomeAvatarPose[numPoses];
        for (int i = 0; i < numPoses; i++)
        {
            m_poses[i] = new ParaHomeAvatarPose(avatarPoseInfo[i]);
        }

        return true;
    }

    private void LoadScenePoses(int i)
    {
        if (m_poses == null)
        {
            if (!LoadSequencePoses())
            {
                Debug.LogError("ParaHomeLoader.LoadScene(): Unable to load scene without pose information.");
                return;
            }
        }
        m_avatar.SetPose(m_poses[m_currentFrame]);
    }

    public bool LoadSequenceSceneObjects()
    {
        string seqDir = Path.Combine(Application.streamingAssetsPath, m_rootDir, m_seqDir, m_seq);
        string objTransformPath = Path.Combine(seqDir, "objTransforms.json");
        if (!File.Exists(objTransformPath))
        {
            Debug.LogError("ParaHomeLoader.LoadObjTransforms(): Object transforms file not found: " + objTransformPath);
            return false;
        }

        string objTransformJson = File.ReadAllText(objTransformPath);
        Dictionary<string, Dictionary<string, float[]>>[] objTransformInfo = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, float[]>>[]>(objTransformJson);
        int numFrames = objTransformInfo.Length;
        m_sceneObjects = new ParaHomeScene[numFrames];
        for (int i = 0; i < numFrames; i++)
        {
            m_sceneObjects[i] = new ParaHomeScene(objTransformInfo[i]);
        }
        return true;
    }

    public void LoadSceneObjects(int i)
    {
        if (m_sceneObjects == null)
        {
            if (!LoadSequenceSceneObjects())
            {
                Debug.LogError("ParaHomeLoader.LoadScene(): Unable to load scene without scene information.");
                return;
            }
        }

        if (i < 0 || i >= m_sceneObjects.Length)
        {
            Debug.LogError("ParaHomeLoader.LoadScene(): Invalid frame index: " + i);
            return;
        }

        ParaHomeScene scene = m_sceneObjects[i];
        ParaHomeObject[] objects = scene.Objects;
        foreach (ParaHomeObject obj in objects)
        {
            Transform objTransform = m_environment.Find(obj.Name);
            if (objTransform != null)
            {
                ParaHomeObjectPart objBase = obj.Base;
                objTransform.gameObject.SetActive(true);
                objTransform.position = objBase.Position;
                objTransform.rotation = objBase.Rotation;

                Transform part1Transform = objTransform.Find("part1");
                if (part1Transform != null)
                {
                    ParaHomeObjectPart part1 = obj.Part1;
                    if (part1 != null)
                    {
                        part1Transform.position = part1.Position;
                        part1Transform.rotation = part1.Rotation;
                    }
                }

                Transform part2Transform = objTransform.Find("part2");
                if (part2Transform != null)
                {
                    ParaHomeObjectPart part2 = obj.Part2;
                    if (part2 != null)
                    {
                        part2Transform.position = part2.Position;
                        part2Transform.rotation = part2.Rotation;
                    }
                }
            }
        }
    }


    private IEnumerator SequenceCoroutine()
    {
        int numFrames = m_sceneObjects.Length;
        while (m_currentFrame < numFrames)
        {
            LoadSceneObjects(m_currentFrame);
            LoadScenePoses(m_currentFrame);
            m_currentFrame++;
            yield return new WaitForSeconds(Time.deltaTime);
        }
        m_sequenceCoroutine = null;
    }

    #endregion

    #region Public Methods

    public void ClearEnvironment()
    {
        if (m_environment == null)
        {
            Debug.LogError("ParaHomeLoader.ClearEnvironment(): Environment not set.");
            return;
        }
        int numObjs = m_environment.childCount;
        for (int i = numObjs - 1; i >= 0; i--)
        {
            DestroyImmediate(m_environment.GetChild(i).gameObject);
        }
    }

    public bool LoadEnvironment()
    {
        if (m_environment == null)
        {
            Debug.LogError("ParaHomeLoader.LoadEnvironment(): Environment not set.");
            return false;
        }

        ClearEnvironment();

        string scansDir = Path.Combine(Application.streamingAssetsPath, m_rootDir, m_scanDir);
        if (!Directory.Exists(scansDir))
        {
            Debug.LogError("Scans directory not found: " + scansDir);
            return false;
        }

        string objColorPath = Path.Combine(scansDir, "color.json");
        string objColorJson = File.ReadAllText(objColorPath);
        Dictionary<string, Dictionary<string, float[]>> sceneObjColors = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, float[]>>>(objColorJson);
        string[] scanDirs = Directory.GetDirectories(scansDir);
        int numScans = scanDirs.Length;
        for (int i = 0; i < numScans; i++)
        {

            string scanDir = scanDirs[i];
            // replace all backslashes with forward slashes
            scanDir = scanDir.Replace('/', '\\');
            string objName = scanDir.Split(Path.DirectorySeparatorChar)[^1];
            Debug.Log(objName);

            GameObject scanObj = new GameObject(objName);

            string scanObjBasePath = Path.Combine(scanDir, "simplified", "base.obj");
            GameObject scanObjBase = LoadEnvironmentObject(scanObjBasePath);
            scanObjBase.name = "base";
            float[] baseColor = sceneObjColors[objName]["base"];
            scanObjBase.GetComponent<Renderer>().sharedMaterial.color = new Color(baseColor[0], baseColor[1], baseColor[2]);
            scanObjBase.transform.SetParent(scanObj.transform);

            string scanObjPart1Path = Path.Combine(scanDir, "simplified", "part1.obj");
            GameObject scanObjPart1 = LoadEnvironmentObject(scanObjPart1Path);
            if (scanObjPart1 != null)
            {
                scanObjPart1.name = "part1";
                float[] part1Color = sceneObjColors[objName]["part1"];
                scanObjPart1.GetComponent<Renderer>().sharedMaterial.color = new Color(part1Color[0], part1Color[1], part1Color[2]);
                scanObjPart1.transform.SetParent(scanObj.transform);
            }

            string scanObjPart2Path = Path.Combine(scanDir, "simplified", "part2.obj");
            GameObject scanObjPart2 = LoadEnvironmentObject(scanObjPart2Path);
            if (scanObjPart2 != null)
            {
                scanObjPart2.name = "part2";
                float[] part2Color = sceneObjColors[objName]["part2"];
                scanObjPart2.GetComponent<Renderer>().sharedMaterial.color = new Color(part2Color[0], part2Color[2], part2Color[2]);
                scanObjPart2.transform.SetParent(scanObj.transform);
            }

            scanObj.SetActive(false);
            scanObj.transform.SetParent(m_environment);
        }

        return true;
    }

    public void LoadScenes()
    {
        LoadSequencePoses();
        LoadSequenceSceneObjects();
    }

    public void ClearScenes()
    {
        StopSequence();
        m_poses = null;
        m_sceneObjects = null;
    }

    public void LoadScene(int i)
    {
        LoadSceneObjects(i);
        LoadScenePoses(i);
    }

    public bool ScenesLoaded
    {
        get { return m_poses != null; }
    }

    public int NumFrames
    {
        get { return m_poses.Length; }
    }

    public int CurrentFrame
    {
        get { return m_currentFrame; }
        set { m_currentFrame = value; }
    }

    public bool PlayingSequence
    {
        get { return m_sequenceCoroutine != null; }
    }

    public void PlaySequence()
    {
        if (!ScenesLoaded)
        {
            Debug.LogError("ParaHomeLoader.PlaySequence(): Scenes not loaded.");
            return;
        }
        m_sequenceCoroutine = StartCoroutine(SequenceCoroutine());
    }

    public void StopSequence()
    {
        if (m_sequenceCoroutine != null)
        {
            StopCoroutine(m_sequenceCoroutine);
            m_sequenceCoroutine = null;
        }
    }

    #endregion

}

[CustomEditor(typeof(ParaHomeLoader))]
public class ParaHomeLoaderEditor : Editor
{
    ParaHomeLoader paraHomeLoader;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        paraHomeLoader = (ParaHomeLoader)target;

        EditorGUILayout.LabelField("Environment", EditorStyles.boldLabel);
        if (GUILayout.Button("Load Environment"))
        {
            paraHomeLoader.LoadEnvironment();
            EditorUtility.SetDirty(paraHomeLoader);
        }
        if (GUILayout.Button("Clear Environment"))
        {
            paraHomeLoader.ClearEnvironment();
            paraHomeLoader.m_avatar.Reset();
            EditorUtility.SetDirty(paraHomeLoader);
        }

        EditorGUILayout.LabelField("Sequence", EditorStyles.boldLabel);

        if (paraHomeLoader.ScenesLoaded)
        {
            EditorGUI.BeginChangeCheck();
            paraHomeLoader.CurrentFrame = EditorGUILayout.IntSlider("Frame", paraHomeLoader.CurrentFrame, 0, paraHomeLoader.NumFrames - 1);
            if (EditorGUI.EndChangeCheck())
            {
                paraHomeLoader.LoadScene(paraHomeLoader.CurrentFrame);
            }
            if (Application.isPlaying)
            {
                if (paraHomeLoader.PlayingSequence)
                {
                    if (GUILayout.Button("Stop Sequence"))
                    {
                        paraHomeLoader.StopSequence();
                    }
                }
                else
                {
                    if (GUILayout.Button("Play Sequence"))
                    {
                        paraHomeLoader.PlaySequence();
                    }
                }
            }
            if (GUILayout.Button("Clear Sequence"))
            {
                paraHomeLoader.ClearScenes();
                paraHomeLoader.m_avatar.Reset();
            }
        }
        else
        {
            if (GUILayout.Button("Load Scenes"))
            {
                paraHomeLoader.LoadScenes();
            }
        }
    }
}

