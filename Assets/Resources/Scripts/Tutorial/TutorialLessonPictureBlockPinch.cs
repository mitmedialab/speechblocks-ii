using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialLessonPictureBlockPinch : IStandaloneTutorialLesson
{
    public string Name { get; } = "pictureblock-pinch";

    public string[] Prerequisites { get; } = { "pictureblock-drag" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        touchManager = stageObject.GetComponent<TouchManager>();
        compositionRoot = GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>();
        Draggable.AddPinchCallback(() => completed = true);
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "canvas" == stage && 0 == touchManager.GetTouchCount() && compositionRoot.GetTopPictureBlocks().Count > 0;
    }

    public IEnumerator GiveLesson()
    {
        if (completed) yield break;
        GameObject[] allPictureBlocks = GameObject.FindGameObjectsWithTag("PictureBlock");
        List<PictureBlock> topPictureBlocks = compositionRoot.GetTopPictureBlocks();
        touchManager.Constrain();
        touchManager.AddAllowedToTouch(allPictureBlocks.Select(pblock => pblock.GetComponent<Draggable>()));
        environment.GetRoboPartner().LookAtTablet();
        yield return synthesizerHelper.SpeechCoroutine("The picture is looking great, only a little too small! Let's make it bigger.",
            cause: "tutorial:pictureblock-pinch:intro",
            canInterrupt: false);
        if (completed) yield break;
        PictureBlock pictureBlock = LinqUtil.MinBy(topPictureBlocks, pblock => pblock.transform.localScale.y, valIfEmpty: null);
        if (null != pictureBlock)
        {
            tutorial.PointAway(pictureBlock.gameObject, axis: Vector2.up + Vector2.left, offset: 3f, cause: "tutorial:pictureblock-pinch:demo");
            tutorial.PointAway(pictureBlock.gameObject, axis: Vector2.down + Vector2.right, offset: 3f, cause: "tutorial:pictureblock-pinch:demo");
            yield return synthesizerHelper.SpeechCoroutine("When you put two fingers on the image, you can stretch and squish it, and also turn it. Try it out!",
                cause: "tutorial:pictureblock-pinch:demo",
                canInterrupt: false);
            double t0 = TimeKeeper.time;
            while (!completed && TimeKeeper.time - t0 < 10) yield return null;
        }
        completed = true;
        touchManager.Unconstrain();
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
    private bool completed = false;
}