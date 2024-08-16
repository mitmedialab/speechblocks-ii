public class User
{
    private string fullname;
    private string shortname;
    private string name_sense;
    private string user_id;
    private bool consented_to_video;
    private bool inChildDrivenCondition;
    private bool inExpressiveCondition;

    public User(string user_id, string fullname, bool consented_to_video, bool childDriven, bool expressive)
    {
        this.fullname = fullname;
        this.user_id = user_id;
        this.consented_to_video = consented_to_video;
        name_sense = Vocab.GetNameSenseFromFullName(fullname);
        shortname = Vocab.GetWord(name_sense);
        inChildDrivenCondition = childDriven;
        inExpressiveCondition = expressive;
    }

    public string GetID() { return user_id; }

    public string GetFullName() { return fullname; }

    public string GetShortName() { return shortname; }

    public string GetNameSense() { return name_sense; }

    public bool IsConsentedToVideo() { return consented_to_video; }

    public bool InChildDrivenCondition() { return inChildDrivenCondition; }

    public bool InExpressiveCondition() { return inExpressiveCondition; }
}
