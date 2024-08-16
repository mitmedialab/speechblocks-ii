using System.Collections.Generic;
using UnityEngine;

public class LoginPage : Panel
{
    private List<LoginButton> loginButtons = new List<LoginButton>();

    public void Setup(List<User> users)
    {
        users = new List<User>(users);
        users.Add(GuestUser(0 != users.Count ? users[0] : null));
        Debug.Log("LOGIN PAGE SETUP");
        base.SetupPanel();
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        TouchManager touchManager = stageObject.GetComponent<TouchManager>();
        touchManager.Unconstrain();
        //stageObject.GetComponent<ISynthesizer>().Speak("Please tap a button to tell me who is going to play with me!", canInterrupt: true, keepPauses: false);
        GameObject buttonPrefab = Resources.Load<GameObject>("Prefabs/login_button");
        ButtonsArranger buttonsArranger = SetupButtonsArranger(buttonPrefab);
        for (int i = 0; i < users.Count; ++i)
        {
            LoginButton loginButton = Instantiate(buttonPrefab).GetComponent<LoginButton>();
            loginButton.Setup(users[i]);
            loginButton.transform.SetParent(transform, false);
            loginButton.transform.localPosition = buttonsArranger.GetButtonPos3D(i);
            loginButtons.Add(loginButton);
        }
        GenericTappable exitTappable = transform.Find("cross_button").GetComponent<GenericTappable>();
        exitTappable.AddAction(() => Application.Quit());
    }

    public override string GetSortingLayer()
    {
        return "login";
    }

    private ButtonsArranger SetupButtonsArranger(GameObject buttonPrefab)
    {
        float height = 2 * Camera.main.orthographicSize;
        float width = Camera.main.aspect * height;
        float buttonSize = ButtonsArranger.GetButtonWidth(buttonPrefab);
        return new ButtonsArranger(x0: 1, y0: 0, areaWidth: width - 2.5f, areaHeight: height - 1, buttonWidth: buttonSize, buttonHeight: buttonSize);
    }

    public List<LoginButton> GetLoginButtons()
    {
        return loginButtons;
    }

    private User GuestUser(User sampleUser)
    {
        bool childDriven = null != sampleUser ? sampleUser.InChildDrivenCondition() : true;
        bool expressive = null != sampleUser ? sampleUser.InExpressiveCondition() : true;
        return new User("guest", "guest", consented_to_video: false, childDriven: childDriven, expressive: expressive);
    }
}
