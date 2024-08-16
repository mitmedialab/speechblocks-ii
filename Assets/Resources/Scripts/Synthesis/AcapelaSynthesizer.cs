using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class AcapelaSynthesizer : ISynthesizerModule {
    private AndroidJavaObject tts = null;
    private int lastSpeechStatus = SynthesizerController.SPEECH_STATUS_FAIL;
    private Action onInit;
    private bool initialized = false;

    private const int SUCCESS = 0;
    private const int QUEUE_FLUSH = 0;

    private const float BASE_RATE = 0.9f;

    private AndroidJavaObject PARAMS = null;

    public AcapelaSynthesizer(Action onInit)
    {
        this.onInit = onInit;
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
        PARAMS = new AndroidJavaObject("java.util.HashMap");
        IntPtr putMethod = AndroidJNIHelper.GetMethodID(PARAMS.GetRawClass(), "put", "(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;");
        object[] paramArgs = new object[2];
        paramArgs[0] = new AndroidJavaObject("java.lang.String", "volume");
        paramArgs[1] = new AndroidJavaObject("java.lang.String", "0.25");
        AndroidJNI.CallObjectMethod(PARAMS.GetRawObject(), putMethod, AndroidJNIHelper.CreateJNIArgArray(paramArgs));
        tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", context, new InitListener(this), "com.acapelagroup.android.tts");
#endif
    }

    public IEnumerator Speak(SynQuery query)
    {
        lastSpeechStatus = SynthesizerController.SPEECH_STATUS_FAIL;
        if (!initialized) yield break;
        string acapelaRequest = ConstructAcapelaRequest(query);
        if (null == acapelaRequest) { lastSpeechStatus = SynthesizerController.SPEECH_STATUS_OK; yield break; }
        Debug.Log("ACAPELA REQUEST: " + acapelaRequest);
        Debug.Log($"CALLING ACAPELA");
        if (SUCCESS != tts.Call<int>("speak", acapelaRequest, QUEUE_FLUSH, PARAMS)) yield break;
        while (tts.Call<bool>("isSpeaking")) yield return null;
        Debug.Log($"ACAPELA DONE");
        lastSpeechStatus = SynthesizerController.SPEECH_STATUS_OK;
    }

    public int LastSpeechStatus()
    {
        return lastSpeechStatus;
    }

    public void Interrupt()
    {
        if (!initialized) return;
        tts.Call<int>("stop");
    }

    public bool IsActivelySpeaking()
    {
        return tts.Call<bool>("isSpeaking");
    }

    private string ConstructAcapelaRequest(SynQuery query)
    {
        List<SynQuery> children = query.GetChildren();
        if (null == children || 0 == children.Count) return null;
        StringBuilder requestBuilder = new StringBuilder();
        float currentRate = -1;
        foreach (SynQuery child in children)
        {
            float rate = child.GetRate();
            if (currentRate != rate)
            {
                AddRateTag(rate, requestBuilder);
                currentRate = rate;
            }
            if (child.ContainsKey("phonemecode"))
            {
                AddPhonemeCode(child, requestBuilder);
            }
            else if (child.ContainsKey("text"))
            {
                requestBuilder.Append((string)child.GetParam("text"));
            }
            else if (child.ContainsKey("spell"))
            {
                AddSpellCode(child, requestBuilder);
            }
            else if (child.ContainsKey("break"))
            {
                AddBreakCode(child, requestBuilder);
            }
        }
        return requestBuilder.ToString();
    }

    private void AddRateTag(float rate, StringBuilder requestBuilder)
    {
        requestBuilder.Append("\\RSPD=");
        requestBuilder.Append((int)(100 * BASE_RATE * rate));
        requestBuilder.Append("\\");
    }

    private void AddPhonemeCode(SynQuery phonemeQuery, StringBuilder requestBuilder)
    {
        string phonemecode = (string)phonemeQuery.GetParam("phonemecode");
        phonemecode = phonemecode.Replace(';', ' ');
        phonemecode = phonemecode.Replace("0", "");
        requestBuilder.Append("\\Prn=");
        requestBuilder.Append(phonemecode);
        requestBuilder.Append(" \\ ");
    }

    private void AddSpellCode(SynQuery spellQuery, StringBuilder requestBuilder)
    {
        string toSpell = (string)spellQuery.GetParam("spell");
        requestBuilder.Append("\\RmS=1\\");
        requestBuilder.Append(toSpell);
        requestBuilder.Append("\\RmS=0\\ ");
    }

    private void AddBreakCode(SynQuery breakQuery, StringBuilder requestBuilder)
    {
        float breakDuration = breakQuery.GetFloatParam("break", 0f);
        int breakDurationMs = (int)(breakDuration * 1000);
        if (breakDurationMs <= 0) return;
        requestBuilder.Append("\\Pau=");
        requestBuilder.Append(breakDurationMs);
        requestBuilder.Append("\\ ");
    }

    private void OnSuccessfulInit()
    {
        //IntPtr putMethod = AndroidJNIHelper.GetMethodID(tts.GetRawClass(), "getVoices");
        Debug.Log("ACAPELA GET VOICES");
        AndroidJavaObject voices = tts.Call<AndroidJavaObject>("getVoices");
        Debug.Log("ACAPELA GET VOICE ITERATOR");
        AndroidJavaObject voicesIterator = voices.Call<AndroidJavaObject>("iterator");
        Debug.Log("ACAPELA HAS NEXT");
        while (voicesIterator.Call<bool>("hasNext"))
        {
            Debug.Log("ACAPELA NEXT");
            AndroidJavaObject voice = voicesIterator.Call<AndroidJavaObject>("next");
            Debug.Log("ACAPELA GET NAME");
            string voiceName = voice.Call<string>("getName");
            Debug.Log("ACAPELA VOICE: " + voiceName);
            if ("eng-USA-Sharon" == voiceName)
            {
                Debug.Log("ACAPELA SET VOICE");
                tts.Call<int>("setVoice", voice);
                initialized = true;
                onInit();
                break;
            }
        }
        Debug.Log("ACAPELA NO MORE VOICES");
    }

    private class InitListener : AndroidJavaProxy
    {
        private AcapelaSynthesizer acapelaSynth;

        public InitListener(AcapelaSynthesizer acapelaSynth) : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            this.acapelaSynth = acapelaSynth;
        }

        public void onInit(int status)
        {
            if (SUCCESS == status)
            {
                Debug.Log("ACAPELA INIT SUCCESSFUL");
                acapelaSynth.OnSuccessfulInit();
            }
            else
            {
                Debug.Log($"ACAPELA INIT RETURNED {status}");
            }
        }
    }
}
