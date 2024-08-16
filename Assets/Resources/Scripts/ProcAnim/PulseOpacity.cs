using UnityEngine;
using System;
using System.Collections;

public class PulseOpacity : MonoBehaviour, IAnimation
{
    public static string UpdateType() { return "opacity"; }
    [SerializeField]
    private float min;
    [SerializeField]
    private float max;
    [SerializeField]
    private float period;
    private double startTime;

    public void Init(params object[] args)
    {
        //Debug.Log("INITIALIZING...");
        min = (float)args[0];
        max = (float)args[1];
        period = (float)args[2];
        //Debug.Log("INITIALIZED");
    }

    public void Start()
    {
        //Debug.Log("STARTED");
        startTime = TimeKeeper.time;
    }

    public void Stop()
    {
        //Debug.Log("STOPPED");
        Destroy(this);
    }

    public bool IsGoing()
    {
        return true;
    }

    // Update is called once per frame
    void Update()
    {
        float progress = (float)((TimeKeeper.time - startTime) % period) / period;
        float alpha = 0.5f * (min + max) + 0.5f * (max - min) * Mathf.Sin(2 * Mathf.PI * progress);
        Opacity.SetOpacity(gameObject, alpha);
    }
}
