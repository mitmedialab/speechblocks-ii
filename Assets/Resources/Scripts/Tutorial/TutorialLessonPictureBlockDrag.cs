
using System.Collections;
using UnityEngine;

public class TutorialLessonPictureBlockDrag : IStandaloneTutorialLesson
{
    public string Name { get; } = "pictureblock-drag";

    public string[] Prerequisites { get; } = { "spelling-complete" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        GameObject resultBoxObject = GameObject.FindWithTag("ResultBox");
        resultBox = resultBoxObject.GetComponent<ResultBox>();
        resultBox.AddDeploymentCallback(OnDeploy);
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        touchManager = stageObject.GetComponent<TouchManager>();
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "keyboard" == stage &&  null != resultBox.GetSpawnedPictureBlock();
    }

    public IEnumerator GiveLesson()
    {
        touchManager.Constrain();
        GameObject pictureBlock = resultBox.GetSpawnedPictureBlock();
        environment.GetRoboPartner().LookAtTablet();
        yield return tutorial.DemonstrateUIElement(uiElement: pictureBlock,
            pointingAxis: Vector2.zero,
            prompt: "Do you see the little image that showed up?",
            cause: "tutorial:pictureblock-drag:intro");
        touchManager.AddAllowedToTouch(pictureBlock.GetComponent<Draggable>());
        yield return synthesizerHelper.SpeechCoroutine("Tap on it to put it on the picture that we are making!", cause: "tutorial:pictureblock-drag:tap", canInterrupt: false, boundToStages: null);
        while (!deployed) { yield return null; }
        yield return synthesizerHelper.SpeechCoroutine("You can drag it to anywhere you want it to be!",
            cause: "tutorial:pictureblock-drag:drag-anywhere",
            canInterrupt: false);
        yield return CoroutineUtils.WaitCoroutine(1.5f);
        while (0 != touchManager.GetTouchCount()) yield return null;
        touchManager.Unconstrain();
    }

    public bool CheckCompletion()
    {
        return deployed;
    }

    public void OnDeploy()
    {
        deployed = true;
    }

    private Environment environment;
    private Tutorial tutorial;
    private ResultBox resultBox;
    private TouchManager touchManager;
    private SynthesizerController synthesizerHelper;
    private bool deployed = false;
}