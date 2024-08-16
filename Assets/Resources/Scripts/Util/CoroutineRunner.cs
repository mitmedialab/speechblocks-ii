using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class re-implements some basic coroutine functions in Unity.
// It is written to go around some weird bug with stopping coroutines.
public class CoroutineRunner
{
    private List<IEnumerator> enumeratorStack = new List<IEnumerator>();
    private Type iEnumeratorType = typeof(IEnumerator);
    private Type asyncOperationType = typeof(AsyncOperation);
    private AsyncOperation asyncOperation = null;
    private int stackNumber = 0;

    public CoroutineRunner() { }

    public CoroutineRunner(IEnumerator coroutine)
    {
        SetCoroutine(coroutine);
    }

    public void SetCoroutine(IEnumerator coroutine)
    {
        Stop();
        ++stackNumber;
        if (null != coroutine)
        {
            enumeratorStack.Add(coroutine);
            Update();
        }
    }

    public void Update()
    {
        if (null != asyncOperation)
        {
            if (!asyncOperation.isDone) return;
            asyncOperation = null;
        }
        try
        {
            while (enumeratorStack.Count > 0)
            {
                IEnumerator topCoroutine = enumeratorStack[enumeratorStack.Count - 1];
                int stackNumberMem = stackNumber;
                if (topCoroutine.MoveNext())
                {
                    if (stackNumberMem != stackNumber) return;
                    object current = topCoroutine.Current;
                    if (iEnumeratorType.IsInstanceOfType(current))
                    {
                        enumeratorStack.Add((IEnumerator)current);
                        continue;
                    }
                    else if (asyncOperationType.IsInstanceOfType(current))
                    {
                        asyncOperation = (AsyncOperation)current;
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
                else if (stackNumberMem == stackNumber)
                {
                    enumeratorStack.RemoveAt(enumeratorStack.Count - 1);
                }
            }
        }
        catch (Exception e)
        {
            ExceptionUtil.OnException(e);
            Stop();
        }
    }

    public void Stop()
    {
        asyncOperation = null;
        enumeratorStack.Clear();
    }

    public bool IsRunning()
    {
        return enumeratorStack.Count > 0;
    }
}
