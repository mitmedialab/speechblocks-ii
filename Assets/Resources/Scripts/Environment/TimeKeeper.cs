using UnityEngine;

// This time is being used instead of Unity's standard TimeKeeper.time to allow for
// setting exact time during replay
public class TimeKeeper
{
    public static double time { get; private set; }

    public static void UpdateGameTime()
    {
        time += Time.deltaTime;
    }

    public static void InitSimulatedTime(double initialTimestamp)
    {
        time = initialTimestamp;
    }

    public static bool UpdateSimulatedTime(double nextTimestamp)
    {
        if (time + 1.5 * Time.deltaTime < nextTimestamp)
        {
            time += Time.deltaTime;
            return false;
        }
        else
        {
            time = nextTimestamp;
            return true;
        }
    }
}
