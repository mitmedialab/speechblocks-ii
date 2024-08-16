
using System.Collections;
using UnityEngine;

public class TutorialLessonPictureBlockDrag2nd : IStandaloneTutorialLesson
{
    public string Name { get; } = "pictureblock-drag-2nd";

    public string[] Prerequisites { get; } = { "speech-reco" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        GameObject resultBoxObject = GameObject.FindWithTag("ResultBox");
        resultBox = resultBoxObject.GetComponent<ResultBox>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "keyboard" == stage &&  null != resultBox.GetSpawnedPictureBlock();
    }

    public IEnumerator GiveLesson()
    {
        tutorial.PointAt(resultBox.GetSpawnedPictureBlock(),
            axis: Vector2.up,
            cause: "tutorial:pictureblock-drag-2nd:show-direction");
        yield return synthesizerHelper.SpeechCoroutine("Remember to tap on the image to add it to the picture!",
            cause: "tutorial:pictureblock-drag-2nd:show-direction",
            canInterrupt: false);
        completed = true;
    }

    public bool CheckCompletion()
    {
        return completed;
    }

    private Tutorial tutorial;
    private ResultBox resultBox;
    private SynthesizerController synthesizerHelper;
    private bool completed = false;
}