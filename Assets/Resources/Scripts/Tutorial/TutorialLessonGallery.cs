
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TutorialLessonGallery : IStandaloneTutorialLesson
{
    public string Name { get; } = "gallery";

    public string[] Prerequisites { get; } = { };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        touchManager = stageObject.GetComponent<TouchManager>();
        gallery = GameObject.FindWithTag("Gallery").GetComponent<Gallery>();
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "gallery" == stage;
    }

    public IEnumerator GiveLesson()
    {
        environment.GetRoboPartner().LookAtTablet();
        touchManager.Constrain();
        yield return synthesizerHelper.SpeechCoroutine("This is going to be your own picturebook!",
            cause: "tutorial:gallery:intro",
            canInterrupt: false);
        List<GameObject> exitButtons = gallery.GetPageElements("cross_button");
        List<GameObject> helpButtons = gallery.GetPageElements("help_button");
        GameObject newSceneButton;
        List<GameObject> currentSceneButtons = gallery.GetButtonsForCurrentScenesAndNewScene(out newSceneButton);
        List<ITappable> allExpectedToTap = currentSceneButtons.Concat(exitButtons).Concat(new List<GameObject>() { newSceneButton }).Select(obj => obj.GetComponent<ITappable>()).ToList();
        touchManager.AddAllowedToTapDelayed(allExpectedToTap);
        if (currentSceneButtons.Count > 0) {
            string them = currentSceneButtons.Count > 1 ? "one of them" : "it";
            string theseAreThePictures = currentSceneButtons.Count > 1 ? "These are the pictures " : "This is the picture";
            yield return tutorial.DemonstrateUIElement(uiElement: currentSceneButtons,
                pointingAxis: currentSceneButtons.Count > 1 ? new Vector2(-0.5f, 1) : Vector2.zero,
                prompt: $"{theseAreThePictures} that you made! To return to {them}, press that picture.",
                cause: "tutorial:gallery:scene-buttons");
        }
        yield return tutorial.DemonstrateUIElement(uiElement: newSceneButton,
                pointingAxis: Vector2.zero,
                prompt: "The plus button starts a new picture.",
                "tutorial:gallery:new-button");
        yield return tutorial.DemonstrateUIElement(uiElement: exitButtons,
                pointingAxis: Vector2.zero,
                prompt: "This cross button exits the game.",
                cause: "tutorial:gallery:exit");
        string tapDescription = ("jibo" == environment.GetStationType()) ? "press the question button" : "tap on me";
        yield return tutorial.DemonstrateUIElement(uiElement: helpButtons,
                pointingAxis: Vector2.zero,
                prompt:$"If you forgot how to play, {tapDescription}, and I will remind you.",
                cause: "tutorial:gallery:help");
        environment.GetRoboPartner().LookAtChild();
        touchManager.Unconstrain();
        isCompleted = true;
        yield return synthesizerHelper.Speak("Enjoy the game!", cause: "tutorial:gallery:end", boundToStages: null);
    }

    public bool CheckCompletion()
    {
        return isCompleted;
    }

    private Tutorial tutorial;
    private Environment environment;
    private SynthesizerController synthesizerHelper;
    private TouchManager touchManager;
    private Gallery gallery;
    private bool isCompleted = false;
}