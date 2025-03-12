using UnityEngine;

public class Element : MonoBehaviour
{
    private Material m_mat;
    private Color m_originalColor;
    private Color m_highlightColor = new Color(158 / 255f, 202 / 255f, 225 / 255f, 1);
    private Color m_hideColor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Init()
    {
        m_mat = GetComponent<Renderer>().material;
        m_originalColor = m_mat.color;
        m_hideColor = new Color(m_originalColor.r, m_originalColor.g, m_originalColor.b, 0.2f);
    }

    public void SetHighlight()
    {
        Debug.Log("Set Highlight");
        m_mat.color = m_highlightColor;
    }

    public void SetHide()
    {
        Debug.Log("Set Hide");
        m_mat.color = m_hideColor;
    }

    public void SetOriginal()
    {
        Debug.Log("Set Original");
        m_mat.color = m_originalColor;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
