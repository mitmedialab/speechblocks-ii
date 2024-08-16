using System;
using UnityEngine;
using UnityEngine.Events;

public class Handle : MonoBehaviour, ITouchListener, ITappable
{
    public const int IS_RETRACTED = 0;
    public const int IS_IN_BETWEEN = 1;
    public const int IS_DEPLOYED = 2;
    public const int IS_OUTSIDE = 3;

    public const float GLIDE_TIME = 0.25f;

    [SerializeField]
    private GameObject targetObject = null;
    [SerializeField]
    private GameObject referent = null;
    [SerializeField]
    private UnityEvent onTouch = null;
    [SerializeField]
    private UnityEvent onDeploy = null;
    [SerializeField]
    private UnityEvent onRetract = null;
    private int state = IS_RETRACTED;
    private GameObject movementTarget = null;
    private GameObject min = null;
    private GameObject max = null;
    private Vector3 movementDirection;
    private float touchAdvanceDeltaTarg = 0;
    private float touchAdvanceDeltaMin = 0;
    private float touchAdvanceDeltaMax = 0;
    private AnimationMaster animaster = null;
    private int touchCount = 0;
    private int touchID = 0;
    private bool isTap = false;
    private bool setup = false;
    private bool triggered = false;

    public void Setup(GameObject targetObject, GameObject referent, UnityEvent onTouch, UnityEvent onDeploy, UnityEvent onRetract) {
        if (setup) return;
        setup = true;
        this.targetObject = targetObject;
        this.referent = referent;
        this.onTouch = onTouch;
        this.onDeploy = onDeploy;
        this.onRetract = onRetract;
        min = transform.Find("min").gameObject;
        max = transform.Find("max").gameObject;
        movementDirection = min.transform.position - max.transform.position;
        movementDirection /= movementDirection.magnitude;
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        animaster = stageObject.GetComponent<AnimationMaster>();
        movementTarget = new GameObject();
        movementTarget.name = "handle_target";
        movementTarget.transform.SetParent(targetObject.transform.parent, false);
        movementTarget.transform.position = targetObject.transform.position;
        state = DetermineState();
    }

    private void Start()
    {
        if (!setup && null != targetObject) {
            Setup(targetObject, referent, onTouch, onDeploy, onRetract);
        }
    }

    private void Update()
    {
        if (null == referent) {
            Debug.Log("INVALID STATE!");
            return;
        }
        int newState = DetermineState();
        if (triggered)
        {
            if (newState == IS_DEPLOYED && state != IS_DEPLOYED && state != IS_OUTSIDE)
            {
                if (null != onDeploy) {
                    try
                    {
                        onDeploy.Invoke();
                    }
                    catch (Exception e)
                    {
                        ExceptionUtil.OnException(e);
                    }
                }
                if (0 == touchCount) { triggered = false; }
            }
            else if (newState == IS_RETRACTED && state != IS_RETRACTED && state != IS_OUTSIDE)
            {
                if (null != onRetract) {
                    try
                    {
                        onRetract.Invoke();
                    }
                    catch (Exception e)
                    {
                        ExceptionUtil.OnException(e);
                    }
                }
                if (0 == touchCount) { triggered = false; }
            }
        }
        state = newState;
    }

    public void OnTouch(TouchInfo touchInfo) {
        ++touchCount;
        LocalGlide glide = GetComponent<LocalGlide>();
        if (null != glide) { Destroy(glide); }
        if (1 == touchCount) {
            if (null != onTouch) {
                try
                {
                    onTouch.Invoke();
                }
                catch (Exception e)
                {
                    ExceptionUtil.OnException(e);
                }
            }
            triggered = true;
            touchID = touchInfo.getTouchId();
            float touchInitialAdvance = GetAdvance(touchInfo.getCurrentPos());
            float targInitialAdvance = GetAdvance(targetObject);
            float minInitialAdvance = GetAdvance(min);
            float maxInitialAdvance = GetAdvance(max);
            touchAdvanceDeltaTarg = targInitialAdvance - touchInitialAdvance;
            touchAdvanceDeltaMin = minInitialAdvance - touchInitialAdvance;
            touchAdvanceDeltaMax = maxInitialAdvance - touchInitialAdvance;
        }
    }

    public void TouchMoved(TouchInfo touchInfo) {
        if (touchID != touchInfo.getTouchId()) return;
        float touchAdvance = GetAdvance(touchInfo.getCurrentPos());
        float targetAdvance = touchAdvance + touchAdvanceDeltaTarg;
        float minObjAdvance = touchAdvance + touchAdvanceDeltaMin;
        float maxObjAdvance = touchAdvance + touchAdvanceDeltaMax;
        float refAdvance = GetAdvance(referent);
        if (maxObjAdvance > refAdvance) {
            targetAdvance += refAdvance - maxObjAdvance;
        } else if (minObjAdvance < refAdvance) {
            targetAdvance += refAdvance - minObjAdvance;
        }
        SetAdvance(targetObject, targetAdvance);
    }

    public void OnTouchUp(TouchInfo touchInfo) {
        --touchCount;
        if (isTap) {
            isTap = false;
            return;
        }
        if (0 == touchCount) {
            float touchAdvanceDelta = GetAdvance(touchInfo.getCurrentPos()) - GetAdvance(touchInfo.getTracePos(2));
            if (touchAdvanceDelta > 0) {
                Deploy();
            } else {
                Retract();
            }
        }
    }

    public void OnTap(TouchInfo touchInfo) {
        isTap = true;
        if (IsDeployed()) {
            Retract();
        } else if (IsRetracted()) {
            Deploy();
        }
    }

    public void Deploy() {
        float shift = GetAdvance(referent) - GetAdvance(max);
        float advance = GetAdvance(targetObject) + shift;
        SetAdvance(movementTarget, advance);
        animaster.StartLocalGlide(targetObject, movementTarget.transform.localPosition, GLIDE_TIME);
    }

    public void Retract() {
        float shift = GetAdvance(referent) - GetAdvance(min);
        float advance = GetAdvance(targetObject) + shift;
        SetAdvance(movementTarget, advance);
        animaster.StartLocalGlide(targetObject, movementTarget.transform.localPosition, GLIDE_TIME);
    }

    public bool IsDeployed()
    {
        return IS_DEPLOYED == DetermineState();
    }

    public bool IsRetracted()
    {
        return IS_RETRACTED == DetermineState();
    }

    private float GetAdvance(Vector3 point) {
        return Vector3.Dot(movementDirection, point);
    }

    private float GetAdvance(GameObject gameObject) {
        return Vector3.Dot(movementDirection, gameObject.transform.position);
    }

    private void SetAdvance(GameObject gameObject, float advance) {
        float currentAdvance = GetAdvance(gameObject);
        gameObject.transform.position += movementDirection * (advance - currentAdvance);
    }

    private int DetermineState() {
        float referentAdvance = GetAdvance(referent);
        float minAdvance = GetAdvance(min);
        float maxAdvance = GetAdvance(max);
        if (Mathf.Abs(minAdvance - referentAdvance) < 0.05f) {
            return IS_RETRACTED;
        }
        if (Mathf.Abs(maxAdvance - referentAdvance) < 0.05f) {
            return IS_DEPLOYED;
        }
        if (referentAdvance < maxAdvance || referentAdvance > minAdvance) {
            return IS_OUTSIDE;
        }
        return IS_IN_BETWEEN;
    }
}
