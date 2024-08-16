using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineUtils
{
    public static IEnumerator RunUntilAllStop(IEnumerable<IEnumerator> enumerators)
    {
        List<CoroutineRunner> runners = Wrap(enumerators);
        while (runners.Count > 0)
        {
            for (int i = runners.Count - 1; i >=0; --i)
            {
                CoroutineRunner runner = runners[i];
                runner.Update();
                if (!runner.IsRunning())
                {
                    runners.RemoveAt(i);
                }
            }
            yield return null;
        }
    }

    public static IEnumerator RunUntilAnyStop(IEnumerable<IEnumerator> enumerators)
    {
        List<CoroutineRunner> runners = Wrap(enumerators);
        while (true)
        {
            foreach (CoroutineRunner runner in runners)
            {
                runner.Update();
                if (!runner.IsRunning())
                {
                    yield break;
                }
            }
            yield return null;
        }
    }

    private static List<CoroutineRunner> Wrap(IEnumerable<IEnumerator> enumerators)
    {
        List<CoroutineRunner> runners = new List<CoroutineRunner>();
        foreach (IEnumerator coroutine in enumerators)
        {
            CoroutineRunner runner = new CoroutineRunner();
            runner.SetCoroutine(coroutine);
            runners.Add(runner);
        }
        return runners;
    }

    public static IEnumerator AwaitCondition(Func<bool> condition)
    {
        while (!condition())
        {
            yield return null;
        }
    }

    public static IEnumerator AwaitCondition(Func<bool> condition, Action onCondition)
    {
        while (!condition())
        {
            yield return null;
        }
        onCondition();
    }

    public static IEnumerator DoWithDelay(Action action, float time)
    {
        yield return WaitCoroutine(time);
        action();
    }

    public static IEnumerator DoWithDelay(Action action, float time, Func<bool> defuse)
    {
        yield return WaitCoroutine(time);
        if (!defuse()) { action(); }
    }

    // this is an alternative to WaitForSeconds which is supported both by native coroutines and by CoroutineRunner
    public static IEnumerator WaitCoroutine(float waitTime)
    {
        double tEnd = TimeKeeper.time + waitTime;
        while (TimeKeeper.time < tEnd) { yield return null; }
    }
}
