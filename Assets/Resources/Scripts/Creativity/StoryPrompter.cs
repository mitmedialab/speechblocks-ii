using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryPrompter : MonoBehaviour
{
    private TouchManager touchManager;
    private Environment environment;
    private StageOrchestrator stageOrchestrator;
    private SynthesizerController synthesizer;
    private Composition composition = null;
    private CoroutineRunner promptRunner = new CoroutineRunner();
    private VoiceActivityDetector voiceActivityDetector = null;
    private SpeechRecoServerComm speechReco = null;
    private int storyPromptID = -1;
    private float promptProbability = 0.5f;
    private const float INITIAL_NO_VOICE_PERIOD = 5f;
    private const float NO_VOICE_PERIOD = 2.5f;

    private string[] ACKNOWLEDGEMENTS = { "Aha!", "Cool!", "Interesting!", "Got it!", "Fascinating!" };

    private void Start()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        touchManager = stageObject.GetComponent<TouchManager>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        voiceActivityDetector = stageObject.GetComponent<VoiceActivityDetector>();
        speechReco = stageObject.GetComponent<SpeechRecoServerComm>();
        environment = stageObject.GetComponent<Environment>();
        stageOrchestrator = stageObject.GetComponent<StageOrchestrator>();
        composition = GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>();
        GameObject.FindWithTag("ResultBox").GetComponent<ResultBox>().AddDeploymentCallback(OnDeployment);
    }

    private void Update()
    {
        promptRunner.Update();
    }

    private void OnDeployment()
    {
        List<PictureBlock> allPictureBlocks = composition.GetAllPictureBlocks();
        if (allPictureBlocks.Count < 2) return;
        if (RandomUtil.Range("story-prompter-should-launch", 0f, 1f) > promptProbability) return;
        promptRunner.SetCoroutine(StoryPromptCoroutine());
    }

    private IEnumerator StoryPromptCoroutine()
    {
        environment.GetRoboPartner().LookAtChild();
        yield return CoroutineUtils.WaitCoroutine(0.5f);
        while (0 != touchManager.GetTouchCount() || synthesizer.IsSpeaking()) yield return null;
        if (GetBreakConditions()) yield break;
        int speechID = -1;
        ++storyPromptID;
        yield return synthesizer.SpeechCoroutine(GetStoryPrompt(), cause: $"story-prompter-{storyPromptID}", out speechID);
        if (synthesizer.WasInterrupted(speechID)) yield break;
        promptProbability *= 0.6f;
        voiceActivityDetector.StartDetection(OnAudioClip, talkingEndOffset: 1.5f, shouldSendInterimClips: false);
        double tEnd = TimeKeeper.time + INITIAL_NO_VOICE_PERIOD;
        bool voiceDetected = false;
        while (!GetBreakConditions()) {
            if (voiceActivityDetector.IsPickingVoice()) { voiceDetected = true; }
            if (!voiceDetected && TimeKeeper.time > tEnd) { break; }
            yield return null;
        }
        voiceActivityDetector.StopDetection();
    }

    private IEnumerator AcknowledgementCoroutine()
    {
        environment.GetRoboPartner().LookAtChild();
        yield return synthesizer.SpeechCoroutine(RandomUtil.PickOne("story-prompter-ack-1", ACKNOWLEDGEMENTS), cause: "story-prompter-ack");
        double tEnd = TimeKeeper.time + NO_VOICE_PERIOD;
        bool voiceDetected = false;
        while (!GetBreakConditions()) {
            if (voiceActivityDetector.IsPickingVoice()) { voiceDetected = true; }
            if (!voiceDetected && TimeKeeper.time > tEnd) {
                yield return synthesizer.SpeechCoroutine(GetFinalPrompt(), cause: "story-prompter-ack-final");
                break;
            }
            yield return null;
        }
        voiceActivityDetector.StopDetection();
    }

    private bool GetBreakConditions()
    {
        return "canvas" != stageOrchestrator.GetStage();
    }

    private SynQuery GetStoryPrompt()
    {
        int condition = RandomUtil.Range("story-prompter-template", 0, 1);
        switch (condition)
        {
            case 0:
                {
                    string[] interesting = { "Interesting", "Cool", "Love your" };
                    string[] can_you_tell = { "Can you tell me", "Would you tell me", "Can you explain" };
                    string[] what_is_going_on = { "what is going on here", "what is happening here", "what story does it tell" };
                    return $"{RandomUtil.PickOne("story-prompt-1-1", interesting)} picture! {RandomUtil.PickOne("story-prompt-1-2", can_you_tell)} {RandomUtil.PickOne("story-prompt-1-3", what_is_going_on)}?";
                }
            default:
                {
                    string[] can_you_please_tell = { "Could you please tell me", "Would you please tell me", "Could you please let me know" };
                    string[] what_is_going_on_your_picture = { "what is going on your picture", "what is happening on your picture", "what story does your picture tell" };
                    return $"{RandomUtil.PickOne("story-prompt-2-1", can_you_please_tell)} {RandomUtil.PickOne("story-prompt-2-2", what_is_going_on_your_picture)}?";
                }
        }
    }

    private SynQuery GetFinalPrompt()
    {
        string[] opener = { "This is interesting!", "This is very cool!", "Love it!" };
        string[] closer = { "How about adding something else to the picture?", "Let's add more things to this picture!" };
        return $"{RandomUtil.PickOne("story-final-prompt-1", opener)} {RandomUtil.PickOne("story-final-prompt-2", closer)}";
    }

    private void OnAudioClip(AudioClip audioClip, int voiceActivityID)
    {
        Logging.LogStoryPromptResponse(storyPromptID, voiceActivityID, audioClip.length);
        if (audioClip.length > 1.5f)
        {
            promptRunner.SetCoroutine(AcknowledgementCoroutine());
            StartCoroutine(LogTranscripts(audioClip, voiceActivityID));
        }
    }

    private IEnumerator LogTranscripts(AudioClip audioClip, int voiceActivityID)
    {
        CoroutineResult<List<string>> transcriptionResult = new CoroutineResult<List<string>>();
        yield return speechReco.GetAudioTranscripts(audioClip, transcriptionResult);
        Destroy(audioClip);
        if (transcriptionResult.WasSuccessful()) { Logging.LogAudioTranscripts(voiceActivityID, transcriptionResult.GetResult()); }
    }
}
