using UnityEngine;
using SimpleJSON;

public class AvatarSelectorButton : MonoBehaviour, ITappable
{
    private AvatarSelectorPanel avatarSelectorPanel = null;
    private AvatarControl mainAvatar = null;
    private string propertiesToSet = null;
    private string valuesToAlter = null;

    public void Setup(AvatarSelectorPanel avatarSelectorPanel, AvatarControl mainAvatar, string propertiesToSet, string valuesToAlter)
    {
        this.avatarSelectorPanel = avatarSelectorPanel;
        this.mainAvatar = mainAvatar;
        this.propertiesToSet = propertiesToSet;
        this.valuesToAlter = valuesToAlter;
        AvatarControl myAvatar = transform.Find("Avatar").GetComponent<AvatarControl>();
        myAvatar.Setup(JSONNode.Parse(mainAvatar.GetDescription()));
        myAvatar.Alter(propertiesToSet, valuesToAlter);
    }

    public void OnTap(TouchInfo touchInfo)
    {
        if (!avatarSelectorPanel.IsDeployed()) return;
        mainAvatar.Alter(propertiesToSet, valuesToAlter);
        GameObject.FindWithTag("AvatarPicker").GetComponent<AvatarPicker>().UpdateUI();
        avatarSelectorPanel.OnSelection(this);
    }
}
