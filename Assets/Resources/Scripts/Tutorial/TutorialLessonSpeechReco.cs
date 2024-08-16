
using System.Collections;
using System.Linq;
using UnityEngine;

public class TutorialLessonSpeechReco : IStandaloneTutorialLesson
{
    public string Name { get; } = "speech-reco";

    public string[] Prerequisites { get; } = { "pictureblock-drag" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        touchManager = stageObject.GetComponent<TouchManager>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        speechRecoButton = GameObject.FindWithTag("SpeechRecoButton").GetComponent<SpeechRecoButton>();
        speechRecoButton.gameObject.SetActive(false);
    }

    public void ConfigureOnStartup()
    {
        speechRecoButton.gameObject.SetActive(false);
    }

    public bool InvitationCanStart(string stage)
    {
        return false;
    }

    public IEnumerator InviteToLesson()
    {
        return null;
    }

    public bool CanStart(string stage)
    {
        return "word_bank" == stage;
    }

    public IEnumerator GiveLesson()
    {
        touchManager.Constrain();
        environment.GetRoboPartner().LookAtTablet();
        speechRecoButton.gameObject.SetActive(true);
        yield return tutorial.InviteToTap(tappable: speechRecoButton,
            pointingAxis: Vector2.zero,
            prompt: "Guess what? You can tell me which word you want to make! Though sometimes I don't hear well. If I don't get your word, please forgive me and just say it again! To tell me a word, tap a microphone!",
            tapInvitation: null,
            mandatoryTap: true,
            cause: "tutorial:speech-reco:call-to-action");
        touchManager.Unconstrain();
    }

    public bool CheckCompletion()
    {
        return speechRecoButton.ButtonIsActive();
    }

    private Environment environment;
    private Tutorial tutorial;
    private TouchManager touchManager;
    private SynthesizerController synthesizerHelper;
    private SpeechRecoButton speechRecoButton;
}