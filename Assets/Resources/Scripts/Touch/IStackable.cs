using System.Collections.Generic;
using UnityEngine;

public interface IStackable
{
    IEnumerator<Vector2> GetLocalProbePoints();
    bool IsSuitableRoot(GameObject newRoot);
}
