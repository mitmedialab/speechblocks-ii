using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SynthesizerController : MonoBehaviour
{
    public const int SPEECH_STATUS_OK = 0;
    public const int SPEECH_STATUS_NO_INTERNET = 1;
    public const int SPEECH_STATUS_FAIL = 2;

    private const float INTERRUPTION_TIMEOUT = 1f;

    private SpeechAccessDispatcher speechAccessDispatcher = null;
    private AudioSource audioSource = null;

    private double lastInterruptedTime = -10000;

    private Dictionary<int, CoroutineRunner> currentSpeechRunners = new Dictionary<int, CoroutineRunner>();
    private List<int> keyBuffer = new List<int>();

    private int speechIDCounter = 0;

    private int speechIDCurrentlyActive = -1;
    private HashSet<int> interruptedSpeechIDs = new HashSet<int>();
    private ISynthesizerModule currentlyActiveModule = null;

    private Action currentInterruptCallback = null;

    private ISynthesizerModule audioModule = null;
    private GoogleSynthesizer googleModule = null;
    private AcapelaSynthesizer acapelaModule = null;
    private AzureSynthesizer azureModule = null;
    private StageOrchestrator stageOrchestrator = null;
    private List<ISynthesizerModule> synthesisModules = new List<ISynthesizerModule>();
    private List<ReplayRecord> replayRecords = new List<ReplayRecord>();

    private bool isReplayMode = false;

    private const string CURRENT = "current";

    private Environment environment;

    public class ReplayRecord
    {
        public int id;
        public string text;
        public string cause;
        public double timestamp;
        public double end_timestamp;
    }

    void Start()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        environment = GetComponent<Environment>();
        audioSource = stageObject.transform.Find("synthesizer-audio-source").GetComponent<AudioSource>();
        speechAccessDispatcher = stageObject.GetComponent<SpeechAccessDispatcher>();
        stageOrchestrator = stageObject.GetComponent<StageOrchestrator>();
        azureModule = new AzureSynthesizer(audioSource);
        synthesisModules.Add(azureModule);
#if UNITY_ANDROID && !UNITY_EDITOR
        googleModule = new GoogleSynthesizer(OnGoogleInit);
        acapelaModule = new AcapelaSynthesizer(OnAcapelaInit);
#endif
        audioModule = new AudioModule(audioSource);
    }

    public int Speak(SynQuery synQuery, string cause, bool canInterrupt = true, bool keepPauses = true, string boundToStages = CURRENT)
    {
        currentSpeechRunners[speechIDCounter] = new CoroutineRunner(SpeechCoroutine(speechIDCounter,
                                                                                    synQuery,
                                                                                    cause,
                                                                                    initialSpeechAccess: true,
                                                                                    canInterrupt: canInterrupt,
                                                                                    keepPauses: keepPauses,
                                                                                    boundToStages: boundToStages));
        return speechIDCounter++;
    }

    public IEnumerator SpeechCoroutine(SynQuery query, string cause, bool canInterrupt = true, bool keepPauses = true, string boundToStages = CURRENT)
    {
        int speechID = Speak(query, cause, canInterrupt: canInterrupt, keepPauses: keepPauses, boundToStages: boundToStages);
        return AwaitSpeechEnd(speechID);
    }

    public IEnumerator SpeechCoroutine(SynQuery query, string cause, out int speechID, bool canInterrupt = true, bool keepPauses = true, string boundToStages = CURRENT)
    {
        int _speechID = Speak(query, cause, canInterrupt: canInterrupt, keepPauses: keepPauses, boundToStages: boundToStages);
        speechID = _speechID;
        return AwaitSpeechEnd(_speechID);
    }

    public IEnumerator SpeechCoroutine(SynQuery query, string cause, Action onInterrupt, bool keepPauses = true, string boundToStages = CURRENT)
    {
        int speechID = Speak(query, canInterrupt: true, keepPauses: keepPauses, cause: cause, boundToStages: boundToStages);
        yield return AwaitSpeechEnd(speechID);
        if (WasInterrupted(speechID)) { onInterrupt(); }
    }

    public bool IsSpeaking(int speechID)
    {
        return currentSpeechRunners.ContainsKey(speechID) && currentSpeechRunners[speechID].IsRunning();
    }

    public bool IsSpeaking()
    {
        return currentSpeechRunners.Values.Any(runner => runner.IsRunning());
    }

    public IEnumerator AwaitSpeechEnd(int speechID)
    {
        while (IsSpeaking(speechID)) yield return null;
        Debug.Log($"Speech end: {speechID}");
    }

    public bool IsActivelySpeaking()
    {
        if (isReplayMode) { return IsSpeaking(); }
        if (null != currentlyActiveModule) { return currentlyActiveModule.IsActivelySpeaking(); }
        return false;
    }

    public void InterruptSpeech(int speechID)
    {
        interruptedSpeechIDs.Add(speechID);
        Logging.LogSpeechInterrupted(speechID);
        currentSpeechRunners.Remove(speechID);
        if (speechIDCurrentlyActive == speechID)
        {
            lastInterruptedTime = TimeKeeper.time;
            EndCurrentSpeech();
        }
    }

    public bool WasInterrupted(int speechID)
    {
        return interruptedSpeechIDs.Contains(speechID);
    }

    public void SetReplayMode(bool replayMode)
    {
        isReplayMode = replayMode;
    }

    public void SetReplayRecords(List<ReplayRecord> replayRecords)
    {
        this.replayRecords.Clear();
        this.replayRecords.AddRange(replayRecords);
    }

    void Update()
    {
        // if (currentSpeechRunners.Count > 0) { Debug.Log($"SPEAKING {string.Join(",", currentSpeechRunners.Keys.Select(k => k.ToString()))}"); }
        azureModule.Update();
        UpdateRunners();
    }

    private void OnGoogleInit()
    {
        azureModule.EnableBackup();
        synthesisModules.Add(googleModule);
    }

    private void OnAcapelaInit()
    {
        azureModule.EnableBackup();
        if (synthesisModules.Count < 2)
        {
            synthesisModules.Add(acapelaModule);
        }
        else
        {
            synthesisModules.Insert(1, acapelaModule);
        }
    }

    private IEnumerator SpeechCoroutine(int speechID, SynQuery query, string cause, bool initialSpeechAccess, bool canInterrupt, bool keepPauses, string boundToStages)
    {
        string[] boundStages = GetBoundStages(boundToStages);
        string request = SynQuery.BuildSSML(query);
        ReplayRecord matchingRecord = null;
        if (isReplayMode) {
            matchingRecord = FindSuitableReplayRecord(text: request, cause: cause);
            if (null == matchingRecord) { Debug.Log("RECORD MISMATCH"); }
            else if (matchingRecord.text != request) { Debug.Log("RECORD TEXT MISMATCH"); }
            else { Debug.Log("RECORD MATCH"); }
        }
        Debug.Log("SPEAK " + request);
        Logging.LogSpeechRequest(speechID, request, cause);
        List<SynQuery> canonicSequence = SynQuery.ToCanonicSequence(query);
        Action interruptCallback = () => InterruptSpeech(speechID);
        double t0 = TimeKeeper.time;
        while (!speechAccessDispatcher.AccessSpeech(interruptCallback, canInterrupt, initialSpeechAccess))
        {
            Debug.Log("SPEECH CONFLICT");
            PerformStageCheck(speechID, boundStages);
            if (canInterrupt && TimeKeeper.time - t0 > 1f)
            {
                Debug.Log("SPEECH IGNORED");
                Logging.LogSpeechIgnored(speechID); yield break;
            }
            yield return null;
        }
        speechIDCurrentlyActive = speechID;
        currentInterruptCallback = interruptCallback;
        if (keepPauses)
        {
            while (TimeKeeper.time - lastInterruptedTime < INTERRUPTION_TIMEOUT && CheckReplayTime(matchingRecord))
            {
                PerformStageCheck(speechID, boundStages);
                yield return null;
            }
        }
        Logging.LogSpeechStarted(speechID);
        foreach (SynQuery seqItem in canonicSequence)
        {
            //Debug.Log("PLAYING SEGMENT " + text);
            if (seqItem.ContainsKey("audio"))
            {
                yield return PlayAudioSegment(seqItem, speechID, boundStages, matchingRecord);
            }
            else
            {
                yield return Synthesize(seqItem, keepPauses, speechID, boundStages, matchingRecord);
            }
        }
        if (null != matchingRecord)
        {
            while (CheckReplayTime(matchingRecord)) {
                PerformStageCheck(speechID, boundStages);
                yield return null;
            }
        }
        Logging.LogSpeechFinished(speechID);
        EndCurrentSpeech();
    }

    private string[] GetBoundStages(string boundToStages)
    {
        if (null == boundToStages) return null;
        if ("current" == boundToStages)
        {
            string currentStage = stageOrchestrator.GetStage();
            if (null == currentStage) return null;
            return new string[] { currentStage };
        }
        else
        {
            return boundToStages.Split('+');
        }
    }

    private void PerformStageCheck(int speechID, string[] boundStages)
    {
        if (null == boundStages) return;
        string stage = stageOrchestrator.GetStage();
        if (null == stage) return;
        if (!boundStages.Contains(stage)) {
            Debug.Log($"STAGE CHECK: {stage} not in [{string.Join(", ", boundStages)}]");
            InterruptSpeech(speechID);
        }
    }

    private void EndCurrentSpeech()
    {
        speechIDCurrentlyActive = -1;
        if (null != currentlyActiveModule) { currentlyActiveModule.Interrupt(); }
        currentlyActiveModule = null;
        speechAccessDispatcher.AccessToSpeechFinished(currentInterruptCallback);
        currentInterruptCallback = null;
    }

    private bool CheckReplayTime(ReplayRecord record)
    {
        if (null == record) return true;
        return TimeKeeper.time < record.end_timestamp;
    }

    private IEnumerator CheckReplayTimeCoroutine(ReplayRecord record)
    {
        while (CheckReplayTime(record)) yield return null;
    }

    private IEnumerator CheckStageCoroutine(int speechID, string[] boundStages)
    {
        while (true)
        {
            PerformStageCheck(speechID, boundStages);
            yield return null;
        }
    }

    private IEnumerator AddChecks(IEnumerator enumerator, int speechID, string[] boundStages, ReplayRecord record)
    {
        if (null == record && null == boundStages) return enumerator;
        List<IEnumerator> coroutines = new List<IEnumerator>();
        coroutines.Add(enumerator);
        if (null != record) { coroutines.Add(CheckReplayTimeCoroutine(record)); }
        if (null != boundStages) { coroutines.Add(CheckStageCoroutine(speechID, boundStages)); }
        return CoroutineUtils.RunUntilAnyStop(coroutines);
    }

    private IEnumerator Synthesize(SynQuery synQuery, bool keepPauses, int speechID, string[] boundStages, ReplayRecord replayRecord)
    {
        synQuery.AssignParameter("keep_pauses", keepPauses);
        bool internetIssue = false;
        for (int i = 0; i < synthesisModules.Count; ++i)
        {
            ISynthesizerModule synthesisModule = synthesisModules[i];
            currentlyActiveModule = synthesisModule;
            yield return AddChecks(synthesisModule.Speak(synQuery), speechID, boundStages, replayRecord);
            currentlyActiveModule = null;
            int speechStatus = synthesisModule.LastSpeechStatus();
            if (SPEECH_STATUS_OK == speechStatus) yield break;
            if (SPEECH_STATUS_NO_INTERNET == speechStatus) internetIssue = true;
        }
        if (internetIssue)
        {
            environment.AnnounceInternetIssue(fatal: false);
            while (environment.IssueAnnouncementInProgress()) yield return null;
        }
    }

    private IEnumerator PlayAudioSegment(SynQuery audioQuery, int speechID, string[] boundStages, ReplayRecord replayRecord)
    {
        currentlyActiveModule = audioModule;
        yield return AddChecks(audioModule.Speak(audioQuery), speechID, boundStages, replayRecord);
        currentlyActiveModule = null;
    }

    private void UpdateRunners()
    {
        keyBuffer.Clear();
        keyBuffer.AddRange(currentSpeechRunners.Keys);
        foreach (int key in keyBuffer)
        {
            CoroutineRunner runner = currentSpeechRunners[key];
            //Debug.Log($"UPDATING RUNNER {key}");
            runner.Update();
            if (!runner.IsRunning())
            {
                //Debug.Log($"STOPPING RUNNER {key}");
                currentSpeechRunners.Remove(key);
            }
        }
    }

    private ReplayRecord FindSuitableReplayRecord(string text, string cause)
    {
        cause = Replayer.RemoveUUIDs(cause);
        List<ReplayRecord> candidateRecords = replayRecords.Where(record => record.cause == cause && Math.Abs(record.timestamp - TimeKeeper.time) < 2).ToList();
        if (0 == candidateRecords.Count) return null;
        if (1 == candidateRecords.Count) return candidateRecords[0];
        List<ReplayRecord> textMatchRecords = candidateRecords.Where(record => record.text == text).ToList();
        if (1 == textMatchRecords.Count) return textMatchRecords[0];
        if (textMatchRecords.Count > 1) { candidateRecords = textMatchRecords; }
        return LinqUtil.MinBy(candidateRecords, record => Math.Abs(record.timestamp - TimeKeeper.time), null);
    }
}
