using UnityEngine;

public interface ITouchListener {
    void OnTouch(TouchInfo touchInfo);
    void TouchMoved(TouchInfo touchInfo);
    void OnTouchUp(TouchInfo touchInfo);
    GameObject gameObject { get; }
}