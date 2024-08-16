using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialLessonBlockDrag : IPlugInTutorialLesson
{
    public string Topic { get; } = "correct-block-tapped";

    public string Name { get; } = "block-drag";

    public string[] Prerequisites { get; } = { };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        touchManager = stageObject.GetComponent<TouchManager>();
        wordBox = GameObject.FindWithTag("WordBox").GetComponent<WordBox>();
        wordBoxBackground = wordBox.transform.Find("background").gameObject;
    }

    public IEnumerator GiveLesson(List<SynQuery> synSequence, object extraArgument)
    {
        Logging.LogLessonStart(Name);
        environment.GetRoboPartner().LookAtTablet();
        yield return synthesizerHelper.SpeechCoroutine(SynQuery.Seq(synSequence), cause: "tutorial:correct-block-tapped:clear-seq");
        if (0 == wordBox.BlocksCount())
        {
            int speechID = synthesizerHelper.Speak("Now drag the block into this box.", cause: "tutorial:correct-block-tapped:drag-here", canInterrupt: false, keepPauses: false);
            tutorial.PointAt(wordBoxBackground, axis: Vector2.zero, cause: "tutorial:correct-block-tapped:drag-here");
            while (0 == wordBox.BlocksCount()) yield return null;
            if (synthesizerHelper.IsSpeaking(speechID)) { synthesizerHelper.InterruptSpeech(speechID); }
        }
        completed = true;
        Logging.LogLessonEnd(Name);
    }

    public bool CheckCompletion()
    {
        return completed;
    }

    private Environment environment;
    private Tutorial tutorial;
    private SynthesizerController synthesizerHelper;
    private TouchManager touchManager;
    private WordBox wordBox;
    private GameObject wordBoxBackground;
    private bool completed = false;

}