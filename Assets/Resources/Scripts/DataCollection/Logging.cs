using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Linq;
using System.Security.Cryptography;
using SimpleJSON;

public class Logging : MonoBehaviour
{
    [SerializeField]
    private string logType = "aux";
    [SerializeField]
    private string logID = null;

    private Vector3 lastRecordedPosition;
    private Vector3 lastRecordedScale;
    private float lastRecordedRotation;

    public const string DEFAULT_CAUSE = "auto";
    private static StreamWriter logstream = null;
    private static double time;
    private static double timeZero;
    private static Dictionary<int, string> touchIDs = new Dictionary<int, string>();
    private static Dictionary<int, Vector2> lastRecordedTouchPos = new Dictionary<int, Vector2>();

    private static float MOVEMENT_THRESHOLD = 0.01f;
    private static float SCALE_THRESHOLD = 0.01f;
    private static float ANGLE_THRESHOLD = 0.01f;

    private void Start()
    {
        if (!HasAssignedID()) {
            LogBirth(gameObject, DEFAULT_CAUSE);
        }
    }

    public bool HasAssignedID() {
        return null != logID && logID.StartsWith(logType);
    }

    public void Setup(string logType)
    {
        this.logType = logType;
    }

    public string GetLogID() {
        return logID;
    }

    public void LoadLogID(string logID)
    {
        this.logID = logID;
    }

    private void OnDestroy() {
        if (HasAssignedID()) {
            LogDeath(gameObject, DEFAULT_CAUSE);
        }
    }

    public static void StartNewLog( List<string> tutorialLessons,
                                    Dictionary<string, string> sharedAvatarVersions,
                                    Dictionary<string, string> privateAvatarVersions,
                                    Dictionary<string, string> sceneVersions)
    {
        if (null != logstream) { FinalizeLog(); }
        time = TimeKeeper.time;
        timeZero = TimeKeeper.time;
        touchIDs.Clear();
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        Environment environment = stageObject.GetComponent<Environment>();
        logstream = new StreamWriter(GetTemporaryLogPath(environment));
        AddEntry("log", "sb-ver", Application.version);
        AddEntry("config", "station-name", environment.GetStationName(), "station-type", environment.GetStationType(), "video-enabled", environment.IsVideoEnabled());
        User user = environment.GetUser();
        AddEntry("kid", "group", environment.GetGroup(), "id", user.GetID(), "video-consent", user.IsConsentedToVideo(), "child-driven", user.InChildDrivenCondition(), "expressive", user.InExpressiveCondition());
        float screenHUnit = 2 * Camera.main.orthographicSize;
        float screenWUnit = Camera.main.aspect * screenHUnit;
        AddEntry("screensize", "wpix", Screen.width, "hpix", Screen.height, "wunit", screenWUnit, "hunit", screenHUnit);
        AddEntry("blocksize", "val", Block.GetStandardHeight());
        AddEntry("starttime", "val", environment.GetLoginTime().ToString("yyyy-MM-dd-HH-mm-ss-FFF"), "hex", DoubleToHex(TimeKeeper.time));
        AddEntry("tutorial", "val", tutorialLessons.ToArray());
        AddEntry("shared-avatars", "val", VersionDictRecord(sharedAvatarVersions));
        AddEntry("private-avatars", "val", VersionDictRecord(privateAvatarVersions));
        AddEntry("scenes", "val", VersionDictRecord(sceneVersions));
        Logging[] loggablesAlive = GameObject.FindObjectsOfType<Logging>();
        foreach (Logging loggable in loggablesAlive) {
            loggable.logID = null;
        }
        foreach (Logging loggable in loggablesAlive) {
            LogBirth(loggable.gameObject, DEFAULT_CAUSE);
        }
    }

    public static void OnLostFocus()
    {
        if (null == logstream) return;
        Log("lost-focus", "time", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-FFF"));
        logstream.Flush();
        //logstream.Close();
        //logstream = null;
    }

    public static void OnRegainedFocus()
    {
        //GameObject stageObject = GameObject.FindWithTag("StageObject");
        //Environment environment = stageObject.GetComponent<Environment>();
        //logstream = new StreamWriter(GetTemporaryLogPath(environment), append: true);
        Log("regained-focus", "time", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-FFF"));
    }

    public static void LogError(string condition, string stackTrace)
    {
        stackTrace = stackTrace.Replace("\n", ";");
        Log("error", "condition", condition, "stacktrace", stackTrace);
    }

    public static void LogRandomRangeFloat(string id, float low, float high, float rand)
    {
        Log("rf", "id", id, "l", FloatToHex(low), "h", FloatToHex(high),  "v", FloatToHex(rand));
    }

    public static void LogRandomRangeInt(string id, int low, int high, int rand)
    {
        Log("ri", "id", id, "l", low, "h", high, "v", rand);
    }

    public static void FinalizeLog()
    {
        if (null == logstream) return;
        AddEntry("endtime", "val", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-FFF"));
        Debug.Log("FINALIZING LOG");
        logstream.Flush();
        logstream.Close();
        logstream = null;
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        Environment environment = stageObject.GetComponent<Environment>();
        stageObject.GetComponent<UploadManager>().ScheduleUpload(sourcePath: GetTemporaryLogPath(environment), targetPath: GetDestinationLogPath(environment), deleteWhenDone: true);
    }

    private static string GetTemporaryLogPath(Environment environment)
    {
        string path = $"{Application.persistentDataPath}/Logs/{environment.GetGroup()}/{environment.GetUser().GetID()}";
        Directory.CreateDirectory(path);
        return $"{path}/{environment.GetLoginTime().ToString("yyyy-MM-dd-HH-mm-ss-FFF")}.txt";
    }

    private static string GetDestinationLogPath(Environment environment)
    {
        return $"Logs/{environment.GetGroup()}/{environment.GetUser().GetID()}/{environment.GetLoginTime().ToString("yyyy-MM-dd-HH-mm-ss-FFF")}.txt";
    }


    public static string GetObjectLogID(GameObject gObj)
    {
        if (null == gObj) return "??";
        Logging loggable = gObj.GetComponent<Logging>();
        if (null == loggable) return "??";
        if (!loggable.HasAssignedID()) { LogBirth(gObj, DEFAULT_CAUSE); }
        return loggable.logID;
    }

    public static void LogMovement(GameObject gameObject, string cause) {
        if (null == logstream) return;
        Logging loggable = gameObject.GetComponent<Logging>();
        if (null == loggable) return;
        Vector3 localPosition = loggable.transform.localPosition;
        if (Vector3.Distance(loggable.lastRecordedPosition, localPosition) > MOVEMENT_THRESHOLD)
        {
            Log("mov", "id", loggable.logID, "bc", cause, "x", localPosition.x, "y", localPosition.y, "z", localPosition.z);
            loggable.lastRecordedPosition = localPosition;
        }
    }

    public static void LogRotation(GameObject gameObject, string cause) {
        if (null == logstream) return;
        Logging loggable = gameObject.GetComponent<Logging>();
        if (null == loggable) return;
        if (!loggable.HasAssignedID()) { LogBirth(loggable.gameObject, DEFAULT_CAUSE); }
        float angle = gameObject.transform.eulerAngles.z;
        if (Mathf.Abs(loggable.lastRecordedRotation - angle) > ANGLE_THRESHOLD) {
            Log("rev", "id", loggable.logID, "bc", cause, "a", angle);
            loggable.lastRecordedRotation = angle;
        }
    }

    public static void LogScale(GameObject gameObject, string cause)
    {
        if (null == logstream) return;
        Logging loggable = gameObject.GetComponent<Logging>();
        if (null == loggable) return;
        if (!loggable.HasAssignedID()) { LogBirth(loggable.gameObject, DEFAULT_CAUSE); }
        Vector3 localScale = gameObject.transform.localScale;
        if (Vector3.Distance(loggable.lastRecordedScale, localScale) > SCALE_THRESHOLD) {
            Log("scl", "id", loggable.logID, "bc", cause, "x", localScale.x, "y", localScale.y, "z", localScale.z);
            loggable.lastRecordedScale = localScale;
        }
    }

    public static void LogOpacity(GameObject gameObject, float opacity, string cause)
    {
        if (null == logstream) return;
        Logging loggable = gameObject.GetComponent<Logging>();
        if (null == loggable) return;
        if (!loggable.HasAssignedID()) { LogBirth(loggable.gameObject, DEFAULT_CAUSE); }
        Log("opq", "id", loggable.logID, "bc", cause, "o", opacity);
    }

    public static void LogLayer(GameObject gameObject, string sortingLayer, string cause)
    {
        if (null == logstream) return;
        Logging loggable = gameObject.GetComponent<Logging>();
        if (null == loggable) return;
        if (!loggable.HasAssignedID()) { LogBirth(loggable.gameObject, DEFAULT_CAUSE); }
        Log("layer", "id", loggable.logID, "bc", cause, "l", sortingLayer);
    }

    public static void LogSortOrder(GameObject gameObject, int sortingOrder, string cause)
    {
        if (null == logstream) return;
        Logging loggable = gameObject.GetComponent<Logging>();
        if (null == loggable) return;
        if (!loggable.HasAssignedID()) { LogBirth(loggable.gameObject, DEFAULT_CAUSE); }
        Log("s-order", "id", loggable.logID, "bc", cause, "o", sortingOrder);
    }

    public static void LogBirth(GameObject gameObject, string cause)
    {
        Logging loggable = gameObject.GetComponent<Logging>();
        if (null == loggable || loggable.HasAssignedID()) return;
        AssignLogID(loggable);
        if (null == logstream) return;
        LogBirthWithAssignedID(loggable, cause);
    }

    public static void LogBirthWithAssignedID(Logging loggable, string cause) {
        GameObject gameObject = loggable.gameObject;
        string parentId = ObtainParentLogID(gameObject);
        Vector3 localPosition = gameObject.transform.localPosition;
        Vector3 localScale = gameObject.transform.localScale;
        float rotation = gameObject.transform.eulerAngles.z;
        loggable.lastRecordedPosition = localPosition;
        loggable.lastRecordedScale = localScale;
        loggable.lastRecordedRotation = rotation;
        List<object[]> paramParts = new List<object[]>();
        float opacity = Opacity.GetOpacity(gameObject);
        object[] logParams = new object[]
            {"new", "id", loggable.logID,
            "parent", parentId,
            "bc", cause,
            "x", localPosition.x, "y", localPosition.y, "z", localPosition.z,
            "a", rotation,
            "sclx", localScale.x, "scly", localScale.y, "sclz", localScale.z};
        paramParts.Add(logParams);
        string sortingLayer = ZSorting.GetSortingLayer(gameObject);
        int sortingOrder = ZSorting.GetSortingOrder(gameObject);
        if (null != sortingLayer)
        {
            paramParts.Add(new object[] { "layer", sortingLayer, "s-order", sortingOrder });
        }
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (null != spriteRenderer)
        {
            Sprite sprite = spriteRenderer.sprite;
            if (null != sprite)
            {
                Color color = spriteRenderer.color;
                object[] spriteRendererParams = new object[]
                    { "sprname", sprite.name, "ppu", sprite.pixelsPerUnit,
                    "pivx", sprite.pivot.x, "pivy", sprite.pivot.y,
                    "szx", spriteRenderer.size.x, "szy", spriteRenderer.size.y,
                    "r", color.r, "g", color.g, "b", color.b};
                paramParts.Add(spriteRendererParams);
            }
        }
        IDetailedLogging[] detailedLoggingItems = gameObject.GetComponents<IDetailedLogging>();
        foreach (IDetailedLogging detailedLogItem in detailedLoggingItems)
        {
            paramParts.Add(detailedLogItem.GetLogDetails());
        }
        object[] allParams = paramParts.SelectMany(i => i).ToArray();
        Log(allParams);
    }

    public static void LogDeath(GameObject gameObject, string cause) {
        if (null == logstream) return;
        Logging loggable = gameObject.GetComponent<Logging>();
        if (null == loggable || !loggable.HasAssignedID()) return;
        Log("del", "id", loggable.logID, "bc", cause);
        loggable.logID = null;
    }

    public static void LogParent(GameObject gameObject, string cause) {
        if (null == logstream) return;
        Logging loggable = gameObject.GetComponent<Logging>();
        if (null == loggable) return;
        if (!loggable.HasAssignedID()) { LogBirth(loggable.gameObject, DEFAULT_CAUSE); }
        string parentId = ObtainParentLogID(gameObject);
        Log("atch", "id", loggable.logID, "parent", parentId, "bc", cause);
        LogMovement(gameObject, "reroot");
    }

    public static void LogActive(GameObject gameObject, string cause) {
        if (null == logstream) return;
        Logging loggable = gameObject.GetComponent<Logging>();
        if (null == loggable) return;
        if (!loggable.HasAssignedID()) { 
            LogBirth(gameObject, "actv-" + cause);
            if (gameObject.activeSelf) return;
        }
        Log("actv", "id", loggable.logID, "val", gameObject.activeSelf, "bc", cause);
    }

    public static void LogTouchDown(int fingerID, Vector2 touchPos) {
        if (null == logstream) return;
        string touchID = SpawnRandomID();
        touchIDs[fingerID] = touchID;
        lastRecordedTouchPos[fingerID] = touchPos;
        Log("tchdn", "id", touchID, "fr", fingerID, "x", touchPos.x, "y", touchPos.y);
    }

    public static void LogListenerTouchDown(int fingerID, GameObject listener)
    {
        if (null == logstream) return;
        Logging loggable = listener.GetComponent<Logging>();
        if (null == loggable) return;
        if (!loggable.HasAssignedID()) { LogBirth(loggable.gameObject, DEFAULT_CAUSE); }
        Log("listener-tchdn", "tchid", GetTouchID(fingerID), "id", loggable.logID);
    }

    public static void LogTouchMove(int fingerID, Vector2 touchPos) {
        if (null == logstream) return;
        if (!touchIDs.ContainsKey(fingerID)) return;
        if (Vector2.Distance(lastRecordedTouchPos[fingerID], touchPos) > MOVEMENT_THRESHOLD) {
            Log("tchmv", "id", touchIDs[fingerID], "x", touchPos.x, "y", touchPos.y);
            lastRecordedTouchPos[fingerID] = touchPos;
        }
    }

    public static void LogListenerTouchMove(int fingerID, GameObject listener)
    {
        if (null == logstream) return;
        Logging loggable = listener.GetComponent<Logging>();
        if (null == loggable) return;
        if (!loggable.HasAssignedID()) { LogBirth(loggable.gameObject, DEFAULT_CAUSE); }
        Log("listener-tchmv", "tchid", GetTouchID(fingerID), "id", loggable.logID);
    }

    public static void LogTouchUp(int fingerID, bool forced) {
        if (null == logstream) return;
        if (!touchIDs.ContainsKey(fingerID)) return;
        Log("tchup", "id", touchIDs[fingerID], "forced", forced);
    }

    public static void LogListenerTouchUp(int fingerID, GameObject listener, bool forced)
    {
        if (null == logstream) return;
        Logging loggable = listener.GetComponent<Logging>();
        if (null == loggable) return;
        if (!loggable.HasAssignedID()) { LogBirth(loggable.gameObject, DEFAULT_CAUSE); }
        Log("listener-tchup", "tchid", GetTouchID(fingerID), "id", loggable.logID, "forced", forced);
    }

    public static void LogTap(int fingerID, GameObject gameObject, bool delayed) {
        if (null == logstream) return;
        Logging loggable = gameObject.GetComponent<Logging>();
        if (null == loggable) return;
        if (!loggable.HasAssignedID()) { LogBirth(loggable.gameObject, DEFAULT_CAUSE); }
        Log("tap", "tchid", GetTouchID(fingerID), "id", loggable.logID, "delayed", delayed);
    }

    public static void LogDragIn(int fingerID, GameObject dragged, GameObject dragInArea)
    {
        if (null == logstream) return;
        Logging dragInAreaLoggable = dragInArea.GetComponent<Logging>();
        Logging draggedLoggable = dragged.GetComponent<Logging>();
        if (null == draggedLoggable || null == dragInAreaLoggable) return;
        if (!dragInAreaLoggable.HasAssignedID()) { LogBirth(dragInAreaLoggable.gameObject, DEFAULT_CAUSE); }
        if (!draggedLoggable.HasAssignedID()) { LogBirth(draggedLoggable.gameObject, DEFAULT_CAUSE); }
        Log("drag-in", "tchid", GetTouchID(fingerID), "obj", draggedLoggable.logID, "drag-in-area", dragInAreaLoggable.logID);
    }

    public static void LogDrop(int fingerID, GameObject dropped, GameObject dropArea)
    {
        if (null == logstream) return;
        Logging dropAreaLoggable = dropArea.GetComponent<Logging>();
        Logging droppedLoggable = dropped.GetComponent<Logging>();
        if (null == dropAreaLoggable || null == droppedLoggable) return;
        if (!dropAreaLoggable.HasAssignedID()) { LogBirth(dropAreaLoggable.gameObject, DEFAULT_CAUSE); }
        if (!droppedLoggable.HasAssignedID()) { LogBirth(droppedLoggable.gameObject, DEFAULT_CAUSE); }
        Log("drop", "tchid", GetTouchID(fingerID), "obj", droppedLoggable.logID, "droparea", dropAreaLoggable.logID);
    }


    public static void LogSpeechRequest(int speechID, string target, string cause) {
        Log("speech-req", "str", target, "bc", cause, "id", speechID);
    }

    public static void LogSpeechIgnored(int speechID)
    {
        Log("speech-ign", "id", speechID);
    }

    public static void LogSpeechStarted(int speechID)
    {
        Log("speech-start", "id", speechID);
    }

    public static void LogSpeechFinished(int speechID)
    {
        Log("speech-end", "id", speechID);
    }

    public static void LogSpeechInterrupted(int speechID)
    {
        Log("speech-interr", "id", speechID);
    }

    public static void LogBlockPlayAudio(GameObject block, string cause)
    {
        Log("blk-play-audio", "id", GetObjectLogID(block), "cause", cause);
    }

    public static void LogPointAt(GameObject gameObject, string cause)
    {
        string logID = GetObjectLogID(gameObject);
        if (null == logID) return;
        Log("point-at", "id", logID, "bc", cause);
    }

    public static void LogPointAway(GameObject gameObject, string cause)
    {
        string logID = GetObjectLogID(gameObject);
        if (null == logID) return;
        Log("point-away", "id", logID, "bc", cause);
    }

    public static void LogLessonStart(string lessonID)
    {
        Log("tutorial-start", "id", lessonID);
    }

    public static void LogLessonEnd(string lessonID)
    {
        Log("tutorial-end", "id", lessonID);
    }

    public static void LogLessonInviteStart(string lessonID)
    {
        Log("tutorial-invite-start", "id", lessonID);
    }

    public static void LogLessonInviteEnd(string lessonID)
    {
        Log("tutorial-invite-end", "id", lessonID);
    }

    public static void LogInterrupt(string eventId) {
        Log("interr", "ev-id", eventId);
    }

    public static void LogScaffoldingTarget(int scaffolderTargetID, PGMapping target, string cause) {
        Log("scaf-set", "targ-id", scaffolderTargetID, "word", target.compositeWord, "mapping", PGPair.Transcription(target.pgs), "bc", cause);
    }

    public static void LogScaffoldingSyllabification(int scaffolderTargetID, PGMapping mapping, int[] syllabification)
    {
        Log("scaf-syll", "targ-id", scaffolderTargetID, "syll", syllabification, "syll-break", Syllabifier.Breakdown(mapping.pgs, syllabification));
    }

    public static void LogScaffoldingInteraction(int scaffolderTargetID, int scaffoldingInteractionID, string cause)
    {
        Log("scaf-int", "targ-id", scaffolderTargetID, "int-id", scaffoldingInteractionID, "bc", cause);
    }

    public static void LogScaffoldingPromptLevel(string level, int scaffolderTargetID, int scaffoldingInteractionID, int targetPGSlot, bool giveQuestion)
    {
        Log("scaf-level", "level", level, "targ-id", scaffolderTargetID, "int-id", scaffoldingInteractionID, "pg-slot", targetPGSlot, "question", giveQuestion);
    }

    public static void LogScaffoldingBlockAccept(int scaffolderTargetID, GameObject block, int blockPosition)
    {
        Log("scaf-accept", "targ-id", scaffolderTargetID, "blk", GetObjectLogID(block), "gr", block.GetComponent<Block>().GetGrapheme(), "pos", blockPosition);
    }

    public static void LogScaffoldingBlockReject(int scaffolderTargetID, GameObject block)
    {
        Log("scaf-reject", "targ-id", scaffolderTargetID, "blk", GetObjectLogID(block), "gr", block.GetComponent<Block>().GetGrapheme());
    }

    public static void LogScaffoldingMorph(int scaffolderTargetID, Block block, int blockPosition, string newGr)
    {
        Log("scaf-morph", "targ-id", scaffolderTargetID, "blk", GetObjectLogID(block.gameObject), "pos", blockPosition, "ph", block.GetPhonemeCode(), "old-gr", block.GetGrapheme(), "new-gr", newGr);
    }

    public static void LogScaffoldingAutoDrag(int scaffolderTargetID, string phonemecode, string grapheme, int landingPlace, string cause)
    {
        Log("scaf-auto-drag", "targ-id", scaffolderTargetID, "ph", phonemecode, "gr", grapheme, "land", landingPlace, "bc", cause);
    }

    public static void LogScaffoldingCancel(int scaffolderTargetID) {
        Log("scaf-cancel", "targ-id", scaffolderTargetID);
    }

    public static void LogScaffoldingComplete(int scaffolderTargetID)
    {
        Log("scaf-done", "targ-id", scaffolderTargetID);
    }

    public static void LogWordBoxUpdate(List<Block> boxBlocks, string cause) {
        object[] blockIDs = boxBlocks.Select(block => block.GetComponent<Logging>().logID).ToArray();
        //string transcription = PGPair.Transcription(boxBlocks.Select(block => block.GetPGPair()).ToList());
        Log("wbox-upd", "blks", blockIDs, "bc", cause);
    }

    public static void LogWordBoxTransition(List<Block> srcBlocks, List<Block> refBlocks, List<Block> srcRefBlocks, string cause) {
        object[] srcBlockIds = srcBlocks.Select(block => block.GetComponent<Logging>().logID).ToArray();
        object[] refBlockIds = refBlocks.Where(block => null != block).Select(block => block.GetComponent<Logging>().logID).ToArray();
        object[] srcRefBlockIds = srcRefBlocks.Select(block => block.GetComponent<Logging>().logID).ToArray();
        Log("wbox-trans", "src", srcBlockIds, "ref", refBlockIds, "src-ref", srcRefBlockIds, "bc", cause);
    }

    public static void LogModeChange(bool letterMode) {
        Log("mode-chg", "mode", letterMode ? "letr" : "ph");
    }

    public static void LogWordBoxSettled(bool letterMode, List<Block> boxBlocks) {
        string transcription = PGPair.Transcription(boxBlocks.Select(block => block.GetPGPair()).ToList());
        Log("wbox-setl", "mode", letterMode ? "letr" : "ph", "trn", transcription);
    }

    public static void LogPGChange(GameObject gameObject, string cause) {
        BlockBase blockBase = gameObject.GetComponent<BlockBase>();
        if (null == blockBase) return;
        Logging logging = gameObject.GetComponent<Logging>();
        if (null == logging) return;
        object[] logDetails = blockBase.GetLogDetails();
        object[] logArgs = (new object[] { "pg-chg", "id", logging.logID, "bc", cause }).Concat(logDetails).ToArray();
        Log(logArgs);
    }

    public static void LogVoice(bool isOn)
    {
        Log("voice", "on", isOn);
    }

    public static void LogSpeechSegmentFormed(int segmentNumber, bool interim)
    {
        Log("speech-segment-formed", "segnum", segmentNumber, "interim", interim);
    }

    public static void LogSpeechRecoStart(int speechRecoID)
    {
        Log("speech-reco-start", "id", speechRecoID);
    }

    public static void LogSpeechRecoInterrupt(int speechRecoID)
    {
        Log("speech-reco-interrupt", "id", speechRecoID);
    }

    public static void LogSpeechRecoResult(int speechRecoID, string serverResponse)
    {
        Log("speech-reco-results", "id", speechRecoID, "response", serverResponse);
    }

    public static void LogStoryPromptResponse(int promptID, int audioID, float length)
    {
        Log("story-prompt-response", "id", promptID, "audio-id", audioID, "len", length);
    }

    public static void LogAudioTranscripts(int audioID, List<string> transcripts)
    {
        Log("audio-transcripts", "audio-id", audioID, "transcripts", string.Join(" | ", transcripts));
    }

    public static void LogStageChange(string stage)
    {
        Log("stage-change", "stage", stage);
    }

    public static void LogChoiceButtonAssignment(GameObject choiceButton, string assignment)
    {
        Log("choice-btn-assign", "id", GetObjectLogID(choiceButton), "choice", assignment);
    }

    public static string GetTouchID(int fingerID) {
        if (!touchIDs.ContainsKey(fingerID)) return "??";
        return touchIDs[fingerID];
    }

    private static void AssignLogID(Logging loggable) {
        loggable.logID = $"{loggable.logType}:{SpawnRandomID()}";
    }

    public static string ObtainParentLogID(GameObject gameObject) {
        Transform parentTransform = gameObject.transform.parent;
        if (null == parentTransform) return null;
        GameObject parent = parentTransform.gameObject;
        Logging logging = parent.GetComponent<Logging>();
        if (null == logging) {
            logging = parent.AddComponent<Logging>();
        }
        if (!logging.HasAssignedID()) {
            LogBirth(logging.gameObject, DEFAULT_CAUSE);
        }
        return logging.logID;
    }

    //public static void 

    //public void LogLocalPos(string objectName, Vector3 localPosition) {
    //    Log("t", "pos", "nm", objectName, "x", localPosition.x.ToString("0.0000"), "y", localPosition.y.ToString("0.0000"), "z", localPosition.z.ToString("0.0000"));
    //}

    //public void LogRotation(string objectName, float rotation) {
    //    Log("t", "rot", "nm", objectName, "rz", rotation.ToString("0.0000"));
    //}

    //public void LogScale(string objectName, Vector3 scale) {
    //    Log("t", "scl", "nm", objectName, "x", scale.x.ToString("0.0000"), "y", scale.y.ToString("0.0000"), "z", scale.z.ToString("0.0000"));
    //}

    //public void LogOpacity(string objectName, float opacity) {
    //    Log("t", "opq", "nm", objectName, "opq", opacity.ToString("0.0000"));
    //}

    public static void Log(params object[] elements)
    {
        if (null == logstream) return;
        try
        {
            CheckTimestamp();
            AddEntry(elements);
        }
        catch
        { }
    }

    private static string AccessParentId(Logging loggable)
    {
        return null;
        //Loggable loggable = new 
        //return null;
    }

    private static void CheckTimestamp()
    {
        if (time != TimeKeeper.time) {
            time = TimeKeeper.time;
            AddEntry("time", "val", time - timeZero, "hex", DoubleToHex(time));
        }
    }

    private static string DoubleToHex(double dbl)
    {
        byte[] dblBytes = BitConverter.GetBytes(dbl);
        string dblHex = BitConverter.ToString(dblBytes).Replace("-", "");
        return dblHex;
    }

    private static string FloatToHex(float flt)
    {
        byte[] fltBytes = BitConverter.GetBytes(flt);
        string fltHex = BitConverter.ToString(fltBytes).Replace("-", "");
        return fltHex;
    }

    private static void AddEntry(params object[] elements)
    {
        if (null == logstream) return;
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("{");
        AddValueToEntry(stringBuilder, "h");
        stringBuilder.Append(": ");
        AddValueToEntry(stringBuilder, elements[0]);
        for (int i = 1; i < elements.Length; i += 2)
        {
            stringBuilder.Append(", ");
            AddValueToEntry(stringBuilder, (string)elements[i]);
            stringBuilder.Append(": ");
            AddValueToEntry(stringBuilder, elements[i + 1]);
        }
        stringBuilder.Append("}");
        logstream.WriteLine(stringBuilder.ToString());
        logstream.Flush();
    }

    private static void AddValueToEntry(StringBuilder stringBuilder, object value)
    {
        if (null == value) {
            stringBuilder.Append("null");
        } else if (value is float)
        {
            stringBuilder.Append(((float)value).ToString("0.####"));
        }
        else if (value is int)
        {
            stringBuilder.Append(value.ToString());
        }
        else if (value is bool)
        {
            stringBuilder.Append((bool)value ? "true" : "false");
        }
        else if (value is object[])
        {
            object[] list = (object[])value;
            stringBuilder.Append("[");
            for (int i = 0; i < list.Length; ++i)
            {
                if (0 != i)
                {
                    stringBuilder.Append(", ");
                }
                AddValueToEntry(stringBuilder, list[i]);
            }
            stringBuilder.Append("]");
        }
        else if (value is string)
        {
            string escaped = (new JSONString((string)value)).ToString();
            escaped = escaped.Substring(1, escaped.Length - 2);
            stringBuilder.Append("\"");
            stringBuilder.Append(escaped);
            stringBuilder.Append("\"");
        }
        else
        {
            stringBuilder.Append("\"");
            stringBuilder.Append(value.ToString());
            stringBuilder.Append("\"");
        }
    }

    private static string VersionDictRecord(Dictionary<string, string> versionDict)
    {
        return string.Join(",", versionDict.Keys.Select(key => $"{key}:{versionDict[key]}"));
    }

    private static GameObject FindLoggableParent(Transform start)
    {
        if (null == start) return null;
        if (null != start.GetComponent<Logging>())
        {
            return start.gameObject;
        }
        return FindLoggableParent(start.parent);
    }

    private static string SpawnRandomID() {
        return System.Guid.NewGuid().ToString();
    }
}