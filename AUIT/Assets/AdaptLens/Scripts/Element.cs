using UnityEngine;

public class Element : MonoBehaviour
{
    private const string LAYER = "Element";
    private Material m_mat;
    private Color m_originalColor;
    private Color m_highlightColor = Color.white;
    private Color m_hideColor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Init()
    {
        Renderer r = GetComponent<Renderer>();
        if (r == null)
        {
            r = GetComponentInChildren<Renderer>();
        }
        if (r == null)
        {
            Debug.LogError("Element: No renderer found");
            return;
        }
        m_mat = r.material;
        m_originalColor = m_mat.color;
        m_hideColor = new Color(m_originalColor.r, m_originalColor.g, m_originalColor.b, 0.05f);

        gameObject.layer = LayerMask.NameToLayer(LAYER);
    }

    public void SetHighlight()
    {
        m_mat.color = m_highlightColor;
    }

    public void SetHide()
    {
        m_mat.color = m_hideColor;
    }

    public void SetOriginal()
    {
        m_mat.color = m_originalColor;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
