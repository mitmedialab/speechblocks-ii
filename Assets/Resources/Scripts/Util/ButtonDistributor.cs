using System;
using System.Collections.Generic;
using UnityEngine;

public class ButtonDistributor<T>
{
    private GameObject buttonsHolder;
    private Func<T, GameObject> buttonInitializer;
    private int recordedOffset = 0;
    private ButtonsArranger buttonArranger;
    private AnimationMaster animaster;
    private List<T> descriptors = new List<T>();
    private List<GameObject> pendingOrigins = new List<GameObject>();
    private List<GameObject> spawnedButtons = new List<GameObject>();
    private ButtonDistributor<T> previousDistributor = null;
    private ButtonDistributor<T> nextDistributor = null;
    private Environment environment = null;

    public ButtonDistributor(GameObject buttonsHolder, Func<T, GameObject> buttonInitializer, ButtonsArranger buttonArranger, AnimationMaster animaster, ButtonDistributor<T> previousDistributor)
    {
        this.buttonsHolder = buttonsHolder;
        this.buttonInitializer = buttonInitializer;
        this.buttonArranger = buttonArranger;
        this.animaster = animaster;
        this.previousDistributor = previousDistributor;
        if (null != previousDistributor) previousDistributor.nextDistributor = this;
        environment = GameObject.FindWithTag("StageObject").GetComponent<Environment>();
    }

    public void Update()
    {
        UpdateOffset();
        if (!IsCompleted() && PreviousDistributorsCompleted()) { SpawnButton(); }
    }

    // returns the list of buttons that were actually added
    public List<T> AddButtons(List<T> newDescriptors, GameObject origin)
    {
        List<T> actuallyAdded = new List<T>();
        int numberToAdd = Math.Min(newDescriptors.Count, buttonArranger.MaxButtons() - LastDistributor().EndingPosition());
        for (int i = 0; i < numberToAdd; ++i)
        {
            T descriptor = newDescriptors[i];
            if (descriptors.Contains(descriptor)) continue;
            descriptors.Add(descriptor);
            pendingOrigins.Add(origin);
            actuallyAdded.Add(descriptor);
        }
        return actuallyAdded;
    }

    public void SpawnButton()
    {
        T descriptor = descriptors[spawnedButtons.Count];
        GameObject button = buttonInitializer(descriptor);
        GameObject origin = pendingOrigins[0];
        pendingOrigins.RemoveAt(0);
        button.transform.SetParent(buttonsHolder.transform, false);
        button.transform.position = origin.transform.position;
        float scale = button.transform.localScale.x;
        button.transform.localScale = new Vector3(0.1f, 0.1f, 1);
        Opacity.SetOpacity(button, 0);
        animaster.StartFade(button, 1, 0.5f);
        animaster.StartLocalGlide(button, buttonArranger.GetButtonPos(recordedOffset + spawnedButtons.Count), 0.5f);
        animaster.StartScale(button, new Vector3(scale, scale, 1), 0.5f);
        spawnedButtons.Add(button);
        if (descriptors.Count > 0 && spawnedButtons.Count == descriptors.Count) { environment.GetRoboPartner().SuggestObjectsOfInterest(new List<GameObject> { spawnedButtons[spawnedButtons.Count - 1] }); }
    }

    public void Clear()
    {
        descriptors.Clear();
        pendingOrigins.Clear();
        foreach (GameObject button in spawnedButtons) { GameObject.Destroy(button); }
        spawnedButtons.Clear();
    }

    public int StartingPosition()
    {
        return null == previousDistributor ? 0 : previousDistributor.EndingPosition();
    }

    public int EndingPosition()
    {
        return StartingPosition() + descriptors.Count;
    }

    public bool ArrangerHasCapacity()
    {
        return LastDistributor().EndingPosition() < buttonArranger.MaxButtons();
    }

    public bool IsCompleted()
    {
        return 0 == pendingOrigins.Count;
    }

    private ButtonDistributor<T> LastDistributor()
    {
        if (null == nextDistributor) return this;
        return nextDistributor.LastDistributor();
    }

    private bool PreviousDistributorsCompleted()
    {
        return null == previousDistributor || previousDistributor.IsCompleted() && previousDistributor.PreviousDistributorsCompleted();
    }

    private void UpdateOffset()
    {
        int startingPosition = StartingPosition();
        if (this.recordedOffset != startingPosition)
        {
            this.recordedOffset = startingPosition;
            for (int i = 0; i < spawnedButtons.Count; ++i)
            {
                animaster.StartLocalGlide(spawnedButtons[i], buttonArranger.GetButtonPos(i + startingPosition), 0.5f);
            }
        }
    }
}
