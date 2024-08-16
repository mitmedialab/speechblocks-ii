// On Android, voice activity detector utilizes android-vad-v1.0.1-release.aar by George Konovalov.
// Which in turn is based on WebRTC from Google.
// Source code is here: https://github.com/gkonovalov/android-vad
// This VAD is a bit prone to false positives, but overall is quite legit.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class VoiceActivityDetector : MonoBehaviour
{
    private const float TALKING_START_OFFSET = 0.5f;
    private       float TALKING_END_OFFSET = 0.5f;
    private const float MIN_VOICE_TIME = 0.05f;
    private const float SAMPLING_WINDOW = 0.1f;
    private const float MIN_AMPLITUDE = 0.03f;
    private const float BURN_IN_TIMEOUT = 0.5f;
    private const float CUTOFF_TIME = 2f;
    private const float INTERIM_RECORDING_SENDING_INTERVAL = 1f;

    private bool shouldSendInterimClips = true;

    private Action<AudioClip, int> talkingSegmentFormedEvent = null;

    private MicrophoneKeeper microphoneKeeper;

    private List<float> _currentRecordingVoice;

    private double _overallTalkingStart = -1;
    private double _overallTalkingEnd = -1;
    private int _talkingStartsCount = 0;
    private double _currentTalkingStart = -1;

    private bool isListening = false;
    private bool isRecording = false;

    private SynthesizerController synthesizer;

    private const int SAMPLE_RATE = 16000;
    private float[] leftoverSamples = null;

    private bool isReplayMode = false;
    private bool replayVoiceOn = false;
    private bool replayHasAudioSegment = false;

    private AndroidJavaObject vad = null;
    private int lastVadPtr = 0;
    private Int16[] audioFrame = new Int16[160];
    private int speechIndex = 0;

    private double lastRecordingStart = -1;

    private double lastInterimRecordingSent = -1000;

    private AudioClip notification = null;

    public void Start()
    {
        microphoneKeeper = GetComponent<MicrophoneKeeper>();
        synthesizer = GetComponent<SynthesizerController>();
        notification = Resources.Load<AudioClip>("Sounds/andersmmg_ding");
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass vad_config_class = new AndroidJavaClass("com.konovalov.vad.VadConfig");
        AndroidJavaClass vad_sample_rate_enum = new AndroidJavaClass("com.konovalov.vad.VadConfig$SampleRate");
        AndroidJavaClass vad_frame_size_enum = new AndroidJavaClass("com.konovalov.vad.VadConfig$FrameSize");
        AndroidJavaClass vad_mode_enum = new AndroidJavaClass("com.konovalov.vad.VadConfig$Mode");
        AndroidJavaObject config_builder = vad_config_class.CallStatic<AndroidJavaObject>("newBuilder");
        config_builder = config_builder.Call<AndroidJavaObject>("setSampleRate", vad_sample_rate_enum.GetStatic<AndroidJavaObject>("SAMPLE_RATE_16K"));
        config_builder = config_builder.Call<AndroidJavaObject>("setFrameSize", vad_frame_size_enum.GetStatic<AndroidJavaObject>("FRAME_SIZE_160"));
        config_builder = config_builder.Call<AndroidJavaObject>("setMode", vad_mode_enum.GetStatic<AndroidJavaObject>("VERY_AGGRESSIVE"));
        AndroidJavaObject config = config_builder.Call<AndroidJavaObject>("build");
        vad = new AndroidJavaObject("com.konovalov.vad.Vad", config);
        _currentRecordingVoice = new List<float>();
        vad.Call("start");
#endif
    }

    public void StartDetection(Action<AudioClip, int> callback, float talkingEndOffset = 0.5f, bool shouldSendInterimClips = true)
    {
        Debug.Log("Start detection");
        talkingSegmentFormedEvent = callback;
        this.TALKING_END_OFFSET = talkingEndOffset;
        this.shouldSendInterimClips = shouldSendInterimClips;
        if (isListening)
            return;

        isListening = true;

        if (!synthesizer.IsSpeaking()) { SetupForRecording(); }
    }

    public void StopDetection()
    {
        if (!isListening)
            return;

        Debug.Log("Stop detection");

        isListening = false;

        InterruptRecording();
    }

    public bool IsListening()
    {
        return isListening;
    }

    public bool IsActivelyRecording()
    {
        return isRecording && TimeKeeper.time - lastRecordingStart > BURN_IN_TIMEOUT;
    }

    public bool IsPickingVoice()
    {
        if (!isReplayMode)
        {
            return _overallTalkingStart > 0;
        }
        else
        {
            return replayVoiceOn;
        }
    }

    public void Update()
    {
        if (!isListening) return;
        if (!isReplayMode)
        {
            if (synthesizer.IsSpeaking() && isRecording) { InterruptRecording(); }
            if (IsActivelyRecording()) { UpdateVoiceDetectionLoop(); }
            if (!synthesizer.IsSpeaking() && !isRecording) { SetupForRecording(); }
        }
        else if (replayHasAudioSegment && null != talkingSegmentFormedEvent)
        {
            replayHasAudioSegment = false;
            try
            {
                talkingSegmentFormedEvent(null, _talkingStartsCount);
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
        }
    }

    public void SetReplayMode(bool replayMode)
    {
        isReplayMode = replayMode;
    }

    public void ReplayVoiceEvent(bool voiceOn)
    {
        replayVoiceOn = voiceOn;
    }

    public void ReplayAudioSegmentEvent(int segmentNumber)
    {
        replayHasAudioSegment = true;
        _talkingStartsCount = segmentNumber;
    }

    private void SetupForRecording()
    {
        SoundUtils.PlaySound(GetComponent<AudioSource>(), notification);
        lastRecordingStart = TimeKeeper.time;

        _currentRecordingVoice = new List<float>();
        lastVadPtr = 0;

        _overallTalkingStart = -1;
        _overallTalkingEnd = -1;
        _currentTalkingStart = -1;
        Logging.LogVoice(isOn: false);

        isRecording = true;
    }

    private void InterruptRecording()
    {
        _currentRecordingVoice = null;
        isRecording = false;
    }

    private void UpdateVoiceDetectionLoop()
    {
        AddVoiceSamples();

        bool wasTalking = _currentTalkingStart > 0;
        bool isTalking = CheckForSpeech();

        if (!wasTalking)
        {
            if (isTalking)
            {
                Logging.LogVoice(isOn: true);
                Debug.Log("Started talking");
                _currentTalkingStart = TimeKeeper.time;
            }
        }

        if (isTalking && _overallTalkingStart < 0 && TimeKeeper.time - _currentTalkingStart > MIN_VOICE_TIME)
        {
            Debug.Log("Interval accepted");
            _overallTalkingStart = _currentTalkingStart;
            ++_talkingStartsCount;
        }

        if (wasTalking && !isTalking)
        {
            if (_overallTalkingStart >= 0) { _overallTalkingEnd = TimeKeeper.time; }
            _currentTalkingStart = -1;
        }

        TrimInitialSilence();

        if (_currentTalkingStart < 0 && _overallTalkingEnd >= 0)
        {
            if (TimeKeeper.time - _overallTalkingEnd > TALKING_END_OFFSET || (shouldSendInterimClips && _overallTalkingEnd - _overallTalkingStart > CUTOFF_TIME))
            {
                Debug.Log($"Segment formed; voice time {_overallTalkingEnd - _overallTalkingStart:0.####}");

                _overallTalkingStart = -1;
                _overallTalkingEnd = -1;

                AudioClip clip = MakeAudioClipFromSamples(_currentRecordingVoice.ToArray());

                Logging.LogSpeechSegmentFormed(_talkingStartsCount, interim: false);
                try
                {
                    if (null != talkingSegmentFormedEvent) { talkingSegmentFormedEvent(clip, _talkingStartsCount); }
                }
                catch (Exception e)
                {
                    ExceptionUtil.OnException(e);
                }
            }
        }

        if (shouldSendInterimClips
            && _overallTalkingStart >= 0 && _currentTalkingStart >= 0
            && TimeKeeper.time - _overallTalkingStart > CUTOFF_TIME
            && TimeKeeper.time - lastInterimRecordingSent > INTERIM_RECORDING_SENDING_INTERVAL)
        {
            Debug.Log("SENDING INTERIM CLIP");
            AudioClip clip = MakeInterimAudioClip();
            Logging.LogSpeechSegmentFormed(_talkingStartsCount, interim: true);
            try
            {
                if (null != talkingSegmentFormedEvent) { talkingSegmentFormedEvent(clip, _talkingStartsCount); }
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
            lastInterimRecordingSent = TimeKeeper.time;
        }
    }

    private void AddVoiceSamples()
    {
        float[] currentSamples = microphoneKeeper.GetNewData();
        if (null != leftoverSamples)
        {
            currentSamples = leftoverSamples.Concat(currentSamples).ToArray();
        }
        int leftoverSize;
        float[] downsampled = SoundUtils.Downsample(currentSamples, MicrophoneKeeper.SAMPLE_RATE, SAMPLE_RATE, out leftoverSize);
        if (0 == leftoverSize)
        {
            leftoverSamples = null;
        }
        else
        {
            leftoverSamples = currentSamples.Skip(currentSamples.Length - leftoverSize).ToArray();
        }    
        _currentRecordingVoice.AddRange(downsampled);
    }

    private AudioClip MakeInterimAudioClip()
    {
        int clipStart = _currentRecordingVoice.Count - Mathf.CeilToInt((INTERIM_RECORDING_SENDING_INTERVAL + CUTOFF_TIME) * SAMPLE_RATE);
        if (clipStart < 0) clipStart = 0;
        float[] sampleBuffer = new float[_currentRecordingVoice.Count - clipStart];
        for (int i = clipStart, j = 0; i < _currentRecordingVoice.Count; ++i, ++j) { sampleBuffer[j] = _currentRecordingVoice[i]; }
        return MakeAudioClipFromSamples(sampleBuffer);
    }

    private AudioClip MakeAudioClipFromSamples(float[] samples)
    {
        AudioClip clip;

        clip = AudioClip.Create("RecordedVoice", samples.Length, microphoneKeeper.ChannelCount(), SAMPLE_RATE, false);
        clip.SetData(samples, 0);

        return clip;
    }

    private void TrimInitialSilence()
    {
        if (_overallTalkingStart > 0 || _currentTalkingStart > 0) return;
        int maxInitialSilentSamples = (int)(MicrophoneKeeper.SAMPLE_RATE * TALKING_START_OFFSET);

        if (_currentRecordingVoice.Count > maxInitialSilentSamples)
        {
            int nToRemove = _currentRecordingVoice.Count - maxInitialSilentSamples;
            _currentRecordingVoice.RemoveRange(0, nToRemove);
            lastVadPtr -= nToRemove;
        }
    }

    private float GetClipAmplitude()
    {
        int samples_number = (int)(SAMPLE_RATE * SAMPLING_WINDOW);
        if (_currentRecordingVoice.Count < samples_number) return 0.0f;
        double avgAmp = 0;
        for (int i = _currentRecordingVoice.Count - samples_number; i < _currentRecordingVoice.Count; ++i)
        {
            avgAmp += Mathf.Abs(_currentRecordingVoice[i]);
        }
        return (float)(avgAmp / samples_number);
    }

    private bool CheckForSpeech()
    {
        if (null == vad)
        {
            float clipAmplitude = GetClipAmplitude();
            return clipAmplitude > MIN_AMPLITUDE;
        }
        else
        {
            bool detected = false;
            bool anyChecks = false;
            for (int hi = lastVadPtr + 160; hi <= _currentRecordingVoice.Count; lastVadPtr += 160, hi += 160)
            {
                anyChecks = true;
                for (int i = lastVadPtr, j = 0; i < hi; ++i, ++j)
                {
                    audioFrame[j] = (Int16)(_currentRecordingVoice[i] * SoundUtils.RESCALE_FACTOR);
                }
                bool detectRes = vad.Call<bool>("isSpeech", audioFrame);
                Debug.Log("SPEECH: " + detectRes.ToString() + " " + speechIndex.ToString());
                detected |= detectRes;
            }
            if (!anyChecks) { return _currentTalkingStart > 0; }
            return detected;
        }
    }
}