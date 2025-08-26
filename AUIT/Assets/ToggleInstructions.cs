using UnityEngine;

public class ToggleInstructions : MonoBehaviour
{
    public GameObject keyBox;
    public GameObject keyPicker;

    public void toggleKeyBox()
    {
        keyBox.SetActive(!keyBox.activeSelf);
        keyPicker.SetActive(!keyPicker.activeSelf);
    }
}
