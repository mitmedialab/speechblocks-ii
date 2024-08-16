using UnityEngine;

public class Block : BlockBase, ITappable {
    private static float height = 0;
    private bool terminationFlag = false;

    public override void Setup(PGPair pgPair, string cause)
    {
        base.Setup(pgPair, cause);
        gameObject.name = $"{pgPair.GetPhonemeCode()}-{pgPair.GetGrapheme()}-block";
    }

    public void Setup(PGPair pgPair, string sortingLayer, string cause) {
        Setup(pgPair, cause);
        ZSorting.SetSortingLayer(gameObject, sortingLayer);
    }

    public static float GetStandardHeight() {
        if (0 == height) {
            GameObject blockPrefab = Resources.Load<GameObject>("Prefabs/Block");
            GameObject blockObj = blockPrefab.transform.Find("block").gameObject;
            height = blockObj.transform.localScale.y * blockObj.GetComponent<SpriteRenderer>().size.y;
        }
        return height;
    }

    public void OnTap(TouchInfo touchInfo) {
        if (base.GetPhonemeCode() == "") return;
        PlayAudio(cause: "tap");
    }

    public bool IsTouched() {
        return GetComponent<Draggable>().IsTouched();
    }

    public Vector2 CurrentTouchPos()
    {
        return GetComponent<Draggable>().CurrentTouchPos();
    }

    public void SetDragParent(bool dragParent)
    {
        GetComponent<Draggable>().SetDragParent(dragParent);
    }

    public bool IsAlive()
    {
        return null != gameObject && !terminationFlag;
    }

    // this is a workaround to Unity being somewhat wonky with destroying objects.
    // if a block is destroyed, we it to be explicitly marked
    public void Terminate()
    {
        terminationFlag = true;
        Destroy(gameObject);
    }
}
