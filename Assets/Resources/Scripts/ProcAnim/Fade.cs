using UnityEngine;
using System;
using System.Collections;

public class Fade : MonoBehaviour, IAnimation
{
    public static string UpdateType() { return "color"; }
    private float startOpacity = 1;
    private float endOpacity = 1;
    private float duration;
    private double startTime;
    private bool isGoing;

    public void Init(params object[] args)
    {
        //Debug.Log("INITIALIZING...");
        endOpacity = (float)args[0];
        duration = (float)args[1];
        //Debug.Log("INITIALIZED");
    }

    public void Start()
    {
        //Debug.Log("STARTED");
        isGoing = true;
        startOpacity = Opacity.GetOpacity(gameObject);
        startTime = TimeKeeper.time;
    }

    public void Stop()
    {
        //Debug.Log("STOPPED");
        Destroy(this);
    }

    public bool IsGoing()
    {
        return isGoing;
    }

    public float GetStart()
    {
        return startOpacity;
    }

    public float GetEnd()
    {
        return endOpacity;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isGoing) return;
        float progress = Math.Min(1.0f, (float)(TimeKeeper.time - startTime) / duration);
        //Debug.Log("PROGRESS: " + progress);
        float opacity = (1 - progress) * startOpacity + progress * endOpacity;
        Opacity.SetOpacity(gameObject, opacity);
        if (1.0f <= progress)
        {
            isGoing = false;
            Destroy(this);
            //Debug.Log("FINISHED");
        }
    }

}
