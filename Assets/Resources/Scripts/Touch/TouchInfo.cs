using UnityEngine;
using System.Collections.Generic;

public class TouchInfo
{
    public TouchInfo(int touchId, double touchStart, Vector2 iniPos)
    {
        this.touchId = touchId;
        this.touchStart = touchStart;
        this.initialPos.Set(iniPos.x, iniPos.y);
        trace.Add(new Vector2(iniPos.x, iniPos.y));
        timeTrace.Add(TimeKeeper.time);
    }

    public TouchInfo(TouchInfo other)
    {
        this.touchId = other.touchId;
        this.touchStart = other.touchStart;
        this.initialPos.Set(other.initialPos.x, other.initialPos.y);
        this.trace.AddRange(other.trace);
        this.timeTrace.AddRange(other.timeTrace);
    }

    public int getTouchId() {
        return touchId;
    }

    public double getTouchStart() {
        return touchStart;
    }

    public Vector2 getInitialPos() {
        return initialPos;
    }

    public void setCurrentPos(Vector2 currentPos) {
        if (trace.Count >= TRACE_LENGTH) {
            trace.RemoveAt(0);
            timeTrace.RemoveAt(0);
        }
        trace.Add(new Vector2(currentPos.x, currentPos.y));
        timeTrace.Add(TimeKeeper.time);
        //Debug.Log("TRACE LENGTH: " + trace.Count);
    }

    public Vector2 getCurrentPos() {
        return trace[trace.Count - 1];
    }

    public Vector2 getTracePos(int stepsBack) {
        if (stepsBack >= trace.Count) {
            return trace[0];
        }
        else {
            return trace[trace.Count - 1 - stepsBack];
        }
    }

    public int TraceLength() {
        return trace.Count;
    }

    public float Speed()
    {
        return Vector2.Distance(trace[trace.Count - 1], trace[0]) / (float)(timeTrace[timeTrace.Count - 1] - timeTrace[0]);
    }

    public bool IsAccelerating(float cutoffSpeed) {
        if (trace.Count < 4) return false;
        //Debug.Log("Acceleration Check with Cutoff " + cutoffSpeed);
        float speed = TraceSpeedAt(0);
        //Debug.Log(speed);
        if (speed < cutoffSpeed) return false;
        for (int i = 1; i < trace.Count - 1; ++i) {
            float speed_then = TraceSpeedAt(i);
            //Debug.Log(speed_then);
            if (speed_then >= speed) return false;
            speed = speed_then;
        }
        return true;
    }

    private float TraceSpeedAt(int stepsBack) {
        int i = trace.Count - 1 - stepsBack;
        float delta_pos = Vector2.Distance(trace[i - 1], trace[i]);
        float delta_t = (float)(timeTrace[i] - timeTrace[i - 1]);
        if (delta_t > 0) {
            return delta_pos / delta_t;
        } else {
            return float.MaxValue;
        }
    }

    private int touchId;
    private double touchStart;
    private Vector2 initialPos = new Vector2();
    private List<Vector2> trace = new List<Vector2>();
    private List<double> timeTrace = new List<double>();
    private const int TRACE_LENGTH = 10;
}