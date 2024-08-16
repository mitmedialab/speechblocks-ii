using System;
using UnityEngine;

public class ExceptionUtil
{
    public static void OnException(Exception e)
    {
        Debug.LogException(e);
        Logging.LogError(e.Message, e.StackTrace);
    }
}
