
using System.Collections;
using System.Linq;
using UnityEngine;

public class TutorialLessonCanvasFinalTouches : IStandaloneTutorialLesson
{
    public string Name { get; } = "canvas-final-touches";

    public string[] Prerequisites { get; } = { "pictureblock-pinch" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        touchManager = stageObject.GetComponent<TouchManager>();
        GameObject drawerObject = GameObject.FindWithTag("WordDrawer");
        wordDrawer = drawerObject.GetComponent<WordDrawer>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        recycleBin = GameObject.FindWithTag("TrashButton");
        recycleBin.SetActive(false);
        exitButton = GameObject.FindWithTag("Gallery").transform.Find("GalleryHandle").gameObject;
        exitButton.SetActive(false);
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "canvas" == stage && 0 == touchManager.GetTouchCount() && (!environment.GetUser().InChildDrivenCondition() || tutorial.IsLessonCompleted("speech-reco"));
    }

    public IEnumerator GiveLesson()
    {
        touchManager.Constrain();
        GameObject[] allPictureBlocks = GameObject.FindGameObjectsWithTag("PictureBlock");
        touchManager.AddAllowedToTouch(allPictureBlocks.Select(pblock => pblock.GetComponent<Draggable>()));
        touchManager.AddAllowedToTapDelayed(exitButton.GetComponent<ITappable>());
        environment.GetRoboPartner().LookAtChild();
        yield return synthesizerHelper.SpeechCoroutine("Now that you know how to build words, you can make whatever picture you want with them! Let me show you a few more things.",
            cause: "tutorial:canvas-final-touches:intro",
            canInterrupt: false);
        environment.GetRoboPartner().LookAtTablet();
        recycleBin.SetActive(true);
        yield return tutorial.DemonstrateUIElement(uiElement: recycleBin,
            pointingAxis: Vector2.zero, prompt: "If you don't like the images that you made anymore, you can drag them into this bin to delete them.",
            cause: "tutorial:canvas-final-touches:recycling-bin");
        if (!environment.GetUser().InChildDrivenCondition())
        {
            GameObject wordDrawerHandle = GameObject.FindWithTag("WordDrawer").transform.Find("up-handle").gameObject;
            yield return tutorial.DemonstrateUIElement(uiElement: wordDrawerHandle,
                pointingAxis: Vector2.zero, prompt: "You can tap on the plus button to make more words.",
                cause: "tutorial:canvas-final-touches:drawer-handle");
        }
        exitButton.SetActive(true);
        yield return tutorial.DemonstrateUIElement(uiElement: exitButton, pointingAxis: Vector2.zero, prompt: "And when you are done with the picture, press the cross button to finish it!",
            "tutorial:canvas-final-touches:gallery-handle");
        environment.GetRoboPartner().LookAtChild();
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
    private WordDrawer wordDrawer;
    private SynthesizerController synthesizerHelper;
    private GameObject exitButton;
    private GameObject recycleBin;
    private bool completed = false;
}