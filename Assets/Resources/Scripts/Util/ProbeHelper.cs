using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ProbeHelper
{
	public static Vector2 GetProbePoint(Vector2 center, float probeRadius, int i)
    {
		double r = INV_SQRT_RAYS_TO_CAST * probeRadius * Mathf.Sqrt(i);
		double x = center.x + r * Mathf.Cos((float)(i * PHYLLOTAXIS_ANGLE));
		double y = center.y + r * Mathf.Sin((float)(i * PHYLLOTAXIS_ANGLE));
		return new Vector2((float)x, (float)y);
	}

	public static GameObject Probe(Vector2 probePoint, Func<GameObject, bool> isRelevant)
    {
		RaycastHit2D[] hits = Physics2D.RaycastAll(probePoint, Camera.main.transform.forward);
		IEnumerable<GameObject> hitObjects = Physics2D.RaycastAll(probePoint, Camera.main.transform.forward)
															.Select(hit => hit.collider.gameObject)
															.Where(obj => IsIndeedTouched(obj, probePoint));
		return PickTop(hitObjects, isRelevant);
	}

	public static GameObject ProbeForOverlap(GameObject sourceObject, Vector2 probePoint, Func<GameObject, bool> isRelevant)
    {
		List<GameObject> hitObjects = Physics2D.RaycastAll(probePoint, Camera.main.transform.forward)
															.Select(hit => hit.collider.gameObject)
															.Where(obj => IsIndeedTouched(obj, probePoint))
															.ToList();
		if (!hitObjects.Contains(sourceObject)) return null;
		hitObjects.Remove(sourceObject);
		return PickTop(hitObjects, isRelevant);
	}

	private static GameObject PickTop(IEnumerable<GameObject> candidates, Func<GameObject, bool> isRelevant)
    {
		List<GameObject> candidatesList = candidates.ToList();
		if (0 == candidatesList.Count) return null;
		candidatesList.Sort(ZSorting.SortingCompare);
		GameObject top = candidatesList[candidatesList.Count - 1];
		if (!isRelevant(top)) return null;
		return top;
	}

	private static bool IsIndeedTouched(GameObject touchCandidate, Vector2 probePoint)
	{
		ActiveTouchArea activeTouchArea = touchCandidate.GetComponent<ActiveTouchArea>();
		if (null == activeTouchArea) return true;
		return activeTouchArea.PointIsWithinActiveZone(probePoint);
	}

	public const int RAYS_TO_CAST = 100;

	private static float INV_SQRT_RAYS_TO_CAST = 0.1f;

	private const double PHYLLOTAXIS_ANGLE = (Mathf.PI * 137.508 / 180);
}
