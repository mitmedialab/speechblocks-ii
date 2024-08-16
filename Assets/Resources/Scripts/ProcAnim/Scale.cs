using UnityEngine;
using System;
using System.Collections;

public class Scale : MonoBehaviour, IAnimation
{
    public static string UpdateType() { return "scale"; }
    private Vector3 startScale;
    private Vector3 endScale;
    private float duration;
    private double startTime;
    private bool isGoing;
    private Func<float, float> easing = null;

    public void Init(params object[] args)
    {
        endScale = (Vector3)args[0];
        duration = (float)args[1];
        if (args.Length > 2) { easing = (Func<float, float>)args[2]; }
    }

    public void Start()
    {
        //Debug.Log("STARTED");
        isGoing = true;
        startTime = TimeKeeper.time;
        startScale = gameObject.transform.localScale;
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

    public Vector2 GetStart()
    {
        return startScale;
    }

    public Vector2 GetEnd()
    {
        return endScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isGoing) return;
        float progress = Math.Min(1.0f, (float)(TimeKeeper.time - startTime) / duration);
        float progressE = (null != easing) ? easing(progress) : progress;
        //Debug.Log("PROGRESS: " + progress);
        Vector3 scale = Vector3.Lerp(startScale, endScale, progressE);
        gameObject.transform.localScale = scale;
        Logging.LogScale(gameObject, "scale");
        if (1.0f <= progress)
        {
            isGoing = false;
            Destroy(this);
            //Debug.Log("FINISHED");
        }
    }
}
