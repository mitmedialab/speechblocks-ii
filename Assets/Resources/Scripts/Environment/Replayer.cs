using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SimpleJSON;

public class Replayer : MonoBehaviour
{
    private string replayPath = null;
    private double nextTimestamp = 0.0;
    private IEnumerator<JSONNode> fileLines = null;
    private Environment environment;
    private TouchManager touchManager;
    private SynthesizerController synthesizerController;
    private VoiceActivityDetector voiceActivityDetector;
    private SpeechRecoServerComm speechRecoServerComm;
    private Dictionary<string, int> fingerByTouch = new Dictionary<string, int>();
    private bool loaded = false;
    private Dictionary<int, GameObject> simTouches = new Dictionary<int, GameObject>();
    private GameObject simTouchPrefab;
    private int speechSegmentCounter = -1;

    public void Setup(string replayPath)
    {
        this.replayPath = replayPath;
    }

    // Start is called before the first frame update
    public IEnumerator InitReplay()
    {
        simTouchPrefab = Resources.Load<GameObject>("Prefabs/finger-sim");
        environment = GetComponent<Environment>();
        touchManager = GetComponent<TouchManager>();
        synthesizerController = GetComponent<SynthesizerController>();
        voiceActivityDetector = GetComponent<VoiceActivityDetector>();
        speechRecoServerComm = GetComponent<SpeechRecoServerComm>();
        touchManager.SetReplayMode(true);
        synthesizerController.SetReplayMode(true);
        speechRecoServerComm.SetReplayMode(true);
        voiceActivityDetector.SetReplayMode(true);
        LoadRandomTracks(replayPath);
        LoadSynthReplayRecords(replayPath);
        fileLines = LogEntries(replayPath);
        yield return ProcessHeader();
        ProcessStep();
        loaded = true;
    }

    void LateUpdate()
    {
        if (!loaded) return;
        if (TimeKeeper.UpdateSimulatedTime(nextTimestamp))
        {
            ProcessStep();
        }
    }

    public static bool IsUUID(string code)
    {
        if (36 != code.Length) return false;
        for (int i = 0; i < code.Length; ++i)
        {
            if (8 == i || 13 == i || 18 == i || 23 == i)
            {
                if (code[i] != '-') return false;
            }
            else
            {
                char chr = code[i];
                bool is_hex_char = (chr >= '0' && chr <= '9') ||
                       (chr >= 'a' && chr <= 'f') ||
                       (chr >= 'A' && chr <= 'F');
                if (!is_hex_char) return false;
            }
        }
        return true;
    }

    public static string RemoveUUIDs(string code)
    {
        return string.Join(":", code.Split(':').Where(codepart => !IsUUID(codepart)));
    }

    private IEnumerator<JSONNode> LogEntries(string logFileName)
    {
        foreach (string line in File.ReadLines(logFileName))
        {
            yield return JSONNode.Parse(line);
        }
    }

    private IEnumerator ProcessHeader()
    {
        string group = null;
        string user_id = null;
        bool videoConsent = false;
        bool childDriven = true;
        bool expressive = true;
        List<string> completed_lessons = null;
        Dictionary<string, string> shared_avatar_versions = new Dictionary<string, string>();
        Dictionary<string, string> private_avatar_versions = new Dictionary<string, string>();
        Dictionary<string, string> scene_versions = new Dictionary<string, string>();
        bool videoEnabled = false;
        int itemsLoaded = 0;
        while (itemsLoaded < 6 && fileLines.MoveNext())
        {
            JSONNode currentNode = fileLines.Current;
            string header = (string)currentNode["h"];
            ++itemsLoaded;
            if ("kid" == header)
            {
                group = (string)currentNode["group"];
                user_id = (string)currentNode["id"];
                videoConsent = (bool)currentNode["video-consent"];
                childDriven = (bool)currentNode["child-driven"];
                expressive = (bool)currentNode["expressive"];
            }
            else if ("config" == header)
            {
                videoEnabled = (bool)currentNode["video-enabled"];
            }
            else if ("shared-avatars" == header)
            {
                shared_avatar_versions = ParseStringDictionary((string)currentNode["val"]);
            }
            else if ("private-avatars" == header)
            {
                private_avatar_versions = ParseStringDictionary((string)currentNode["val"]);
            }
            else if ("starttime" == header)
            {
                TimeKeeper.InitSimulatedTime(RestoreDouble((string)currentNode["hex"]));
            }
            else if ("tutorial" == header)
            {
                completed_lessons = LoadStringList(currentNode["val"]);
            }
            else if ("scenes" == header)
            {
                scene_versions = ParseStringDictionary((string)currentNode["val"]);
            }
            else
            {
                --itemsLoaded;
            }
        }
        if (itemsLoaded < 6) { Application.Quit(); }
        yield return environment.InitiateReplay( group: group,
                                    user_id: user_id,
                                    videoEnabled: videoEnabled,
                                    videoConsent: videoConsent,
                                    childDriven: childDriven,
                                    expressive: expressive,
                                    completed_lessons: completed_lessons,
                                    shared_avatar_versions: shared_avatar_versions,
                                    private_avatar_versions: private_avatar_versions,
                                    scene_versions: scene_versions);
    }

    private void ProcessStep()
    {
        bool readAnyLines = false;
        while (fileLines.MoveNext())
        {
            readAnyLines = true;
            JSONNode currentNode = fileLines.Current;
            string header = (string)currentNode["h"];
            if ("time" == header)
            {
                nextTimestamp = RestoreDouble((string)currentNode["hex"]);
                return;
            }
            else if ("tchdn" == header)
            {
                int finger = (int)currentNode["fr"];
                float x = (float)currentNode["x"];
                float y = (float)currentNode["y"];
                fingerByTouch[(string)currentNode["id"]] = finger;
                simTouches[finger] = GameObject.Instantiate(simTouchPrefab);
                Vector2 tchWrld = (Vector2)Camera.main.ScreenToWorldPoint(new Vector2(x, y));
                simTouches[finger].transform.position = new Vector3(tchWrld.x, tchWrld.y, 0);
                touchManager.ReplayTouchDown(finger, x, y);
            }
            else if ("tchmv" == header)
            {
                string tchid = currentNode["id"];
                if (fingerByTouch.ContainsKey(tchid))
                {
                    int finger = fingerByTouch[tchid];
                    float x = (float)currentNode["x"];
                    float y = (float)currentNode["y"];
                    Vector2 tchWrld = (Vector2)Camera.main.ScreenToWorldPoint(new Vector2(x, y));
                    simTouches[finger].transform.position = new Vector3(tchWrld.x, tchWrld.y, 0);
                    touchManager.ReplayTouchMove(finger, x, y);
                }
            }
            else if ("tchup" == header)
            {
                string tchid = currentNode["id"];
                if (fingerByTouch.ContainsKey(tchid))
                {
                    int finger = fingerByTouch[tchid];
                    Destroy(simTouches[finger]);
                    simTouches.Remove(finger);
                    touchManager.ReplayTouchUp(finger);
                    fingerByTouch.Remove((string)currentNode["id"]);
                }
            }
            else if ("voice" == header)
            {
                voiceActivityDetector.ReplayVoiceEvent((bool)currentNode["isOn"]);
            }
            else if ("speech-segment-formed" == header)
            {
                JSONNode segnumNode = currentNode["segnum"];
                speechSegmentCounter = null != segnumNode ? (int)segnumNode : ++speechSegmentCounter;
                voiceActivityDetector.ReplayAudioSegmentEvent(speechSegmentCounter);
            }
            else if ("speech-reco-start" == header)
            {
                speechRecoServerComm.ReplayRecoStart((int)currentNode["id"]);
            }
            else if ("speech-reco-interrupt" == header)
            {
                speechRecoServerComm.ReplayRecoInterrupt((int)currentNode["id"]);
            }
            else if ("speech-reco-results" == header)
            {
                string response = (string)(new JSONString((string)currentNode["response"]));
                speechRecoServerComm.ReplayRecoResult((int)currentNode["id"], response);
            }
        }
        if (!readAnyLines)
        {
            nextTimestamp = float.MaxValue;
            touchManager.SetReplayMode(false);
            synthesizerController.SetReplayMode(false);
            speechRecoServerComm.SetReplayMode(false);
            voiceActivityDetector.SetReplayMode(false);
        }
    }

    private double RestoreDouble(string hexRecord)
    {
        byte[] bytes = new byte[hexRecord.Length / 2];
        for (int i = 0; i < hexRecord.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hexRecord.Substring(i, 2), 16);
        }
        return BitConverter.ToDouble(bytes, 0);
    }

    private float RestoreFloat(string hexRecord)
    {
        byte[] bytes = new byte[hexRecord.Length / 2];
        for (int i = 0; i < hexRecord.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hexRecord.Substring(i, 2), 16);
        }
        return BitConverter.ToSingle(bytes, 0);
    }

    private Dictionary<string, string> ParseStringDictionary(string record)
    {
        Dictionary<string, string> parsed = new Dictionary<string, string>();
        foreach (string entry in record.Split(','))
        {
            string[] entryParts = entry.Split(':');
            if (2 != entryParts.Length) continue;
            parsed[entryParts[0]] = entryParts[1];
        }
        return parsed;
    }

    private List<string> LoadStringList(JSONNode entry)
    {
        List<string> strList = new List<string>();
        foreach (JSONNode child in entry)
        {
            strList.Add((string)child);
        }
        return strList;
    }

    private void LoadRandomTracks(string filename)
    {
        Dictionary<string, List<RandomUtil.FloatSample>> floatSamples = new Dictionary<string, List<RandomUtil.FloatSample>>();
        Dictionary<string, List<RandomUtil.IntSample>> intSamples = new Dictionary<string, List<RandomUtil.IntSample>>();
        foreach (string line in File.ReadLines(filename))
        {
            if (line.StartsWith("{\"h\": \"rf\""))
            {
                JSONNode record = JSONNode.Parse(line);
                RandomUtil.FloatSample floatSample = new RandomUtil.FloatSample();
                floatSample.low  = RestoreFloat((string)record["l"]);
                floatSample.high = RestoreFloat((string)record["h"]);
                floatSample.rand = RestoreFloat((string)record["v"]);
                string randid = (string)record["id"];
                DictUtil.GetOrSpawn(floatSamples, randid).Add(floatSample);
            }
            else if (line.StartsWith("{\"h\": \"ri\""))
            {
                JSONNode record = JSONNode.Parse(line);
                RandomUtil.IntSample intSample = new RandomUtil.IntSample();
                intSample.low = (int)record["l"];
                intSample.high = (int)record["h"];
                intSample.rand = (int)record["v"];
                string randid = (string)record["id"];
                DictUtil.GetOrSpawn(intSamples, randid).Add(intSample);
            }
        }
        RandomUtil.SetupForReplay(floatSamples, intSamples);
    }

    private void LoadSynthReplayRecords(string filename)
    {
        List<SynthesizerController.ReplayRecord> synthRecords = new List<SynthesizerController.ReplayRecord>();
        double time = 0;
        foreach (string line in File.ReadLines(filename))
        {
            if (line.StartsWith("{\"h\": \"starttime\"") || line.StartsWith("{\"h\": \"time\""))
            {
                JSONNode record = JSONNode.Parse(line);
                time = RestoreDouble((string)record["hex"]);
            }
            else if (line.StartsWith("{\"h\": \"speech-req\""))
            {
                JSONNode record = JSONNode.Parse(line);
                SynthesizerController.ReplayRecord synthRecord = new SynthesizerController.ReplayRecord();
                synthRecord.id = (int)record["id"];
                synthRecord.cause = RemoveUUIDs((string)record["bc"]);
                synthRecord.text = (string)record["str"];
                synthRecord.timestamp = time;
                synthRecord.end_timestamp = time + 20;
                synthRecords.Add(synthRecord);
            }
            else if (line.StartsWith("{\"h\": \"speech-end\""))
            {
                JSONNode record = JSONNode.Parse(line);
                int id = (int)record["id"];
                for (int i = synthRecords.Count - 1; i >= 0; --i)
                {
                    SynthesizerController.ReplayRecord synthRecord = synthRecords[i];
                    if (synthRecord.id == id)
                    {
                        synthRecord.end_timestamp = time;
                    }
                }
            }
        }
        synthesizerController.SetReplayRecords(synthRecords);
    }
}
