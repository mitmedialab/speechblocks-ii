using System;
using System.IO;
using UnityEngine;

public class Recorder : MonoBehaviour
{
    private MicrophoneKeeper micKeeper = null;
    private SoundUtils.WavFileHandle wavFileHandle = null;
    private UploadManager uploadManager = null;
    private Environment environment;
    private bool recordingFinished = false;
    private string targetPath = null;

    private const int VIDEO_HEIGHT = 180;

    public void Setup()
    {
        micKeeper = GetComponent<MicrophoneKeeper>();
        environment = GetComponent<Environment>();
        uploadManager = GetComponent<UploadManager>();
        string group = environment.GetGroup();
        string user_id = environment.GetUser().GetID();
        string timestamp = environment.GetLoginTime().ToString("yyyy-MM-dd-HH-mm-ss-FFF");
        targetPath = $"Audio/{group}/{user_id}/{timestamp}.wav";
        string recordPath = Path.Combine(Application.persistentDataPath, targetPath);
        wavFileHandle = new SoundUtils.WavFileHandle(recordPath, MicrophoneKeeper.SAMPLE_RATE, micKeeper.ChannelCount());
    }

    public bool RecordingFinished()
    {
        return recordingFinished;
    }

    void Update()
    {
        wavFileHandle.AddSamples(micKeeper.GetNewData());
    }

    public void FinishRecording()
    {
        Destroy(this);
        wavFileHandle.Finish();
        string tempPath = Path.Combine(Application.persistentDataPath, targetPath);
        uploadManager.ScheduleUpload(sourcePath: tempPath, targetPath: targetPath, deleteWhenDone: true);
        recordingFinished = true;
    }
}
