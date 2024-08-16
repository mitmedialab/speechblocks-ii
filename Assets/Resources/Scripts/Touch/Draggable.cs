using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

// Checking pixel transparency using this advice: https://answers.unity.com/questions/399267/get-transparency-of-one-pixel.html

public class Draggable : MonoBehaviour, ITouchListener
{
    private static HashSet<Draggable> allDraggables = new HashSet<Draggable>();

    //private int order = 0;
    private static Vector2 ONE = new Vector2(1, 0);
    private TouchManager touchManager;
    private List<int> mySuggestedTouches = new List<int>();
    private List<int> myTouches = new List<int>();
    private Vector2 centerpointOffset;
    private double originalScaleUnbounded = 1;
    private float pinchStartDistance = 0;
    private float originalAngle = 0;
    private float pinchStartAngle = 0;
    private bool dragLock = false;
    private Draggable parentDraggable = null;
    //private GameObject deleteButton = null;
    [SerializeField]
    private bool allowPinch = false;
    [SerializeField]
    private bool allowRotation = true;
    [SerializeField]
    private bool rearrangeOrder = false;

    private IStackable stackableComponent = null;

    private static double SCALE_LOWER_XLIM = 0.4f;
    private static double SCALE_UPPER_XLIM = 5.25f;
    private static double SCALE_LOWER_LIM = 0.6f;
    private static double SCALE_UPPER_LIM = 4.75f;
    private static double SCALE_SMOOTH_POW = 5f;

    private static List<Action> pinchCallbacks = new List<Action>();
    private IDragInArea lastDragArea = null;

    void Start() {
        stackableComponent = GetComponent<IStackable>();
        touchManager = GameObject.FindWithTag("StageObject").GetComponent<TouchManager>();
        allDraggables.Add(this);
    }

    private void OnDestroy()
    {
        allDraggables.Remove(this);
    }

    public void SetAllowPinch(bool allowPinch)
    {
        this.allowPinch = allowPinch;
    }

    public void SetRearrangeOrder(bool rearrangeOrder)
    {
        this.rearrangeOrder = rearrangeOrder;
    }

    public void SetDragParent(bool dragParent)
    {
        if (dragParent)
        {
            Transform parent = transform.parent;
            if (null == parent) return;
            parentDraggable = parent.GetComponent<Draggable>();
            if (null == parentDraggable) return;
            foreach (int touch in myTouches)
            {
                parentDraggable.OnTouch(touchManager.GetTouchInfo(touch));
            }
            myTouches.Clear();
        }
        else
        {
            if (null == parentDraggable) return;
            foreach (int suggestedTouch in mySuggestedTouches)
            {
                OnTouch(touchManager.GetTouchInfo(suggestedTouch));
            }
            parentDraggable = null;
        }
    }

    public void OnTouch(TouchInfo touchInfo)
    {
        if (!mySuggestedTouches.Contains(touchInfo.getTouchId()))
        {
            mySuggestedTouches.Add(touchInfo.getTouchId());
        }
        if (null == parentDraggable)
        {
            if (myTouches.Count < 2)
            {
                if (0 == myTouches.Count)
                {
                    myTouches.Add(touchInfo.getTouchId());
                    centerpointOffset = touchInfo.getCurrentPos() - (Vector2)transform.position;
                    if (null != stackableComponent && Composition.IsOnCanvas(transform)) { Detach(); }
                    if (rearrangeOrder) { PutOnTop(); }
                }
                else if (allowPinch)
                {
                    foreach (Action pinchCallback in pinchCallbacks) {
                        try
                        {
                            pinchCallback();
                        }
                        catch (Exception e)
                        {
                            ExceptionUtil.OnException(e);
                        }
                    }
                    myTouches.Add(touchInfo.getTouchId());
                    Vector2 touchPos = touchInfo.getCurrentPos();
                    TouchInfo otherTouch = touchManager.GetTouchInfo(myTouches[0]);
                    Vector2 otherTouchPos = otherTouch.getCurrentPos();
                    Vector2 touchDelta = touchPos - otherTouchPos;
                    pinchStartDistance = touchDelta.magnitude;
                    pinchStartAngle = Vector2.SignedAngle(ONE, touchDelta);
                    centerpointOffset = 0.5f * (touchPos + otherTouchPos) - (Vector2)transform.position;
                    originalScaleUnbounded = MathUtil.InvSmoothBounds(transform.localScale[1], SCALE_LOWER_XLIM, SCALE_UPPER_XLIM, SCALE_SMOOTH_POW);
                    originalAngle = transform.rotation.eulerAngles.z;
                }
            }
            ProbeForDragArea(touchInfo);
        } else {
            parentDraggable.OnTouch(touchInfo);
        }
    }

    public void TouchMoved(TouchInfo touchInfo)
    {
        if (null == parentDraggable)
        {
            if (dragLock) return;
            if (!myTouches.Contains(touchInfo.getTouchId())) return;
            if (1 == myTouches.Count)
            {
                Vector2 touchPosition = touchInfo.getCurrentPos();
                transform.position = new Vector3(touchPosition.x - centerpointOffset.x,
                                                 touchPosition.y - centerpointOffset.y,
                                                 transform.position.z);
                Logging.LogMovement(gameObject, $"drag:{GetTouchesCode()}");
                if (touchInfo.Speed() < 0.3f) { ProbeDragIn(touchInfo); }
            }
            else
            {
                ProcessPinch();
            }
        }
        else
        {
            parentDraggable.TouchMoved(touchInfo);
        }
    }

    public void OnTouchUp(TouchInfo touchInfo)
    {
        mySuggestedTouches.Remove(touchInfo.getTouchId());
        if (null == parentDraggable)
        {
            myTouches.Remove(touchInfo.getTouchId());
            if (1 == myTouches.Count)
            {
                Vector2 touchPos = touchManager.GetTouchInfo(myTouches[0]).getCurrentPos();
                centerpointOffset = touchPos - (Vector2)transform.position;
                float scale = transform.localScale[1];
                if (scale > SCALE_UPPER_LIM)
                {
                    transform.localScale = new Vector3(Mathf.Sign(transform.localScale[0]) * (float)SCALE_UPPER_LIM, (float)SCALE_UPPER_LIM, 1);
                }
                else if (scale < SCALE_LOWER_LIM)
                {
                    transform.localScale = new Vector3(Mathf.Sign(transform.localScale[0]) * (float)SCALE_LOWER_LIM, (float)SCALE_LOWER_LIM, 1);
                }
            }
            else if (0 == myTouches.Count)
            {
                ProbeDragIn(touchInfo);
                lastDragArea = null;
                if (ProbeDrop(touchInfo)) { return; }
                if (null != stackableComponent && "Default" == ZSorting.GetSortingLayer(gameObject)) { Reattach(); }
            }
        }
        else
        {
            parentDraggable.OnTouchUp(touchInfo);
        }
    }

    public bool IsTouched()
    {
        return myTouches.Count > 0;
    }

    public int NumSuggestedTouches()
    {
        return mySuggestedTouches.Count;
    }

    public Vector2 TouchDelta()
    {
        return centerpointOffset;
    }

    public void LockDrag()
    {
        dragLock = true;
    }

    public void UnlockDrag()
    {
        dragLock = false;
    }

    public Vector2 CurrentTouchPos()
    {
        //if (!IsTouched()) return null;
        return (Vector2)GetDragTarget().position + centerpointOffset;
    }

    public void PutOnTop()
    {
        List<Draggable> children = RetrieveSubtree(sorted: true);
        foreach (Draggable child in children) { allDraggables.Remove(child); }
        string myLayer = ZSorting.GetSortingLayer(gameObject);
        Draggable[] draggableOrder = allDraggables.Where(draggable => null == draggable.parentDraggable && ZSorting.GetSortingLayer(draggable.gameObject) == myLayer)
                                                      .OrderBy(draggable => ZSorting.GetSortingOrder(draggable.gameObject))
                                                      .ToArray();
        int top = 0;
        IEnumerable<Draggable> newOrder = draggableOrder.Concat(children);
        foreach (Draggable draggable in newOrder)
        {
            int current = top++;
            ZSorting.SetSortingOrder(draggable.gameObject, current);
            Logging.LogSortOrder(draggable.gameObject, current, $"drag-rearrange:{GetTouchesCode()}");
        }
        foreach (Draggable child in children) { allDraggables.Add(child); }
    }

    public static void AddPinchCallback(Action pinchCallback)
    {
        pinchCallbacks.Add(pinchCallback);
    }

    private void ProbeForDragArea(TouchInfo touchInfo)
    {
        Vector2 fingerPosition = touchInfo.getCurrentPos();
        RaycastHit2D[] raycastHits = Physics2D.RaycastAll(fingerPosition, Camera.main.transform.forward);
        foreach (RaycastHit2D raycastHit in raycastHits)
        {
            IDragInArea dragInArea = raycastHit.collider.gameObject.GetComponent<IDragInArea>();
            if (null != dragInArea)
            {
                lastDragArea = dragInArea;
                return;
            }
        }
    }

    private void ProbeDragIn(TouchInfo touchInfo)
    {
        Vector2 fingerPosition = touchInfo.getCurrentPos();
        RaycastHit2D[] raycastHits = Physics2D.RaycastAll(fingerPosition, Camera.main.transform.forward);
        foreach (RaycastHit2D raycastHit in raycastHits)
        {
            IDragInArea dragInArea = raycastHit.collider.gameObject.GetComponent<IDragInArea>();
            if (null != dragInArea)
            {
                try
                {
                    if (lastDragArea != dragInArea && dragInArea.AcceptDragIn(gameObject))
                    {
                        lastDragArea = dragInArea;
                        Logging.LogDragIn(touchInfo.getTouchId(), gameObject, raycastHit.collider.gameObject);
                    }
                }
                catch (Exception e)
                {
                    ExceptionUtil.OnException(e);
                }
                return;
            }
        }
        lastDragArea = null;
        return;
    }

    private bool ProbeDrop(TouchInfo touchInfo)
    {
        Vector2 fingerUpPosition = touchInfo.getCurrentPos();
        RaycastHit2D[] raycastHits = Physics2D.RaycastAll(fingerUpPosition, Camera.main.transform.forward);
        foreach (RaycastHit2D raycastHit in raycastHits)
        {
            IDropArea dropArea = raycastHit.collider.gameObject.GetComponent<IDropArea>();
            try
            {
                if (null != dropArea && dropArea.AcceptDrop(gameObject))
                {
                    Logging.LogDrop(touchInfo.getTouchId(), gameObject, raycastHit.collider.gameObject);
                    return true;
                }
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
        }
        return false;
    }

    private Transform GetDragTarget()
    {
        if (null != parentDraggable)
        {
            return parentDraggable.transform;
        }
        else
        {
            return gameObject.transform;
        }
    }

    private void ProcessPinch()
    {
        Vector2 touch0Pos = touchManager.GetTouchInfo(myTouches[0]).getCurrentPos();
        Vector2 touch1Pos = touchManager.GetTouchInfo(myTouches[1]).getCurrentPos();
        Vector2 touchDelta = touch1Pos - touch0Pos;
        float deltaScale = touchDelta.magnitude / pinchStartDistance;
        float deltaAngle = Vector2.SignedAngle(ONE, touchDelta) - pinchStartAngle;
        float offsetMag = deltaScale * centerpointOffset.magnitude;
        float offsetAng = Vector2.SignedAngle(ONE, centerpointOffset) + deltaAngle;
        Vector2 touchMidpoint = 0.5f * (touch1Pos + touch0Pos);
        float newX = touchMidpoint.x - offsetMag * Mathf.Cos(Mathf.PI * offsetAng / 180);
        float newY = touchMidpoint.y - offsetMag * Mathf.Sin(Mathf.PI * offsetAng / 180);
        transform.position = new Vector3(newX, newY, transform.position.z);
        Logging.LogMovement(gameObject, $"drag:{GetTouchesCode()}");
        double scale = originalScaleUnbounded * (touchDelta.magnitude / pinchStartDistance);
        scale = MathUtil.SmoothBounds(scale, SCALE_LOWER_XLIM, SCALE_UPPER_XLIM, SCALE_SMOOTH_POW);
        float scaleF = (float)scale;
        transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x) * scaleF, scaleF, 1);
        Logging.LogScale(gameObject, $"drag:{GetTouchesCode()}");
        if (allowRotation)
        {
            transform.rotation = Quaternion.Euler(0, 0, originalAngle + deltaAngle);
            Logging.LogRotation(gameObject, $"drag:{GetTouchesCode()}");
        }
    }

    private void Detach()
    {
        transform.SetParent(GameObject.FindWithTag("CompositionRoot").transform, true);
    }

    private void Reattach() {
        Draggable newRoot = FetchNewRoot();
        if (null != newRoot)
        {
            transform.SetParent(newRoot.transform, true);
            Logging.LogParent(gameObject, $"draggable-reattach:{GetTouchesCode()}");
            return;
        }
    }

    private Draggable FetchNewRoot() {
        List<Draggable> mySubtree = RetrieveSubtree(sorted: false);
        try
        {
            IEnumerator<Vector2> probePoints = stackableComponent.GetLocalProbePoints();
            while (probePoints.MoveNext())
            {
                Vector3 worldProbePoint = transform.localToWorldMatrix.MultiplyPoint(probePoints.Current);
                GameObject candidateRoot = ProbeHelper.ProbeForOverlap(gameObject, worldProbePoint, obj => CanBeNewRoot(obj, mySubtree));
                if (null != candidateRoot) return candidateRoot.GetComponent<Draggable>();
            }
        }
        catch (Exception e)
        {
            ExceptionUtil.OnException(e);
        }
        return null;
    }

    private bool CanBeNewRoot(GameObject candidate, List<Draggable> detachedSubtree)
    {
        if (null == candidate.GetComponent<IStackable>()) return false;
        Draggable draggable = candidate.GetComponent<Draggable>();
        if (null == draggable) return false;
        if (detachedSubtree.Contains(draggable)) return false;
        try
        {
            return stackableComponent.IsSuitableRoot(candidate);
        }
        catch (Exception e)
        {
            ExceptionUtil.OnException(e);
            return false;
        }
    }

    private List<Draggable> RetrieveSubtree(bool sorted) {
        List<Draggable> children = new List<Draggable>();
        _RetrieveSubtree(gameObject, children);
        if (sorted) { children.Sort((drgA, drgB) => ZSorting.GetSortingOrder(drgA.gameObject) - ZSorting.GetSortingOrder(drgB.gameObject)); }
        return children;
    }

    private string GetTouchesCode()
    {
        return string.Join("+", myTouches.Select(touchID => Logging.GetTouchID(touchID)));
    }

    private static void _RetrieveSubtree(GameObject target, List<Draggable> children) {
        Draggable draggable = target.GetComponent<Draggable>();
        if (null != draggable && null == draggable.parentDraggable) { 
            children.Add(draggable); 
        }
        foreach (Transform child in target.transform) {
            _RetrieveSubtree(child.gameObject, children);
        }
    }
}
