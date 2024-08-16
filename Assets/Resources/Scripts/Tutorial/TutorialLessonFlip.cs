using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialLessonFlip : IStandaloneTutorialLesson
{
    public string Name { get; } = "flip";

    public string[] Prerequisites { get; } = { "pictureblock-pinch" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        touchManager = stageObject.GetComponent<TouchManager>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        compositionRoot = GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>();
        flipper = GameObject.FindWithTag("Flipper").GetComponent<Flipper>();
        flipper.gameObject.SetActive(false);
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "canvas" == stage && 0 == touchManager.GetTouchCount() && compositionRoot.GetTopPictureBlocks().Count > 0;
    }

    public IEnumerator GiveLesson()
    {
        touchManager.Constrain();
        touchManager.AddAllowedToTouch(compositionRoot.GetTopPictureBlocks().Select(pblock => pblock.GetComponent<Draggable>()));
        environment.GetRoboPartner().LookAtChild();
        yield return synthesizerHelper.SpeechCoroutine("I think this image might look even better if it faces the other way!",
            cause: "tutorial:flip:intro",
            canInterrupt: false);
        environment.GetRoboPartner().LookAtTablet();
        flipper.gameObject.SetActive(true);
        yield return tutorial.DemonstrateUIElement(flipper.gameObject, Vector2.zero,
            prompt: "To flip it, please drag the image onto this arrow!",
            cause: "tutorial:flip:demo");
        double t0 = TimeKeeper.time;
        while (!flipper.EverFlipped() && TimeKeeper.time - t0 < 15f) { yield return null; }
        touchManager.Unconstrain();
        completed = true;
    }

    public bool CheckCompletion()
    {
        return completed;
    }

    private Environment environment;
    private Tutorial tutorial;
    private TouchManager touchManager;
    private SynthesizerController synthesizerHelper;
    private Composition compositionRoot;
    private Flipper flipper;
    private bool completed = false;
}