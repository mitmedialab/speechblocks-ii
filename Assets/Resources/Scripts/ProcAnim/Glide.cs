using UnityEngine;
using System;
using System.Collections;

public class Glide : MonoBehaviour, IAnimation
{
    public static string UpdateType() { return "position"; }
    private Vector2 startPosition;
    private Vector2 endPosition;
    private float duration;
    private double startTime;
    private float z;
    private bool isGoing;
    private Func<float, float> easing = null;

    public void Init(params object[] args)
    {
        //Debug.Log("INITIALIZING...");
        endPosition = (Vector2)args[0];
        z = gameObject.transform.position.z;
        duration = (float)args[1];
        if (args.Length > 2) { easing = (Func<float, float>)args[3]; }
        //Debug.Log("INITIALIZED");
    }

    public void Start()
    {
        //Debug.Log("STARTED");
        isGoing = true;
        startTime = TimeKeeper.time;
        startPosition = transform.position;
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
        return startPosition;
    }

    public Vector2 GetEnd()
    {
        return endPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isGoing) return;
        float progress = Math.Min(1.0f, (float)(TimeKeeper.time - startTime) / duration);
        float progressE = (null != easing) ? easing(progress) : progress;
        //Debug.Log("PROGRESS: " + progress);
        Vector2 position = Vector2.Lerp(startPosition, endPosition, progressE);
        gameObject.transform.position = new Vector3(position.x, position.y, z);
        Logging.LogMovement(gameObject, "glide");
        if (1.0f <= progress)
        {
            isGoing = false;
            Destroy(this);
            //Debug.Log("FINISHED");
        }
    }
}
