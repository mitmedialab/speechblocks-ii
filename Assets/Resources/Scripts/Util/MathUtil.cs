using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MathUtil
{
    public static double LogSumExp(IEnumerable<double> xs)
    {
        double m = xs.Max();
        return m + Math.Log(xs.Select(x => Math.Exp(x - m)).Sum());
    }

    public static double SmoothBounds(double x, double min, double max, double pow)
    {
        double mid      = (min + max) / 2;
        double halfspan = (max - min) / 2;
        double val      = (x - mid) / halfspan;
        double resp     = val / Math.Pow(1 + Math.Pow(Math.Abs(val), pow), 1 / pow);
        double y        = mid + resp * halfspan;
        return y;
    }

    public static double InvSmoothBounds(double y, double min, double max, double pow)
    {
        if (y < min)      return -1000000000f;
        else if (y > max) return  1000000000f;
        double mid      = (min + max) / 2;
        double halfspan = (max - min) / 2;
        double val      = (y - mid) / halfspan;
        double resp     = val / Math.Pow(1 - Math.Pow(Math.Abs(val), pow), 1 / pow);
        double x        = mid + resp * halfspan;
        return x;
    }
}
