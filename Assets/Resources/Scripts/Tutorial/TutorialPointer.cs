using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialPointer : MonoBehaviour
{
    private const int POINT_CYCLES = 5;
    private const float POINT_AMPLITUDE = 0.25f;
    private const float POINTING_TIME = 4f;
    private static List<TutorialPointer> allPointers = new List<TutorialPointer>();
    private bool visible = true;

    public static void DestroyAllPointers()
    {
        foreach (TutorialPointer pointer in allPointers)
        {
            if (null != pointer && null != pointer.gameObject)
            {
                Destroy(pointer.gameObject);
            }
        }
        allPointers.Clear();
    }

    public void PointAt(GameObject target, Vector2 axis, string cause)
    {
        Logging.LogPointAt(target, cause);
        if (axis == Vector2.zero)
        {
            axis = target.transform.position;
            if (axis.magnitude < 0.01) { axis = Vector3.up; }
        }
        StartCoroutine(PointingCoroutine(target, axis, pointAt: true));
    }

    public void PointAway(GameObject target, Vector2 axis, float offset, string cause)
    {
        Logging.LogPointAway(target, cause);
        StartCoroutine(PointingCoroutine(target, axis, pointAt: false, offset: offset));
    }

    public GameObject GetTarget()
    {
        return target;
    }

    private IEnumerator PointingCoroutine(GameObject target, Vector2 axis, bool pointAt, float offset = 0)
    {
        allPointers.Add(this);
        StageOrchestrator stageOrchestrator = GameObject.FindWithTag("StageObject").GetComponent<StageOrchestrator>();
        string targetStage = stageOrchestrator.GetStageOfObject(target);
        try
        {
            AttachToObject(target, axis);
            this.target = target;
        }
        catch
        {
            Destroy(gameObject);
            yield break;
        }
        Transform arrowHolderTransform = transform.Find("arrow_holder");
        double startTime = TimeKeeper.time;
        while (true)
        {
            ControlVisibility(stageOrchestrator, targetStage);
            AdjustScale();
            float t = (float)(TimeKeeper.time - startTime) / POINTING_TIME;
            if (t > 1) { t = 1; }
            float scale = ScaleLaw(t);
            arrowHolderTransform.localScale = new Vector3(scale, scale, 1);
            float x = pointAt ? PointingAtLaw(t) : PointingAwayLaw(t, offset);
            arrowHolderTransform.localPosition = new Vector3(x, 0, 0);
            yield return null;
            if (1 == t) break;
        }
        this.target = null;
        Destroy(gameObject);
    }

    private void AttachToObject(GameObject target, Vector2 axis)
    {
        transform.SetParent(target.transform);
        transform.localPosition = Vector3.zero;
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(axis.y, axis.x));
        AdjustScale();
    }

    private void AdjustScale()
    {
        Vector3 parentScaleVec = transform.parent.lossyScale;
        float parentScale = Mathf.Max(Mathf.Abs(parentScaleVec.x), Mathf.Abs(parentScaleVec.y), 0.01f);
        float targetScale = 1.0f / parentScale;
        transform.localScale = new Vector3(targetScale, targetScale, 1);
    }

    private float ScaleLaw(float t)
    {
        return Mathf.Pow(t, 0.1f) * Mathf.Pow(1 - t, 0.1f);
    }

    private float PointingAtLaw(float t)
    {
        return -POINT_AMPLITUDE * (1 + Mathf.Cos(2 * Mathf.PI * (POINT_CYCLES - 0.5f) * t));
    }

    private float PointingAwayLaw(float t, float offset)
    {
        return POINT_AMPLITUDE * (offset + 5f + Mathf.Sin(2 * Mathf.PI * (POINT_CYCLES - 0.5f) * t));
    }

    private void ControlVisibility(StageOrchestrator stageOrchestrator, string target_stage)
    {
        if (target_stage == stageOrchestrator.GetStage())
        {
            if (!visible)
            {
                visible = true;
                Opacity.SetOpacity(gameObject, 1);
            }
        }
        else
        {
            if (visible)
            {
                visible = false;
                Opacity.SetOpacity(gameObject, 0);
            }
        }
    }

    private GameObject target = null;
}
