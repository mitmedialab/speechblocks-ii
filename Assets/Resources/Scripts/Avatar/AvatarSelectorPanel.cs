using System;
using System.Collections.Generic;
using UnityEngine;

public class AvatarSelectorPanel : MonoBehaviour
{
    private AnimationMaster animaster = null;
    private GameObject avatarButtonPrefab = null;
    private AvatarControl mainAvatar = null;
    private ButtonsArranger buttonsArranger = null;
    private Vector3 hidingPlace;
    private List<AvatarSelectorButton> buttons = new List<AvatarSelectorButton>();
    private int clearingCount = 0;

    private string invokedWithProperties = null;

    private void Start()
    {
        hidingPlace = transform.localPosition;
        avatarButtonPrefab = Resources.Load<GameObject>("Prefabs/avatar_selector_button");
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        animaster = stageObject.GetComponent<AnimationMaster>();
        mainAvatar = GameObject.FindWithTag("AvatarPicker").transform.Find("Avatar").GetComponent<AvatarControl>();
        Vector2 myBoxSize = transform.Find("background").GetComponent<BoxCollider2D>().size;
        buttonsArranger = new ButtonsArranger(x0: 0,
                                              y0: 0,
                                              areaWidth: myBoxSize.x - 0.5f,
                                              areaHeight: myBoxSize.y - 0.5f,
                                              buttonWidth: ButtonsArranger.GetButtonWidth(avatarButtonPrefab),
                                              buttonHeight: ButtonsArranger.GetButtonHeight(avatarButtonPrefab));
    }

    public void Deploy(GameObject invokingButton, string propertiesToSelect, IEnumerable<string> candidateValues)
    {
        invokedWithProperties = propertiesToSelect;
        Clear();
        SpawnButtons(propertiesToSelect, candidateValues);
        Opacity.SetOpacity(gameObject, 0);
        animaster.StartFade(gameObject, target: 1, duration: 0.5f);
        float xOffset = invokingButton.transform.localPosition.x > 0 ? -hidingPlace.x : hidingPlace.x;
        transform.localPosition = new Vector3(xOffset, hidingPlace.y, hidingPlace.z);
        animaster.StartLocalGlide(gameObject, new Vector2(xOffset, 0.0f), duration: 0.3f);
    }

    public string InvokedWithProperty()
    {
        return invokedWithProperties;
    }

    public bool IsDeployed()
    {
        return Mathf.Abs(transform.localPosition.y) < 0.01f;
    }

    public bool IsRetracted()
    {
        return Mathf.Abs(transform.localPosition.y - hidingPlace.y) < 0.01f;
    }

    public void Retract()
    {
        invokedWithProperties = null;
        animaster.StartLocalGlide(gameObject, new Vector2(transform.localPosition.x, hidingPlace.y), duration: 0.25f);
        float originalClearingCount = clearingCount;
        StartCoroutine(CoroutineUtils.DoWithDelay(action: Clear, time: 0.6f, defuse: () => originalClearingCount != clearingCount));
    }

    public void OnSelection(AvatarSelectorButton avatarButton)
    {
        Retract();
        avatarButton.transform.SetParent(mainAvatar.transform, worldPositionStays: true);
        animaster.StartLocalGlide(avatarButton.gameObject, Vector2.zero, duration: 0.5f);
        animaster.StartFade(avatarButton.gameObject, target: 0, duration: 0.5f);
    }

    private void SpawnButtons(string propertiesToSelect, IEnumerable<string> candidateValues)
    {
        int i = 0;
        foreach (string valueOption in candidateValues)
        {
            GameObject avatarButtonObject = Instantiate(avatarButtonPrefab);
            AvatarSelectorButton avatarButton = avatarButtonObject.GetComponent<AvatarSelectorButton>();
            avatarButton.Setup(this, mainAvatar, propertiesToSelect, valueOption);
            avatarButtonObject.transform.SetParent(transform, worldPositionStays: false);
            avatarButtonObject.transform.localPosition = buttonsArranger.GetButtonPos3D(i);
            buttons.Add(avatarButton);
            ++i;
        }
    }

    private void Clear()
    {
        foreach (AvatarSelectorButton button in buttons)
        {
            Destroy(button.gameObject);
        }
        buttons.Clear();
        ++clearingCount;
    }
}
