using UnityEngine;

public class LoginButton : MonoBehaviour, ITappable
{
    private User user = null;

    public void Setup(User user)
    {
        this.user = user;
        GetComponent<Picture>().Setup(user.GetNameSense(), 0.9f, 0.9f, "login");
    }

    public void OnTap(TouchInfo touchInfo)
    {
        GameObject.FindWithTag("StageObject").GetComponent<Environment>().PickUser(user);
    }

    public User GetUser()
    {
        return user;
    }
}
