using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using Dummiesman;
using UnityEditor;
using System;
using UnityEngine.Events;

public class BoundingBox
{
    public string name;
    public string part;
    public float[] position;
    public float[] rotation;
    public float[] scale;
}

public class ParaHomeLoader : MonoBehaviour
{
    #region Delegates 

    public delegate void OnScenesLoaded(List<ParaHomeContext> contexts);
    public OnScenesLoaded onScenesLoaded;

    #endregion


    #region Events 

    public UnityEvent sceneLoadedEvent = new UnityEvent();

    #endregion 

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

    // References
    public ParaHomeAvatar m_avatar;
    public Transform m_environment;
    public Material m_objMaterial;
    public GameObject m_boundingBoxPrefab;

    // Settings
    public enum LoadOptions
    {
        Sequence,
        Saved
    }
    public LoadOptions m_loadOption = LoadOptions.Saved;
    public string m_rootDir = "ParaHome";
    public string m_scanDir = "data/scan";
    public string m_seqDir = "data/seq";
    public string m_seq = "s1";
    public string m_savedDir = "data/saved";
    public bool m_loadOnStart = true;


    public Vector3 m_offsetPos = Vector3.zero;
    public Vector3 m_offsetRot = Vector3.zero;



    #endregion

    #region Private Fields

    private ParaHomeScene[] m_sceneObjects;

    private ParaHomeAvatarPose[] m_poses;

    private int m_currentFrame;

    private Coroutine m_sequenceCoroutine;

    private List<ParaHomeContext> m_contexts;
    public List<ParaHomeContext> Contexts
    {
        get { return m_contexts; }
    }

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
        LoadScenePoses(m_poses[i]);
    }

    private bool LoadSequenceSceneObjects()
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

    
    private void LoadSceneObjects(int i)
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
        LoadSceneObjects(scene);
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

    // Save bounding box information to a json file
    private void SaveBoundingBoxes()
    {
        List<BoundingBox> boundingBoxes = new List<BoundingBox>();
        foreach (Transform obj in m_environment)
        {
            string objName = obj.name;
            foreach (Transform part in obj)
            {
                string partName = part.name;
                Transform bounds = part.Find("bounds");
                if (bounds != null)
                {
                    BoundingBox boundingBox = new BoundingBox();
                    boundingBox.name = objName;
                    boundingBox.part = partName;
                    boundingBox.position = new float[] { bounds.localPosition[0], bounds.localPosition[1], bounds.localPosition[2] };
                    boundingBox.rotation = new float[] { bounds.localRotation[0], bounds.localRotation[1], bounds.localRotation[2], bounds.localRotation[3] };
                    boundingBox.scale = new float[] { bounds.localScale[0], bounds.localScale[1], bounds.localScale[2] };
                    boundingBoxes.Add(boundingBox);
                }
            }
            
            
        }

        string json = JsonConvert.SerializeObject(boundingBoxes, Formatting.Indented);
        //string path = Path.Combine(Application.streamingAssetsPath, m_rootDir, m_scanDir, "bounds.json");
        File.WriteAllText("bounds.json", json);
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

        // Load the bounding boxes
        string boundsPath = Path.Combine(scansDir, "bounds.json");
        string boundsJson = File.ReadAllText(boundsPath);
        List<BoundingBox> bounds = JsonConvert.DeserializeObject<List<BoundingBox>>(boundsJson);
        foreach (BoundingBox boundingBox in bounds)
        {
            Transform obj = m_environment.Find(boundingBox.name);
            if (obj != null)
            {
                Transform part = obj.Find(boundingBox.part);
                if (part != null)
                {
                    Vector3 position = new Vector3(boundingBox.position[0], boundingBox.position[1], boundingBox.position[2]);
                    Quaternion rotation = new Quaternion(boundingBox.rotation[0], boundingBox.rotation[1], boundingBox.rotation[2], boundingBox.rotation[3]);
                    Vector3 scale = new Vector3(boundingBox.scale[0], boundingBox.scale[1], boundingBox.scale[2]);
                    GameObject boundsObj = Instantiate(m_boundingBoxPrefab);
                    boundsObj.name = "bounds";
                    boundsObj.transform.SetParent(part);
                    boundsObj.transform.localPosition = position;
                    boundsObj.transform.localRotation = rotation;
                    boundsObj.transform.localScale = scale;
                }
            }
        }

        return true;
    }

    public void LoadSceneObjects(ParaHomeScene scene)
    {
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

                // Apply transformation to the object 
                objTransform.position += m_offsetPos;
                Quaternion offsetRot = Quaternion.Euler(m_offsetRot);
                objTransform.position = offsetRot * objTransform.position;
                objTransform.rotation = offsetRot * objTransform.rotation;
            }
        }
    }

    public void LoadScenePoses(ParaHomeAvatarPose pose)
    {
        Quaternion offsetRot = Quaternion.Euler(m_offsetRot);
        m_avatar.SetPose(pose, m_offsetPos, offsetRot);
    }


    public void LoadScenes()
    {
        LoadSequencePoses();
        LoadSequenceSceneObjects();

        m_contexts = new List<ParaHomeContext>();
        for (int i = 0; i < m_poses.Length && i < m_sceneObjects.Length; i++)
        {
            ParaHomeContext context = new ParaHomeContext(m_poses[i], m_sceneObjects[i]);
            m_contexts.Add(context);
        }
        if (onScenesLoaded != null)
        {
            onScenesLoaded(m_contexts);
        }
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

        sceneLoadedEvent?.Invoke();
    }

    public ParaHomeScene CurrentScene
    {
        get { 
            if (m_sceneObjects == null || m_currentFrame < 0 || m_currentFrame >= m_sceneObjects.Length)
            {
                return null;
            }
            return m_sceneObjects[m_currentFrame]; 
        }
    }

    public ParaHomeAvatarPose CurrentPose
    {
        get
        {
            if (m_poses == null || m_currentFrame < 0 || m_currentFrame >= m_poses.Length)
            {
                return null;
            }
            return m_poses[m_currentFrame];
        }
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

    public void SaveScene()
    {
        if (m_poses == null)
        {
            if (!LoadSequencePoses())
            {
                Debug.LogError("ParaHomeLoader.SaveScene(): Unable to load scene without pose information.");
                return;
            }
        }
        if (m_sceneObjects == null)
        {
            if (!LoadSequenceSceneObjects())
            {
                Debug.LogError("ParaHomeLoader.LoadScene(): Unable to load scene without scene information.");
                return;
            }
        }
        if (m_currentFrame >= m_poses.Length || m_currentFrame >= m_sceneObjects.Length || m_currentFrame < 0)
        {
            Debug.LogError("ParaHomeLoader.SaveScene(): Invalid frame index: " + m_currentFrame);
            return;
        }
        ParaHomeAvatarPose pose = m_poses[m_currentFrame];
        ParaHomeScene scene = m_sceneObjects[m_currentFrame];
        ParaHomeAvatarPoseInfo poseInfo = pose.PoseInfo;
        Dictionary<string, Dictionary<string, float[]>> environmentInfo = scene.EnvironmentInfo;
        ParaHomeSceneInfo sceneInfo = new ParaHomeSceneInfo();
        sceneInfo.poseInfo = poseInfo;
        sceneInfo.environmentInfo = environmentInfo;
        string sceneJson = JsonConvert.SerializeObject(sceneInfo, Formatting.Indented);
        string savedDir = Path.Combine(Application.streamingAssetsPath, m_rootDir, m_savedDir);
        // check if directory exists, create otherwise
        if (!Directory.Exists(savedDir))
        {
            Directory.CreateDirectory(savedDir);
        }
        // count number of files in the directory
        int numFiles = Directory.GetFiles(savedDir, $"*.json").Length;
        string path = Path.Combine(savedDir, $"scene_{numFiles + 1}.json");
        Debug.Log($"Saving scene to {path}");
        File.WriteAllText(path, sceneJson);
    }

    public void LoadSavedScenes()
    {
        string savedDir = Path.Combine(Application.streamingAssetsPath, m_rootDir, m_savedDir);
        if (!Directory.Exists(savedDir))
        {
            Debug.LogError("Saved directory not found: " + savedDir);
            return;
        }
        string[] sceneFiles = Directory.GetFiles(savedDir, $"*.json");
        m_poses = new ParaHomeAvatarPose[sceneFiles.Length];
        m_sceneObjects = new ParaHomeScene[sceneFiles.Length];
        for (int i = 0; i < sceneFiles.Length; i++)
        {
            string sceneJson = File.ReadAllText(sceneFiles[i]);
            ParaHomeSceneInfo sceneInfo = JsonConvert.DeserializeObject<ParaHomeSceneInfo>(sceneJson);
            m_poses[i] = new ParaHomeAvatarPose(sceneInfo.poseInfo);
            m_sceneObjects[i] = new ParaHomeScene(sceneInfo.environmentInfo);
        }

        
        m_contexts = new List<ParaHomeContext>();
        for (int i = 0; i < m_poses.Length && i < m_sceneObjects.Length; i++)
        {
            ParaHomeContext context = new ParaHomeContext(m_poses[i], m_sceneObjects[i]);
            m_contexts.Add(context);
        }
        if (onScenesLoaded != null)
        {
            onScenesLoaded(m_contexts);
        }
    }

    private void Start()
    {
        if (m_loadOnStart)
        {
            switch (m_loadOption)
            {
                case LoadOptions.Sequence:
                    LoadScenes();
                    break;
                case LoadOptions.Saved:
                    LoadSavedScenes();
                    LoadScene(0);
                    break;
            }
        }
    }

    #endregion


}

[CustomEditor(typeof(ParaHomeLoader))]
public class ParaHomeLoaderEditor : Editor
{
    ParaHomeLoader paraHomeLoader;
    SerializedProperty sceneLoadedEventProperty;

    public void LoadSequenceGUI()
    {
        paraHomeLoader.m_scanDir = EditorGUILayout.TextField("Scan Directory", paraHomeLoader.m_scanDir);
        paraHomeLoader.m_seqDir = EditorGUILayout.TextField("Sequence Directory", paraHomeLoader.m_seqDir);
        paraHomeLoader.m_seq = EditorGUILayout.TextField("Sequence", paraHomeLoader.m_seq);

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
            if (GUILayout.Button("Save Scene"))
            {
                paraHomeLoader.SaveScene();
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

    public void LoadSavedGUI()
    {
        paraHomeLoader.m_savedDir = EditorGUILayout.TextField("Saved Directory", paraHomeLoader.m_savedDir);
        if (paraHomeLoader.ScenesLoaded)
        {
            EditorGUI.BeginChangeCheck();
            paraHomeLoader.CurrentFrame = EditorGUILayout.IntSlider("Frame", paraHomeLoader.CurrentFrame, 0, paraHomeLoader.NumFrames - 1);
            if (EditorGUI.EndChangeCheck())
            {
                paraHomeLoader.LoadScene(paraHomeLoader.CurrentFrame);
            }

            if (GUILayout.Button("Clear Scenes"))
            {
                paraHomeLoader.ClearScenes();
                paraHomeLoader.m_avatar.Reset();
            }
        }
        else
        {
            if (GUILayout.Button("Load Scenes"))
            {
                paraHomeLoader.LoadSavedScenes();
            }
        }
    }

    private void OnEnable()
    {
        paraHomeLoader = (ParaHomeLoader)target;
        sceneLoadedEventProperty = serializedObject.FindProperty("sceneLoadedEvent");
    }


    public override void OnInspectorGUI()
    {

        paraHomeLoader = (ParaHomeLoader)target;

        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        paraHomeLoader.m_avatar = (ParaHomeAvatar)EditorGUILayout.ObjectField("Avatar", paraHomeLoader.m_avatar, typeof(ParaHomeAvatar), true);
        paraHomeLoader.m_environment = (Transform)EditorGUILayout.ObjectField("Environment", paraHomeLoader.m_environment, typeof(Transform), true);
        paraHomeLoader.m_objMaterial = (Material)EditorGUILayout.ObjectField("Object Material", paraHomeLoader.m_objMaterial, typeof(Material), true);
        paraHomeLoader.m_boundingBoxPrefab = (GameObject)EditorGUILayout.ObjectField("Bounding Box Prefab", paraHomeLoader.m_boundingBoxPrefab, typeof(GameObject), true);

        EditorGUILayout.Space(10);

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

        EditorGUILayout.Space(10);

        // Include dropdown here 
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        paraHomeLoader.m_loadOnStart = EditorGUILayout.Toggle("Load On Start", paraHomeLoader.m_loadOnStart);
        paraHomeLoader.m_loadOption = (ParaHomeLoader.LoadOptions)EditorGUILayout.EnumPopup("Load Setting", paraHomeLoader.m_loadOption);
        paraHomeLoader.m_rootDir = EditorGUILayout.TextField("Root Directory", paraHomeLoader.m_rootDir);


        switch (paraHomeLoader.m_loadOption) {
            case ParaHomeLoader.LoadOptions.Sequence:
                LoadSequenceGUI();
                break;
            case ParaHomeLoader.LoadOptions.Saved:
                LoadSavedGUI();
                break;
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(sceneLoadedEventProperty, new GUIContent("Scene Loaded Event"), true);
        serializedObject.ApplyModifiedProperties();


    }
}

