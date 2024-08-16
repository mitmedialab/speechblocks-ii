using UnityEngine;

public class BoundsUtil {
    public static bool Contains2D(Bounds bounds, Vector2 vec) {
        return vec.x >= bounds.min.x && vec.x <= bounds.max.x && vec.y >= bounds.min.y && vec.y <= bounds.max.y;
    }

    public static bool IntervalsOverlap(int lo1, int hi1, int lo2, int hi2)
    {
        return !(hi1 <= lo2 || hi2 <= lo1);
    }
}