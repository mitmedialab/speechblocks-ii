using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;

public class GoogleSynthesizer : ISynthesizerModule {
    private Dictionary<string, object> acapelaToIPATree = new Dictionary<string, object>();
    private AndroidJavaObject tts = null;
    private int lastSpeechStatus = SynthesizerController.SPEECH_STATUS_FAIL;
    private Action onInit;
    private bool initialized = false;

    private const int SUCCESS = 0;
    private const int QUEUE_FLUSH = 0;

    private const float BASE_RATE = 0.77f;

    private AndroidJavaObject PARAMS = null;

    public GoogleSynthesizer(Action onInit)
    {
        this.onInit = onInit;
        acapelaToIPATree = PhonemeUtil.LoadPhonemeConversion("Config/ACAPELA_to_IPA");
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
        tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", context, new InitListener(this), "com.google.android.tts");
#endif
    }

    public IEnumerator Speak(SynQuery query)
    {
        lastSpeechStatus = SynthesizerController.SPEECH_STATUS_FAIL;
        string ssml = FormSSMLForGoogle(query);
        if (null == ssml) { lastSpeechStatus = SynthesizerController.SPEECH_STATUS_OK; yield break; }
        Debug.Log("ACAPELA REQUEST: " + ssml);
        if (!initialized) yield break;
        Debug.Log($"CALLING ACAPELA");
        if (SUCCESS != tts.Call<int>("speak", ssml, QUEUE_FLUSH, PARAMS)) yield break;
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

    private string FormSSMLForGoogle(SynQuery synQuery)
    {
        SynQuery gquery = BuildSynQueryForGoogle(synQuery);
        if (null == gquery) return null;
        return "<speak>"
            + SynQuery.BuildSSML(gquery)
            + "</speak>";
    }

    private SynQuery BuildSynQueryForGoogle(SynQuery synQuery)
    {
        List<SynQuery> mainSequence = synQuery.GetChildren();
        BaseRateMainSequence(mainSequence);
        AdjustBreaks(mainSequence);
        List<List<SynQuery>> groups = SynQuery.GroupSequenceByProsody(mainSequence);
        List<SynQuery> secondLevel = new List<SynQuery>();
        foreach (List<SynQuery> group in groups)
        {
            if (0 == group.Count) continue;
            List<SynQuery> modGroup = new List<SynQuery>();
            float rate = group[0].GetRate();
            foreach (SynQuery item in group)
            {
                SynQuery mod = SynQuery.Del(item, "rate");
                if (mod.ContainsKey("phonemecode"))
                {
                    mod = SynQuery.Mod(mod, "alphabet", "ipa", "phonemecode", PreparePhonemecodeForGoogle((string)mod.GetParam("phonemecode")));
                    mod = SynQuery.Del(mod, "text");
                }
                modGroup.Add(mod);
            }
            SynQuery secondLevelQuery = SynQuery.Rate(SynQuery.Seq(modGroup), rate);
            secondLevel.Add(secondLevelQuery);
        }
        if (0 == secondLevel.Count) return null;
        return SynQuery.Seq(secondLevel);
    }

    private void BaseRateMainSequence(List<SynQuery> mainSequence)
    {
        for (int i = 0; i < mainSequence.Count; ++i)
        {
            mainSequence[i] = SynQuery.Rate(mainSequence[i], BASE_RATE);
        }
    }

    private void AdjustBreaks(List<SynQuery> mainSequence)
    {
        for (int i = 0; i < mainSequence.Count; ++i)
        {
            SynQuery query = mainSequence[i];
            if (query.ContainsKey("break"))
            {
                query = SynQuery.Mod(query, "break", query.GetFloatParam("break", 0) / query.GetRate(), "rate", 1f);
                mainSequence[i] = query;
            }
        }
    }

    private string PreparePhonemecodeForGoogle(string acapelaCode)
    {
        string phonemecode = MakePhonemeAdjustments(acapelaCode);
        phonemecode = PhonemeUtil.ConvertPhonemes(acapelaToIPATree, phonemecode);
        return ChangeAccentsAndSeparators(phonemecode);
    }

    private string MakePhonemeAdjustments(string phonemecode)
    {
        //int accentedPhonemesNum = phonemecode.Split(';').Where(ph => PhonemeUtil.HasAccent(ph)).Count();
        //if (1 != accentedPhonemesNum) return phonemecode;
        return PhonemeUtil.Unaccentuated(phonemecode);
    }

    private string ChangeAccentsAndSeparators(string phonemecode)
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (char c in phonemecode)
        {
            if (';' == c || '0' == c) continue;
            if ('1' == c)
            {
                stringBuilder.Append('ˈ');
            }
            else if ('2' == c)
            {
                stringBuilder.Append('ˌ');
            }
            else
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString();
    }

    private void OnSuccessfulInit()
    {
        initialized = true;
        onInit();
    }

    private class InitListener : AndroidJavaProxy
    {
        private GoogleSynthesizer googleSynth;

        public InitListener(GoogleSynthesizer googleSynth) : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            this.googleSynth = googleSynth;
        }

        public void onInit(int status)
        {
            if (SUCCESS == status)
            {
                Debug.Log("GOOGLE SYNTH INIT SUCCESSFUL");
                googleSynth.OnSuccessfulInit();
            }
            else
            {
                Debug.Log($"GOOGLE SYNTH INIT RETURNED {status}");
            }
        }
    }
}
