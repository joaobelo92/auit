using UnityEngine;

public class Voxel : MonoBehaviour
{
    #region Private Fields 

    private Material m_mat; 

    #endregion

    #region Private Methods

    private void Init() {
        m_mat = GetComponent<MeshRenderer>().material;
    }

    #endregion

    #region Public Methods

    public void SetColor(Color color)
    {
        m_mat.color = color;
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
        
    }
}
