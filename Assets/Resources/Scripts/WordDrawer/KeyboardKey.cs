using System.Collections.Generic;
using UnityEngine;

public class KeyboardKey : BlockBase, ITouchListener, ITappable {
    private Block spawned = null;
    private static GameObject blockPrefab = null;
    private int trackedTouch = -1;
    private AnimationMaster animaster = null;
    private Scaffolder scaffolder = null;
    private bool isActive = true;
    private TouchManager touchManager = null;

    void Start()
    {
        if (null != animaster) return;
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        animaster = stageObject.GetComponent<AnimationMaster>();
        scaffolder = stageObject.GetComponent<Scaffolder>();
        touchManager = stageObject.GetComponent<TouchManager>();
        gameObject.AddComponent<Opacity>();
    }

    public override void Setup(PGPair pgPair, string cause) {
        if (null == animaster) Start();
        if (null == blockPrefab) blockPrefab = Resources.Load<GameObject>("Prefabs/Block");
        base.Setup(pgPair, cause);
    }

    public bool IsActive()
    {
        return isActive;
    }

    public void OnTap(TouchInfo touchInfo) {
        if (isActive)
        {
            if (GetPhonemeCode() == "") return;
            PlayAudio(cause: "tap");
        }
        scaffolder.RegisterKeyTap(this);
    }

    public void OnTouch(TouchInfo touchInfo) {
        if (!isActive || trackedTouch >= 0) return;
        trackedTouch = touchInfo.getTouchId();
    }

    public void TouchMoved(TouchInfo touchInfo) {
        if (touchInfo.getTouchId() != trackedTouch) return;
        if (null == spawned) {
            if ((touchInfo.getCurrentPos() - touchInfo.getInitialPos()).magnitude > 0.5) {
                spawned = Spawn(touchInfo.getCurrentPos());
                spawned.GetComponent<Draggable>().OnTouch(touchInfo);
            }
        }
        else {
            spawned.GetComponent<Draggable>().TouchMoved(touchInfo);
        }
    }

    public Block GetSpawned()
    {
        return spawned;
    }

    public Block Spawn(Vector2 position)
    {
        Block spawnedBlock = ((GameObject)Instantiate(blockPrefab)).GetComponent<Block>();
        spawnedBlock.transform.position = new Vector3(position.x, position.y, transform.position.z - 2);
        spawnedBlock.Setup(GetPGPair(), "word_drawer", "auto");
        spawnedBlock.SetDragParent(false);
        spawnedBlock.transform.SetParent(GameObject.FindWithTag("WordDrawer").transform, true);
        Vector3 blockLocalScale = spawnedBlock.transform.localScale;
        spawnedBlock.transform.localScale = new Vector3(TouchManager.INFLATION_ON_TAP * blockLocalScale.x, TouchManager.INFLATION_ON_TAP * blockLocalScale.y, blockLocalScale.z);
        Logging.LogBirth(spawnedBlock.gameObject, GetComponent<Logging>().GetLogID());
        if (!touchManager.IsUnconstrained()) { touchManager.AddAllowedToTouch(spawnedBlock.GetComponent<Draggable>()); }
        return spawnedBlock;
    }

    public void OnTouchUp(TouchInfo touchInfo) {
        if (touchInfo.getTouchId() != trackedTouch) return;
        trackedTouch = -1;
        if (null == spawned) return;
        spawned.GetComponent<Draggable>().OnTouchUp(touchInfo);
        Vector3 blockLocalScale = spawned.transform.localScale;
        spawned.transform.localScale = new Vector3(blockLocalScale.x / TouchManager.INFLATION_ON_TAP, blockLocalScale.y / TouchManager.INFLATION_ON_TAP, blockLocalScale.z);
        spawned = null;
    }

    public void Deactivate()
    {
        if (!isActive) return;
        animaster.StartAnimation(gameObject, typeof(Fade), /*opacity*/ 0.3f, /*duration*/ 1f);
        isActive = false;
        //if (-1 != trackedTouch) TODO
        //{
        //}
    }

    public void Activate()
    {
        if (isActive) return;
        animaster.StartAnimation(gameObject, typeof(Fade), /*opacity*/ 1f, /*duration*/ 0.3f);
        isActive = true;
    }
}
