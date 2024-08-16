using UnityEngine;

public class IdeaButton : MonoBehaviour, ITappable
{
    private IdeaMaster ideaMaster;

    void Start()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        ideaMaster = stageObject.GetComponent<IdeaMaster>();
    }

    public void OnTap(TouchInfo touchInfo)
    {
        ideaMaster.Ideate(caller: gameObject);
    }
}
