
using System.Collections;
using UnityEngine;

public class TutorialLessonCanvas : IStandaloneTutorialLesson
{
    public string Name { get; } = "canvas";

    public string[] Prerequisites { get; } = { };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        touchManager = stageObject.GetComponent<TouchManager>();
        GameObject drawerObject = GameObject.FindWithTag("WordDrawer");
        wordDrawer = drawerObject.GetComponent<WordDrawer>();
        handleObject = drawerObject.transform.Find("up-handle").gameObject;
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "canvas" == stage;
    }

    public IEnumerator GiveLesson()
    {
        touchManager.Constrain();
        yield return synthesizerHelper.SpeechCoroutine("This is a place for making the picture.", cause: "tutorial:canvas:intro", canInterrupt: false);
        yield return tutorial.InviteToTap(handleObject.GetComponent<ITappable>(),
            pointingAxis: Vector2.zero,
            prompt: "Let's add something to it!",
            tapInvitation: "Press the plus button!",
            mandatoryTap: true,
            cause: "tutorial:canvas:lets-press");
        touchManager.Unconstrain();
    }

    public bool CheckCompletion()
    {
        return wordDrawer.IsDeployed();
    }

    private Tutorial tutorial;
    private Environment environment;
    private SynthesizerController synthesizerHelper;
    private TouchManager touchManager;
    private WordDrawer wordDrawer;
    private GameObject handleObject;
}