// Easing function ideas from https://www.febucci.com/2018/08/easing-functions/

using UnityEngine;

public class Easing {
    public static float EaseIn(float t)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        return t * t;
    }

    public static float EaseOut(float t)
    {
        return 1 - EaseIn(1 - t);
    }

    public static float EaseInOut(float t)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        return Mathf.Lerp(EaseIn(t), EaseOut(t), t);
    }

    public static float EaseInSteadyOut(float t, float steadyW)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        if (steadyW > 1) steadyW = 1;
        float cornerW = 0.5f * (1 - steadyW);
        float cornerH = 0.5f * cornerW / (1 - cornerW);
        if (t < cornerW)
        {
            return cornerH * t * t;
        }
        else if (t > 1 - cornerW)
        {
            float d = 1 - t;
            return 1 - cornerH * d * d;
        }
        else
        {
            return Mathf.Lerp(cornerH, 1 - cornerH, (t - cornerW) / steadyW);
        }
    }

    public static float EaseHop(float t)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 0;
        float d = 0.5f - t;
        return 1 - 4 * d * d;
    }

    public static float EaseThereAndBackAgain(float t)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 0;
        if (t < 0.5) return EaseInOut(2 * t);
        return 1 - EaseInOut(2 * (t - 0.5f));
    }
}