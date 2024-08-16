using System;
using System.Collections;
using UnityEngine;
using System.Linq;

public class TutorialLessonIdeaButton: IStandaloneTutorialLesson
{
    public string Name { get; } = "idea-button";

    public string[] Prerequisites { get; } = { "pictureblock-pinch" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        touchManager = stageObject.GetComponent<TouchManager>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        stageOrchestrator = stageObject.GetComponent<StageOrchestrator>();
        ideaButtons = GameObject.FindGameObjectsWithTag("IdeaButton");
        foreach (GameObject ideaButton in ideaButtons)
        {
            ideaButton.SetActive(false);
        }
        handleObject = GameObject.FindWithTag("WordDrawer").transform.Find("up-handle").gameObject;
        ideaMaster = stageObject.GetComponent<IdeaMaster>();
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "canvas" == stage && 0 == touchManager.GetTouchCount();
    }

    public IEnumerator GiveLesson()
    {
        touchManager.Constrain();
        touchManager.AddAllowedToTouch(GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>().GetTopPictureBlocks().Select(pblock => pblock.GetComponent<Draggable>()));
        environment.GetRoboPartner().LookAtChild();
        yield return synthesizer.SpeechCoroutine("Let's add something else to our picture!", cause: "tutorial:idea-button:intro", canInterrupt: false);
        environment.GetRoboPartner().LookAtTablet();
        foreach (GameObject ideaButton in ideaButtons)
        {
            ideaButton.SetActive(true);
        }
        GameObject canvasIdeaButton = Array.Find(ideaButtons, button => button.transform.parent == null);
        yield return tutorial.InviteToTap(tappable: canvasIdeaButton.GetComponent<ITappable>(),
            pointingAxis: Vector2.zero,
            prompt: $"I can tell you some ideas on what we can make",
            tapInvitation: "Tap the lightbulb to try it!",
            mandatoryTap: true,
            cause: "tutorial:idea-button:demo");
        environment.GetRoboPartner().LookAtChild();
        while (!ideaMaster.GaveAnyIdeas()) yield return null;
        while (synthesizer.IsSpeaking()) yield return null;
        touchManager.AddAllowedToTapDelayed(canvasIdeaButton.GetComponent<ITappable>());
        touchManager.AddAllowedToTapDelayed(handleObject.GetComponent<ITappable>());
        yield return synthesizer.SpeechCoroutine("You can use this idea, or make something else instead. If you want more ideas, you can tap on this button again.",
            cause: "tutorial:idea-button:use-this-or-not",
            canInterrupt: false);
        completed = true;
        yield return synthesizer.SpeechCoroutine("Now, do you remember how we can add things to the picture? That's right, we need to tap on the plus button!",
            cause: "tutorial:idea-button:invite-back",
            canInterrupt: false);
        tutorial.PointAt(handleObject, Vector2.zero, "tutorial:idea-button:invite-back");
        touchManager.Unconstrain();
    }

    public bool CheckCompletion()
    {
        return completed;
    }

    private Environment environment;
    private Tutorial tutorial;
    private TouchManager touchManager;
    private SynthesizerController synthesizer;
    private StageOrchestrator stageOrchestrator;
    private GameObject[] ideaButtons;
    private GameObject handleObject;
    private IdeaMaster ideaMaster;
    private bool completed = false;
}