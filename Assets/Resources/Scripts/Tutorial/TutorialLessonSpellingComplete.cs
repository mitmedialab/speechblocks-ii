
using System.Collections;
using System.Text;
using System.Linq;
using UnityEngine;

public class TutorialLessonSpellingComplete : IStandaloneTutorialLesson
{
    public string Name { get; } = "spelling-complete";

    public string[] Prerequisites { get; } = { "spelling" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        environment = stageObject.GetComponent<Environment>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        touchManager = stageObject.GetComponent<TouchManager>();
        resultBox = GameObject.FindWithTag("ResultBox").GetComponent<ResultBox>();
        wordDrawer = GameObject.FindWithTag("WordDrawer").GetComponent<WordDrawer>();
        wordDrawer.GetKeyboard().transform.Find("dn-handle").gameObject.SetActive(false);
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "keyboard" == stage && wordDrawer.IsDisplayingKeyboard() && null != resultBox.GetWordSense();
    }

    public IEnumerator GiveLesson()
    {
        touchManager.Constrain();
        wordDrawer.GetKeyboard().transform.Find("dn-handle").gameObject.SetActive(true);
        environment.GetRoboPartner().LookAtChild();
        string prompt = "Wow, it was so fun to build this word with you! I love how hard you worked!";
        yield return synthesizerHelper.SpeechCoroutine(prompt, cause: "tutorial:spelling-complete", canInterrupt: false);
        completed = true;
        touchManager.Unconstrain();
    }

    public bool CheckCompletion()
    {
        return completed;
    }

    private Environment environment;
    private ResultBox resultBox;
    private TouchManager touchManager;
    private SynthesizerController synthesizerHelper;
    private WordDrawer wordDrawer;
    private bool completed = false;
}