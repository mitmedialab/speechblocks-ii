using UnityEngine;

public class WorldUtils {
    public static float GetWorldHeight() {
        return 2 * Camera.main.orthographicSize;
    }

    public static float GetWorldWidth()
    {
        return 2 * Camera.main.orthographicSize * Camera.main.aspect;
    }
}