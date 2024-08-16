using UnityEngine;
using System;
using System.Collections;

public class PulseSize : MonoBehaviour, IAnimation
{
    public static string UpdateType() { return "scale"; }
    private float max;
    private float duration;
    private double startTime;
    private int iterations;
    private Vector3 scale;
    private bool isGoing = false;

    public void Init(params object[] args)
    {
        //Debug.Log("INITIALIZING...");
        max = (float)args[0];
        duration = (float)args[1];
        iterations = (int)args[2];
        //Debug.Log("INITIALIZED");
    }

    public void Start()
    {
        //Debug.Log("STARTED");
        startTime = TimeKeeper.time;
        scale = gameObject.transform.localScale;
        isGoing = true;
    }

    public void Stop()
    {
        //Debug.Log("STOPPED");
        gameObject.transform.localScale = scale;
        Logging.LogScale(gameObject, "pulse");
        Destroy(this);
        isGoing = false;
    }

    public bool IsGoing()
    {
        return isGoing;
    }

    // Update is called once per frame
    void Update()
    {
        float progress = (float)(TimeKeeper.time - startTime) / duration;
        float scale = 1 + (max - 1) * applyHop(progress);
        gameObject.transform.localScale = new Vector3(scale, scale, 1);
        Logging.LogScale(gameObject, "pulse");
    }

    private float applyHop(float a)
    {
        float hopT = iterations * a;
        int hopTurn = (int)hopT;
        hopT -= hopTurn;
        float height = Mathf.Exp((float)(-1.01f * hopTurn));
        return height * (1 - 4f * Mathf.Pow(hopT - 1f / 2f, 2));
    }
}
