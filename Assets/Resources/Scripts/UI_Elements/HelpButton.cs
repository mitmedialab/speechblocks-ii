using UnityEngine;

public class HelpButton : MonoBehaviour, ITappable
{
    [SerializeField]
    private GameObject standardMode = null;
    [SerializeField]
    private GameObject jiboMode = null;

    public void OnTap(TouchInfo touchInfo)
    {
        GameObject.FindWithTag("StageObject").GetComponent<Tutorial>().TriggerHelp();
    }

    public void ActivateJiboMode()
    {
        standardMode.SetActive(false);
        jiboMode.SetActive(true);
    }
}
