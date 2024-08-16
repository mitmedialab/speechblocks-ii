using System;
using UnityEngine;
using UnityEngine.Events;

public class GenericTappable : MonoBehaviour, ITappable, ITouchListener {
    [SerializeField]
    private UnityEvent myEvent = new UnityEvent();
    private int touchCount;

    public void OnTap(TouchInfo touchInfo) {
        try
        {
            myEvent.Invoke();
        }
        catch (Exception e)
        {
            ExceptionUtil.OnException(e);
        }
    }

    public void OnTouch(TouchInfo touchInfo) {
        ++touchCount;
    }

    public void TouchMoved(TouchInfo touchInfo) {}

    public void OnTouchUp(TouchInfo touchInfo) {
        --touchCount;
    }

    public bool IsTouched() {
        return 0 < touchCount;
    }

    public void AddAction(Action action)
    {
        myEvent.AddListener(new UnityAction(action));
    }
}
