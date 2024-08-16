using UnityEngine;

public class ErrorOnDemand : MonoBehaviour, ITappable
{
    private Environment environment = null;

    public void OnTap(TouchInfo touchInfo)
    {
        environment.GetUser();
    }
}
