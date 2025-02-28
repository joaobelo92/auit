using UnityEngine;
using UnityEditor;
using AUIT.AdaptationObjectives.Definitions;

public class VoxelView : MonoBehaviour
{
    #region Public Fields

    [Header("References")]
    public AUIT.AUIT m_auit;
    public Transform m_bounds;

    [Header("Settings")]
    [Range(0.1f, 1f)]
    public float m_voxelSize;
    [Range(0.01f, 1f)]
    public float m_voxelMargin;

    #endregion

    #region Private Fields

    private GameObject m_voxelObj; 
    private Voxel[,,] m_voxels;
    private Vector3Int m_voxelDims;

    private Vector3 m_prevBounds;
    private float m_prevVoxelSize; 
    private float m_prevVoxelMargin;

    #endregion

    #region Private Methods

    // Check if the parameters are valid
    private bool IsParamsValid() {
        if (m_voxelMargin >= m_voxelSize) {
            Debug.LogError("VoxelView.IsParamsValid(): Voxel margin must be smaller than voxel size.");
            return false;
        }
        Vector3 bounds = m_bounds.localScale;
        if (bounds.x <= 0 || bounds.y <= 0 || bounds.z <= 0)
        {
            Debug.LogError("VoxelView.IsParamsValid(): Bounds must be positive.");
            return false;
        }
        if (bounds.x < m_voxelSize || bounds.y < m_voxelSize || bounds.z < m_voxelSize)
        {
            Debug.LogError("VoxelView.IsParamsValid(): Bounds must be larger than voxel size.");
            return false;
        }
        return true; 
    }

    // Initialize voxels
    private void Init() {
        m_voxelObj = transform.Find("Voxel").gameObject;
        m_prevBounds = m_bounds.localScale;
        m_prevVoxelSize = m_voxelSize;
        m_prevVoxelMargin = m_voxelMargin;

        InitVoxelGrid();
    }

    // Calculate the dimensions of the voxel grid
    private Vector3Int CalculateVoxelDimensions() {
        return Vector3Int.Max(Vector3Int.one, Vector3Int.FloorToInt(m_bounds.localScale / m_voxelSize));
    }

    // Initialize the voxel grid
    private void InitVoxelGrid() {
        if (!IsParamsValid())
        {
            return;
        }
        m_voxelDims = CalculateVoxelDimensions();

        Vector3 voxelSize = new Vector3(m_bounds.localScale.x / m_voxelDims.x,
            m_bounds.localScale.y / m_voxelDims.y,
            m_bounds.localScale.z / m_voxelDims.z);
        Vector3 offset = - m_bounds.localScale / 2f + 0.5f * voxelSize;
        
        m_voxels = new Voxel[m_voxelDims.x, m_voxelDims.y, m_voxelDims.z];
        for (int x = 0; x < m_voxelDims.x; x++) {
            for (int y = 0; y < m_voxelDims.y; y++) {
                for (int z = 0; z < m_voxelDims.z; z++) {
                    GameObject voxelObj = Instantiate(m_voxelObj, transform);
                    voxelObj.name = $"Voxel_{x}_{y}_{z}";
                    voxelObj.transform.localPosition = new Vector3(x * voxelSize.x, y * voxelSize.y, z * voxelSize.z) + offset;
                    voxelObj.transform.localScale = (m_voxelSize - m_voxelMargin) * Vector3.one;
                    voxelObj.SetActive(true);
                    Voxel voxel = voxelObj.AddComponent<Voxel>();
                    m_voxels[x, y, z] = voxel;
                }
            }
        }
    }

    // Update the voxel grid
    private Voxel[,,] UpdateVoxelGrid(Vector3Int dims)
    {
        // Reinitialize voxel grid if dimensions changed
        Voxel[,,] voxels = new Voxel[dims.x, dims.y, dims.z];

        // Destroy all voxels outside new dimensions
        for (int x = 0; x < m_voxelDims.x; x++)
        {
            for (int y = 0; y < m_voxelDims.y; y++)
            {
                for (int z = 0; z < m_voxelDims.z; z++)
                {
                    // If this voxel is outside the new dimensions in ANY dimension, destroy it
                    if (x >= dims.x || y >= dims.y || z >= dims.z)
                    {
                        Destroy(m_voxels[x, y, z].gameObject);
                    }
                }
            }
        }

        // Repurpose or create new voxels
        for (int x = 0; x < dims.x; x++)
        {
            for (int y = 0; y < dims.y; y++)
            {
                for (int z = 0; z < dims.z; z++)
                {
                    if (x < m_voxelDims.x && y < m_voxelDims.y && z < m_voxelDims.z)
                    {
                        voxels[x, y, z] = m_voxels[x, y, z];
                    }
                    else
                    {
                        GameObject voxelObj = Instantiate(m_voxelObj, transform);
                        voxelObj.SetActive(true);
                        Voxel voxel = voxelObj.AddComponent<Voxel>();
                        voxels[x, y, z] = voxel;
                    }
                    voxels[x, y, z].gameObject.name = $"Voxel_{x}_{y}_{z}";
                }
            }
        }

        return voxels;
    }

    // Check if the voxel grid needs to be updated
    private bool IsUpdateVoxels()
    {
        return m_bounds.localScale != m_prevBounds ||
            m_voxelSize != m_prevVoxelSize ||
            m_voxelMargin != m_prevVoxelMargin;
    }

    // Update voxels
    private void UpdateVoxels() {
        if (m_voxels == null)
        {
            InitVoxelGrid();
        }

        transform.localPosition = m_bounds.localPosition;
        transform.localRotation = m_bounds.localRotation;

        if (IsParamsValid() && IsUpdateVoxels())
        {
            Vector3Int dims = CalculateVoxelDimensions();

            if (dims != m_voxelDims)
            {
                m_voxels = UpdateVoxelGrid(dims);
                m_voxelDims = dims;
            }

            Vector3 voxelSize = new Vector3(m_bounds.localScale.x / m_voxelDims.x,
                m_bounds.localScale.y / m_voxelDims.y,
                m_bounds.localScale.z / m_voxelDims.z);
            Vector3 offset = -m_bounds.localScale / 2f + 0.5f * voxelSize;
            for (int x = 0; x < m_voxelDims.x; x++)
            {
                for (int y = 0; y < m_voxelDims.y; y++)
                {
                    for (int z = 0; z < m_voxelDims.z; z++)
                    {
                        m_voxels[x, y, z].transform.localPosition = new Vector3(x * voxelSize.x, y * voxelSize.y, z * voxelSize.z) + offset;
                        m_voxels[x, y, z].transform.localScale = (m_voxelSize - m_voxelMargin) * Vector3.one;
                    }
                }
            }
        }

        m_prevBounds = m_bounds.localScale;
        m_prevVoxelSize = m_voxelSize;
        m_prevVoxelMargin = m_voxelMargin;
    }

    #endregion

    #region Public Methods

    public void VisualizeCosts()
    {
        Layout voxel = new Layout(Vector3.zero);
        for (int x = 0; x < m_voxelDims.x; x++)
        {
            for (int y = 0; y < m_voxelDims.y; y++)
            {
                for (int z = 0; z < m_voxelDims.z; z++)
                {
                    Vector3 position = m_voxels[x, y, z].transform.position;
                    voxel.Position = position;
                    float cost = m_auit.ComputeCost(voxel, true);
                    Debug.Log(cost);
                }
            }
        }

    }

    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVoxels();
    }
}

[CustomEditor(typeof(VoxelView))]
public class VoxelViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        VoxelView voxelView = (VoxelView)target;
        if (GUILayout.Button("Visualize Costs"))
        {
            voxelView.VisualizeCosts();
        }
    }
}