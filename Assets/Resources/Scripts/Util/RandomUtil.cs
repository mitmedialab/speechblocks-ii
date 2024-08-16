using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class RandomUtil
{
    private const int APPROX_NORMAL_N_ITER = 4;
    private const float APPROX_NORMAL_FACTOR = 1.73205f; // = Sqrt(12 / APPROX_NORMAL_N_ITER)

    private static Dictionary<string, List<FloatSample>> floatRandTrack = null;
    private static Dictionary<string, List<IntSample>> intRandTrack = null;

    public struct FloatSample
    {
        public float low;
        public float high;
        public float rand;
    }

    public struct IntSample
    {
        public int low;
        public int high;
        public int rand;
    }

    public static void SetupForReplay(Dictionary<string, List<FloatSample>> floatRandTrack, Dictionary<string, List<IntSample>> intRandTrack)
    {
        RandomUtil.floatRandTrack = floatRandTrack;
        foreach (string key in floatRandTrack.Keys) { floatRandTrack[key].Reverse(); }
        RandomUtil.intRandTrack = intRandTrack;
        foreach (string key in intRandTrack.Keys) { intRandTrack[key].Reverse(); }
    }

    public static float Range(string randid, float minRange, float maxRange)
    {
        if (null == floatRandTrack)
        {
            float randval = UnityEngine.Random.Range(minRange, maxRange);
            Logging.LogRandomRangeFloat(id: randid, low: minRange, high: maxRange, rand: randval);
            return randval;
        }
        else
        {
            List<FloatSample> samples = DictUtil.GetOrDefault(floatRandTrack, randid, null);
            if (null == samples || 0 == samples.Count) {
                Debug.Log($"WARNING: NO MORE SAMPLES FOR {randid}");
                return UnityEngine.Random.Range(minRange, maxRange);
            }
            FloatSample sample = samples[samples.Count - 1];
            samples.RemoveAt(samples.Count - 1);
            if (sample.low != minRange || sample.high != maxRange)
            {
                Debug.Log($"WARNING: MISMATCH {randid}; LOW {sample.low} vs {minRange}; HIGH {sample.high} vs {maxRange}");
                return UnityEngine.Random.Range(minRange, maxRange);
            }
            return sample.rand;
        }
    }

    public static int Range(string randid, int minRange, int maxRange)
    {
        if (null == intRandTrack)
        {
            int randval = UnityEngine.Random.Range(minRange, maxRange);
            Logging.LogRandomRangeInt(id: randid, low: minRange, high: maxRange, rand: randval);
            return randval;
        }
        else
        {
            List<IntSample> samples = DictUtil.GetOrDefault(intRandTrack, randid, null);
            if (null == samples || 0 == samples.Count)
            {
                Debug.Log($"WARNING: NO MORE SAMPLES FOR {randid}");
                return UnityEngine.Random.Range(minRange, maxRange);
            }
            IntSample sample = samples[samples.Count - 1];
            samples.RemoveAt(samples.Count - 1);
            if (sample.low != minRange || sample.high != maxRange)
            {
                Debug.Log($"WARNING: MISMATCH {randid}; LOW {sample.low} vs {minRange}; HIGH {sample.high} vs {maxRange}");
                return UnityEngine.Random.Range(minRange, maxRange);
            }
            return sample.rand;
        }
    }

    public static T PickOne<T>(string randid, T[] options)
    {
        return options[Range(randid, 0, options.Length)];
    }

    public static T PickOne<T>(string randid, List<T> options)
    {
        return options[Range(randid, 0, options.Count)];
    }

    public static List<T> Shuffle<T>(string randid, IEnumerable<T> target)
    {
        List<T> buffer = new List<T>(target);
        for (int i = 0; i < buffer.Count - 1; ++i)
        {
            int swap_i = Range(randid, i, buffer.Count);
            T swap = buffer[swap_i];
            buffer[swap_i] = buffer[i];
            buffer[i] = swap;
        }
        return buffer;
    }

    public static T PickOneWeighted<T>(string randid, List<T> choices, List<double> logWeights)
    {
        double logNormalizer = MathUtil.LogSumExp(logWeights);
        double selector = Range(randid, 0.0f, 1.0f);
        for (int i = 1; i < choices.Count; ++i)
        {
            double weight = Math.Exp(logWeights[i] - logNormalizer);
            if (selector < weight) return choices[i];
            selector -= weight;
        }
        return choices[0];
    }

    public static float ApproxNormal(string randid, float sigma, float trim)
    {
        float sample = 0;
        for (int i = 0; i < APPROX_NORMAL_N_ITER; ++i)
        {
            sample += Range(randid, -0.5f, 0.5f);
        }
        sample *= sigma * APPROX_NORMAL_FACTOR;
        if (sample < -trim) { sample = -trim; }
        if (sample > trim) { sample = trim; }
        return sample;
    }

    public static float SampleExponential(string randid, float MeanPeriod)
    {
        return -Mathf.Log(Range(randid, 0.0f, 1.0f)) * MeanPeriod;
    }

    public static Vector2 DeviatePoint(string randid, Vector2 mean, float stdevDist)
    {
        float alpha = Range(randid, 0, Mathf.PI);
        float radius = ApproxNormal(randid, stdevDist, float.MaxValue);
        return new Vector2(mean.x + radius * Mathf.Cos(alpha), mean.y + radius * Mathf.Sin(alpha));
    }

    public static Vector3 DeviateDirection(string randid, Vector3 mean, float deviationStdAngle)
    {
        float deviationAngle = ApproxNormal(randid, deviationStdAngle, 3 * deviationStdAngle);
        float deviationDirAngle = Range(randid, 0, Mathf.PI);
        return DeviateDirection(mean, deviationAngle, deviationDirAngle);
    }

    public static Vector3 DeviateDirection(Vector3 mean, float deviationAngle, float deviationDirAngle)
    {
        Vector3 axisA = Vector3.Cross(mean, Vector3.up);
        if (axisA.magnitude < 1e-6)
        {
            axisA = Vector3.left;
        }
        else
        {
            axisA = axisA.normalized;
        }
        Vector3 axisB = Vector3.Cross(mean, axisA).normalized;
        Vector3 deviationDir = Mathf.Cos(deviationDirAngle) * axisA + 0.5f * Mathf.Sin(deviationDirAngle) * axisB;
        return Mathf.Cos(deviationAngle) * mean + Mathf.Sin(deviationAngle) * deviationDir * mean.magnitude;
    }
}
