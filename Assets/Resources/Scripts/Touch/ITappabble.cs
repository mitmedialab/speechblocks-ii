using UnityEngine;

public interface ITappable {
    void OnTap(TouchInfo touchInfo);
    GameObject gameObject { get; }
}