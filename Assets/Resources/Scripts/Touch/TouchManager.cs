using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public class TouchManager : MonoBehaviour {
    private StageOrchestrator stageOrchestrator;
    private Dictionary<int, ITappable[]> awaitingTap = new Dictionary<int, ITappable[]>();
    private Dictionary<int, TouchInfo> touchInfos = new Dictionary<int, TouchInfo>();
    private Dictionary<int, ITouchListener[]> touchListeners = new Dictionary<int, ITouchListener[]>();
    private List<Action> onAnyTouch = new List<Action>();
    //private Dictionary<int, ILo>
    private bool mouseDown = false;
    private bool fakeTouchOn = false;
    private bool rightMouseDown = false;
    private bool isUnconstrained = false;

    private AudioSource audioSource = null;
    private AudioClip click = null;
    private AudioClip reject = null;

    private float TAP_OFFSET = 0;
    private const float PROBE_RADUIS = 0.6f;
    private const float MIN_TAP_INTERVAL = 0.4f;

    private List<ITappable> allowedToTapDelayed = new List<ITappable>();
    private List<ITappable> allowedToTap = new List<ITappable>();
    private List<ITouchListener> allowedToTouch = new List<ITouchListener>();
    private List<Action> actionsAwaitingNextTap = new List<Action>();
    private ITappable delayedTapped = null;
    private TouchInfo delayedTouchInfo = null;
    private double delayedTapTime = 0;

    private bool isReplayMode = false;
    private List<int> replayTouches = new List<int>();
    private Dictionary<int, TouchPhase> replayTouchPhases = new Dictionary<int, TouchPhase>();
    private Dictionary<int, Vector2> replayTouchPositions = new Dictionary<int, Vector2>();

    private Dictionary<GameObject, List<int>> objectsRespondingToTouches = new Dictionary<GameObject, List<int>>();

    public const float INFLATION_ON_TAP = 1.1f;

    private double lastTapTime = -1000;

	// Use this for initialization
	void Start () {
        TAP_OFFSET = 0.15f * Block.GetStandardHeight();
        audioSource = GetComponent<AudioSource>();
        stageOrchestrator = GetComponent<StageOrchestrator>();
        click = Resources.Load<AudioClip>("Sounds/click");
        reject = Resources.Load<AudioClip>("Sounds/reject");
	}

    // Update is called once per frame
    void Update()
    {
        if (isReplayMode) {
            UpdateReplayTouches();
        } if (Input.touchSupported) {
            UpdateFingerTouches();
        } else {
            UpdateMouseTouches();
        }
        CheckOnAwaiting();
    }

    private void UpdateFingerTouches()
    {
        Touch[] myTouches = Input.touches;
        foreach (Touch touch in myTouches)
        {
            if (TouchPhase.Began == touch.phase)
            {
                OnTouchDown(touch.fingerId, touch.position);
            }
            else if (TouchPhase.Moved == touch.phase)
            {
                OnTouchMove(touch.fingerId, touch.position);
            }
            else if (TouchPhase.Ended == touch.phase || TouchPhase.Canceled == touch.phase)
            {
                OnTouchUp(touch.fingerId, forced: false);
            }
        }
    }

    private void UpdateMouseTouches()
    {
        if (!mouseDown)
        {
            if (Input.GetMouseButton(0))
            {
                mouseDown = true;
                OnTouchDown(0, Input.mousePosition);
            }
        }
        else
        {
            if (!Input.GetMouseButton(0))
            {
                mouseDown = false;
                OnTouchUp(0, forced: false);
            }
            else
            {
                OnTouchMove(0, Input.mousePosition);
            }
        }
        if (!rightMouseDown)
        {
            if (Input.GetMouseButton(1))
            {
                rightMouseDown = true;
                if (!fakeTouchOn)
                {
                    OnTouchDown(1, Input.mousePosition);
                    fakeTouchOn = true;
                }
                else
                {
                    OnTouchUp(1, forced: false);
                    fakeTouchOn = false;
                }
            }
        }
        else
        {
            if (!Input.GetMouseButton(1))
            {
                rightMouseDown = false;
            }
            else if (fakeTouchOn)
            {
                OnTouchMove(1, Input.mousePosition);
            }
        }
    }

    private void UpdateReplayTouches()
    {
        foreach (int touchID in replayTouches)
        {
            TouchPhase touchPhase = replayTouchPhases[touchID];
            if (TouchPhase.Began == touchPhase)
            {
                OnTouchDown(touchID, replayTouchPositions[touchID]);
                replayTouchPhases[touchID] = TouchPhase.Stationary;
            }
            else if (TouchPhase.Moved == touchPhase)
            {
                OnTouchMove(touchID, replayTouchPositions[touchID]);
                replayTouchPhases[touchID] = TouchPhase.Stationary;
            }
            else if (TouchPhase.Ended == touchPhase)
            {
                OnTouchUp(touchID, forced: false);
                replayTouchPhases.Remove(touchID);
                replayTouchPositions.Remove(touchID);
            }
        }
        replayTouches.RemoveAll(touchID => !replayTouchPhases.ContainsKey(touchID));
    }

    public void SetReplayMode(bool replayMode)
    {
        this.isReplayMode = replayMode;
    }

    public void ReplayTouchDown(int touchID, float touchX, float touchY)
    {
        int insertionIndex = replayTouches.BinarySearch(touchID);
        if (insertionIndex < 0) { insertionIndex = ~insertionIndex; }
        replayTouches.Insert(insertionIndex, touchID);
        replayTouchPhases[touchID] = TouchPhase.Began;
        replayTouchPositions[touchID] = new Vector2(touchX, touchY);
    }

    public void ReplayTouchMove(int touchID, float touchX, float touchY)
    {
        replayTouchPhases[touchID] = TouchPhase.Moved;
        replayTouchPositions[touchID] = new Vector2(touchX, touchY);
    }

    public void ReplayTouchUp(int touchID)
    {
        replayTouchPhases[touchID] = TouchPhase.Ended;
    }

    public int GetTouchCount()
    {
        return touchInfos.Count;
    }

    public TouchInfo GetTouchInfo(int touchId) {
        if (touchInfos.ContainsKey(touchId)) {
            return touchInfos[touchId];
        } else {
            return null;
        }
    }

    public void Constrain()
    {
        Debug.Log("CONSTRAIN TOUCH MANAGER");
        isUnconstrained = false;
        ResetConstraints();
        List<int> touches = new List<int>(touchInfos.Keys);
        foreach (int touchId in touches)
        {
            OnTouchUp(touchId, forced: true);
        }
    }

    public void ResetConstraints()
    {
        if (isUnconstrained) return;
        allowedToTapDelayed.Clear();
        allowedToTap.Clear();
        allowedToTouch.Clear();
        delayedTapped = null;
    }

    public void Unconstrain() {
        Debug.Log("UNCONSTRAIN TOUCH MANAGER");
        isUnconstrained = true;
        if (null != delayedTapped)
        {
            Tap(delayedTapped, delayedTouchInfo, isDelayed: true);
            delayedTapped = null;
            delayedTouchInfo = null;
        }
    }

    public void AddAllowedToTapDelayed(ITappable allowedToTapDelayed)
    {
        this.allowedToTapDelayed.Add(allowedToTapDelayed);
    }

    public void AddAllowedToTapDelayed<T>(IEnumerable<T> allowedToTapDelayed) where T : ITappable
    {
        foreach (T element in allowedToTapDelayed) { this.allowedToTapDelayed.Add(element); }
    }

    public void AddAllowedToTap(ITappable allowedToTap)
    {
        this.allowedToTap.Add(allowedToTap);
        if (delayedTapped == allowedToTap)
        {
            Tap(delayedTapped, delayedTouchInfo, isDelayed: true);
        }
    }

    public void AddAllowedToTap<T>(IEnumerable<T> allowedToTap) where T : ITappable
    {
        foreach (T element in allowedToTap) { this.allowedToTap.Add(element); }
    }

    public void AddAllowedToTap(string tag)
    {
        AddAllowedToTap(GameObject.FindGameObjectsWithTag(tag).Select(obj => obj.GetComponent<ITappable>()).ToList());
    }

    public void AddAllowedToTouch(ITouchListener allowedToTouch)
    {
        this.allowedToTouch.Add(allowedToTouch);
    }

    public void AddAllowedToTouch<T>(IEnumerable<T> allowedToTouch) where T : ITouchListener
    {
        foreach (T element in allowedToTouch) { this.allowedToTouch.Add(element); }
    }

    public bool IsUnconstrained()
    {
        return isUnconstrained;
    }

    public void AddActionAwaitingNextTap(Action action)
    {
        actionsAwaitingNextTap.Add(action);
    }

    public void AddResponseToAnyTouch(Action action) {
        onAnyTouch.Add(action);
    }

    public void RemoveResponseToAnyTouch(Action action) {
        onAnyTouch.Remove(action);
    }

    public void GetCurrentlyDragged(List<GameObject> draggedBuffer, string stage)
    {
        foreach (int key in touchListeners.Keys)
        {
            foreach (ITouchListener listener in touchListeners[key])
            {
                if (typeof(Draggable).IsInstanceOfType(listener))
                {
                    draggedBuffer.Add(((Draggable)listener).gameObject);
                    break;
                }
                else if (typeof(KeyboardKey).IsInstanceOfType(listener))
                {
                    Block spawned = ((KeyboardKey)listener).GetSpawned();
                    if (null != spawned)
                    {
                        Debug.Log("Adding spawned object");
                        draggedBuffer.Add(spawned.gameObject);
                        break;
                    }
                }
            }
        }
    }

    public void GetCurrentlyAwaitingTap(List<GameObject> tappedBuffer, string stage)
    {
        foreach (int key in awaitingTap.Keys)
        {
            ITappable[] tappables = awaitingTap[key];
            if (tappables.Length > 0)
            {
                tappedBuffer.Add(tappables[0].gameObject);
            }
        }
    }

    public void IndicateTapRejection()
    {
        Debug.Log("TAP REJECTION");
        PlayAudioClip(reject);
    }

    private void OnTouchDown(int touchId, Vector2 touchpoint) {
        Logging.LogTouchDown(touchId, touchpoint);
        double currentTime = TimeKeeper.time;
        Vector2 touchPointWorld = (Vector2)Camera.main.ScreenToWorldPoint(touchpoint);
        TouchInfo touchInfo = new TouchInfo(touchId, currentTime, touchPointWorld);
        touchInfos[touchId] = touchInfo;
        GameObject responded = RespondedObject(touchInfo);
        if (null != responded)
        {
            VisualizeTouchDownOnObject(responded, touchId);
            ITouchListener[] respondedListeners;
            if (isUnconstrained)
            {
                respondedListeners = responded.GetComponents<ITouchListener>().ToArray();
            }
            else
            {
                respondedListeners = responded.GetComponents<ITouchListener>().Where(listener => allowedToTouch.Contains(listener)).ToArray();
            }
            if (respondedListeners.Length > 0)
            {
                Logging.LogListenerTouchDown(touchId, responded);
            }
            foreach (ITouchListener listener in respondedListeners)
            {
                try
                {
                    listener.OnTouch(touchInfo);
                }
                catch (Exception e)
                {
                    ExceptionUtil.OnException(e);
                }
            }
            touchListeners[touchId] = respondedListeners;
            if (null != stageOrchestrator.GetStage())
            {
                ITappable[] respondedTappables = responded.GetComponents<ITappable>().ToArray();
                delayedTapped = null;
                delayedTouchInfo = null;
                awaitingTap[touchId] = respondedTappables;
            }
        }
        foreach (Action anyTouchResponse in onAnyTouch) {
            try
            {
                anyTouchResponse();
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
        }
    }

    private GameObject RespondedObject(TouchInfo touchInfo) {
        try
        {
            float half_height = Camera.main.orthographicSize;
            float half_width = Camera.main.aspect * half_height;
            Vector2 touchPos = touchInfo.getCurrentPos();
            for (int i = 0; i < ProbeHelper.RAYS_TO_CAST; ++i)
            {
                Vector2 probePoint = ProbeHelper.GetProbePoint(touchInfo.getCurrentPos(), PROBE_RADUIS, i);
                if (probePoint.x < -half_width || probePoint.x > half_width || probePoint.y < -half_height || probePoint.y > half_height) continue;
                GameObject probeResult = ProbeHelper.Probe(probePoint, isRelevant: IsRelevantTouchObject);
                if (null != probeResult) return probeResult;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private bool IsRelevantTouchObject(GameObject obj)
    {
        return null != obj.GetComponent<ITouchListener>() || null != obj.GetComponent<ITappable>();
    }

    private void OnTouchMove(int touchId, Vector2 touchpoint) {
        Logging.LogTouchMove(touchId, touchpoint);
        //Debug.Log("Touch Moved" + touchpoint);
        if (touchListeners.ContainsKey(touchId))
        {
            try
            {
                TouchInfo touchInfo = touchInfos[touchId];
                touchInfo.setCurrentPos((Vector2)Camera.main.ScreenToWorldPoint(touchpoint));
                CheckOnAwaiting();
                ITouchListener[] listeners = touchListeners[touchId];
                if (listeners.Length > 0)
                {
                    Logging.LogListenerTouchMove(touchId, listeners[0].gameObject);
                }
                foreach (ITouchListener touchListener in listeners)
                {
                    if (null != touchListener)
                    {
                        try
                        {
                            touchListener.TouchMoved(touchInfo);
                        }
                        catch (Exception e)
                        {
                            ExceptionUtil.OnException(e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
        }
    }

    private void OnTouchUp(int touchId, bool forced) {
        Logging.LogTouchUp(touchId, forced);
        //Debug.Log("Touch Up");
        VisualizeTouchUp(touchId);
        if (!touchInfos.ContainsKey(touchId)) return;
        TouchInfo touchInfo = touchInfos[touchId];
        if (!forced)
        {
            if (awaitingTap.ContainsKey(touchId))
            {
                if (Vector2.Distance(touchInfo.getCurrentPos(), touchInfo.getInitialPos()) < TAP_OFFSET)
                {
                    try
                    {
                        foreach (ITappable tappable in awaitingTap[touchId])
                        {
                            if (null != tappable)
                            {
                                if (isUnconstrained || allowedToTap.Contains(tappable))
                                {
                                    if (lastTapTime + MIN_TAP_INTERVAL < TimeKeeper.time)
                                    {
                                        Tap(tappable, touchInfo, isDelayed: false);
                                    }
                                    else
                                    {
                                        PlayAudioClip(reject);
                                    }
                                }
                                else if (allowedToTapDelayed.Contains(tappable))
                                {
                                    //Debug.Log("Delayed tap recorded");
                                    delayedTapped = tappable;
                                    delayedTouchInfo = new TouchInfo(touchInfo);
                                    delayedTapTime = TimeKeeper.time;
                                    PlayAudioClip(click);
                                }
                                else
                                {
                                    PlayAudioClip(reject);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ExceptionUtil.OnException(e);
                    }
                    lastTapTime = TimeKeeper.time;
                }
                awaitingTap.Remove(touchId);
            }
        }
        if (touchListeners.ContainsKey(touchId))
        {
            try
            {
                ITouchListener[] listeners = touchListeners[touchId];
                if (listeners.Length > 0)
                {
                    Logging.LogListenerTouchUp(touchId, listeners[0].gameObject, forced);
                }
                foreach (ITouchListener touchListener in listeners)
                {
                    if (null != touchListener)
                    {
                        try
                        {
                            touchListener.OnTouchUp(touchInfo);
                        }
                        catch (Exception e)
                        {
                            ExceptionUtil.OnException(e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
            //Debug.Log("Block " + targetBlock.letter + " released");
            touchListeners.Remove(touchId);
        }
        touchInfos.Remove(touchId);
    }

    private void PlayAudioClip(AudioClip audioClip)
    {
        if (audioSource.isPlaying) return;
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    private void Tap(ITappable tappable, TouchInfo touchInfo, bool isDelayed)
    {
        Logging.LogTap(touchInfo.getTouchId(), tappable.gameObject, isDelayed);
        try
        {
            tappable.OnTap(touchInfo);
        }
        catch (Exception e)
        {
            ExceptionUtil.OnException(e);
        }
        List<Action> toPerformOnTap = new List<Action>(actionsAwaitingNextTap);
        actionsAwaitingNextTap.Clear();
        foreach (Action action in toPerformOnTap) {
            try
            {
                action();
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
        }
    }

    private void CheckOnAwaiting()
    {
        List<int> awaitingTapKeys = new List<int>(awaitingTap.Keys);
        foreach (int key in awaitingTapKeys)
        {
            if (!touchInfos.ContainsKey(key)) { awaitingTap.Remove(key); continue; }
            TouchInfo touchInfo = touchInfos[key];
            if (Vector2.Distance(touchInfo.getCurrentPos(), touchInfo.getInitialPos()) > TAP_OFFSET)
            {
                awaitingTap.Remove(key);
            }
        }
    }

    private void VisualizeTouchDownOnObject(GameObject respondedObject, int touchID)
    {
        if (null != respondedObject.GetComponent<PictureBlock>()) return;
        List<int> touches = DictUtil.GetOrSpawn(objectsRespondingToTouches, respondedObject);
        if (0 == touches.Count) {
            Vector3 localScale = respondedObject.transform.localScale;
            respondedObject.transform.localScale = new Vector3(INFLATION_ON_TAP * localScale.x, INFLATION_ON_TAP * localScale.y, localScale.z);
        }
        touches.Add(touchID);
    }

    private void VisualizeTouchUp(int touchID)
    {
        List<GameObject> listObjectsRespondingToTouches = new List<GameObject>(objectsRespondingToTouches.Keys);
        foreach (GameObject respondingObject in listObjectsRespondingToTouches)
        {
            List<int> touches = objectsRespondingToTouches[respondingObject];
            touches.RemoveAll(aTouchID => aTouchID == touchID);
            if (0 == touches.Count) {
                objectsRespondingToTouches.Remove(respondingObject);
                Vector3 localScale = respondingObject.transform.localScale;
                respondingObject.transform.localScale = new Vector3(localScale.x / INFLATION_ON_TAP, localScale.y / INFLATION_ON_TAP, localScale.z);
            }
        }
    }
}
