using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpeechRecoButton : CategoryButton
{
    private SpeechRecoServerComm recoServerComm;
    private VoiceActivityDetector voiceActivityDetector;
    private WordsArea wordsArea;
    private Environment environment;
    private SynthesizerController synthesizer;
    private WordDrawer wordDrawer;
    private StageOrchestrator stageOrchestrator;
    private Tutorial tutorial;
    private bool isActive = false;
    private int currentSpeechID = -1;
    private int currentTapID = 0;
    private int timesNotRecognized = 0;

    private int lastAnnouncementType = 0;
    private int lastAllowedAnnouncementID = -1;
    private double lastSynthEndTime = -1000;

    private float UTTERANCE_INTERVAL = 2f;

    private const int ANNOUNCE_ISSUE = 1;
    private const int ANNOUNCE_SUCCESS = 2;

    private const int THRESHOLD_TIMES_NOT_RECOGNIZED = 2;

    private string[] INTROS = new string[]{ "Tell me which word shall we make after the bell!", "Tell me what shall we spell after the bell!", "Tell me what would you like to spell after the ding sound!", "Tell me which word would you like to make after the ding sound!" };
    private string[] DIDNT_GET = new string[] { "Sorry, I didn't get it...", "Sorry, I didn't understand...", "I'm afraid I didn't get it...", "I'm afraid I didn't understand..." };
    private string[] TRY_AGAIN = new string[] { "Try saying it again loudly and clearly!", "Try again with loud and clear voice!" };
    private string[] WORDS_AREA_FULL = new string[] { "If you want to tell me a new word, please tap the microphone button.", "If you want to make a different word, please press the microphone button." };
    private string[] DONT_KNOW = new string[] { "Hmm, maybe I don't know this word... Can you pick some other word instead?", "Hmm, not sure if I know this word... How about you choose another one?" };
    private string[] ON_ERROR = new string[] { "Looks like something happened with the Internet and I can't look up the words you said. Can you check with your parents that my tablet is connected?", "Looks like the Internet isn't working, so I can't look up the words you said. Can you ask your parents to connect this tablet to the Internet?" };
    private string[] ALREADY_THERE = new string[] { "Check the buttons below. Maybe the word you want is already there?", "I think the word you want may already be on some button below." };

    private Dictionary<string, int> batchOfWord = new Dictionary<string, int>();

    private void Update()
    {
        if (isActive && !DrawerIsInCorrectState())
        {
            Deactivate();
        }
    }

    protected override void DoOnStart()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        recoServerComm = stageObject.GetComponent<SpeechRecoServerComm>();
        environment = stageObject.GetComponent<Environment>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        voiceActivityDetector = stageObject.GetComponent<VoiceActivityDetector>();
        stageOrchestrator = stageObject.GetComponent<StageOrchestrator>();
        tutorial = stageObject.GetComponent<Tutorial>();
        wordsArea = GameObject.FindWithTag("WordBankWordsArea").GetComponent<WordsArea>();
        wordDrawer = GameObject.FindWithTag("WordDrawer").GetComponent<WordDrawer>();
    }

    protected override void DoOnTap()
    {
        if (0 == wordsArea.TargetWordCount() && currentSpeechID >= 0) { Debug.Log("IGNORING ROBOCALL TAP"); return; }
        Debug.Log("ACCEPTING ROBOCALL TAP");
        isActive = true;
        wordsArea.Clear();
        batchOfWord.Clear();
        ++currentTapID;
        timesNotRecognized = 0;
        environment.GetRoboPartner().LookAtChild();
        if (!tutorial.IsLessonCompleted("speech-reco"))
        {
            currentSpeechID = synthesizer.Speak(RandomUtil.PickOne("asr-btn-tap1", INTROS), cause: "speech-btn:intro", keepPauses: false);
        }
        voiceActivityDetector.StartDetection(callback: OnUserSpeech);
    }

    protected override void DoDeactivate()
    {
        isActive = false;
        voiceActivityDetector.StopDetection();
        if (currentSpeechID >= 0) {
            synthesizer.InterruptSpeech(currentSpeechID);
            currentSpeechID = -1;
        }
    }

    private bool DrawerIsInCorrectState()
    {
        return wordDrawer.IsDeployed() && wordDrawer.IsDisplayingWordBank();
    }

    private void OnUserSpeech(AudioClip audioClip, int speechSegmentID)
    {
        Debug.Log("User speech picked");
        if ("word_bank" != stageOrchestrator.GetStage()) return;
        StartCoroutine(RecognitionCoroutine(currentTapID, speechSegmentID, audioClip));
    }

    private IEnumerator RecognitionCoroutine(int tapID, int speechSegmentID, AudioClip audioClip)
    {
        environment.GetRoboPartner().LookAtTablet();
        CoroutineResult<List<string>> recoResult = new CoroutineResult<List<string>>();
        yield return recoServerComm.Recognize(audioClip, recoResult);
        Destroy(audioClip);
        double waitStartTime = TimeKeeper.time;
        if (!isActive || tapID != currentTapID) yield break;
        if (null != recoResult.GetResult())
        {
            timesNotRecognized = 0;
            if (wordsArea.TargetWordCount() < wordsArea.Capacity())
            {
                List<string> recoResultWordSenses = recoResult.GetResult();
                List<string> actuallyAdded = wordsArea.DeployAdditive(gameObject, recoResultWordSenses);
                if (0 != actuallyAdded.Count)
                {
                    int announcementID = RequestAnnouncement(ANNOUNCE_SUCCESS, tapID, waitStartTime);
                    if (announcementID < 0) yield break;
                    IEnumerator lesson = tutorial.GetPlugInLesson("speech-reco-results", new List<SynQuery>());
                    if (null != lesson)
                    {
                        yield return Announce(MakeSentenceUponRecognition(), announcementID, cause: "speech-btn:recognized");
                        yield return lesson;
                    }
                }
                else if (recoResultWordSenses.Any(wsense => DictUtil.GetOrDefault(batchOfWord, wsense, -1) != speechSegmentID))
                {
                    int announcementID = RequestAnnouncement(ANNOUNCE_SUCCESS, tapID, waitStartTime);
                    if (announcementID < 0) yield break;
                    yield return Announce(RandomUtil.PickOne("asr-btn-reco1", ALREADY_THERE), announcementID, cause: "speech-btn:already-there");
                }
                foreach (string addedWordSense in actuallyAdded)
                {
                    batchOfWord[addedWordSense] = speechSegmentID;
                }
            }
            else
            {
                int announcementID = RequestAnnouncement(ANNOUNCE_SUCCESS, tapID, waitStartTime);
                if (announcementID < 0) yield break;
                yield return Announce(RandomUtil.PickOne("asr-btn-reco2", WORDS_AREA_FULL), announcementID, cause: "speech-btn:word-area-full");
            }
        }
        else if (SpeechRecoServerComm.RECO_ERROR == recoResult.GetErrorCode())
        {
            int announcementID = RequestAnnouncement(ANNOUNCE_SUCCESS, tapID, waitStartTime);
            if (announcementID < 0) yield break;
            yield return Announce(RandomUtil.PickOne("asr-btn-reco3", ON_ERROR), announcementID, cause: "speech-btn:error");
        }
        else if (SpeechRecoServerComm.UNKNOWN_WORDS_PICKED == recoResult.GetErrorCode())
        {
            ++timesNotRecognized;
            yield return new WaitForSeconds(2.5f);
            int announcementID = RequestAnnouncement(ANNOUNCE_SUCCESS, tapID, waitStartTime);
            if (announcementID < 0) yield break;
            string prompt;
            if (timesNotRecognized < THRESHOLD_TIMES_NOT_RECOGNIZED)
            {
                prompt = $"{RandomUtil.PickOne("asr-btn-reco4", DIDNT_GET)} {RandomUtil.PickOne("asr-btn-reco5", TRY_AGAIN)}";
            }
            else
            {
                prompt = RandomUtil.PickOne("asr-btn-reco6", DONT_KNOW);
            }
            yield return Announce(prompt, announcementID, cause: "speech-btn:not-recognized");
        }
    }

    private IEnumerator Announce(string prompt, int announcementID, string cause)
    {
        while (voiceActivityDetector.IsPickingVoice()) yield return null;
        if (announcementID != lastAllowedAnnouncementID) yield break;
        yield return synthesizer.SpeechCoroutine(prompt, cause, out currentSpeechID, keepPauses: false);
        lastSynthEndTime = TimeKeeper.time;
    }

    private int RequestAnnouncement(int announcementType, int tapID, double waitStartTime)
    {
        bool allow = isActive
            && tapID == currentTapID
            && (announcementType > lastAnnouncementType || !(synthesizer.IsSpeaking() && TimeKeeper.time - lastSynthEndTime > UTTERANCE_INTERVAL));
        if (allow) {
            lastAnnouncementType = announcementType;
            ++lastAllowedAnnouncementID;
            return lastAllowedAnnouncementID;
        }
        else
        {
            return -1;
        }
    }

    private string MakeSentenceUponRecognition()
    {
        string[] IVE_GOT = { "I've got", "I've heard" };
        switch (RandomUtil.Range("asr-btn-rec-sent", 0, 2))
        {
            case 0:
                return $"This is what {RandomUtil.PickOne("asr-btn-rec-sent", IVE_GOT)}.";
            default:
                if (1 == wordsArea.TargetWordCount())
                {
                    return $"This is the word {RandomUtil.PickOne("asr-btn-rec-sent", IVE_GOT)}.";
                }
                else
                {
                    return $"These are the words {RandomUtil.PickOne("asr-btn-rec-sent", IVE_GOT)}.";
                }
        }
    }
}
