using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class SynQuery
{
    public SynQuery Clone()
    {
        SynQuery clone = new SynQuery();
        foreach (string key in parameters.Keys)
        {
            clone.parameters[key] = parameters[key];
        }
        return clone;
    }

    public void AssignParameter(string key, object value)
    {
        if (parameters.ContainsKey(key)) return;
        parameters[key] = value;
    }

    public bool ContainsKey(string key)
    {
        return parameters.ContainsKey(key);
    }

    public object GetParam(string paramName)
    {
        return DictUtil.GetOrDefault(parameters, paramName, null);
    }

    public object GetParam(string paramName, object defaultVal)
    {
        return DictUtil.GetOrDefault(parameters, paramName, defaultVal);
    }

    public float GetFloatParam(string paramName, float defaultVal)
    {
        object paramObj = GetParam(paramName);
        if (null == paramObj) return defaultVal;
        return (float)paramObj;
    }

    public float GetRate()
    {
        return GetFloatParam("rate", 1f);
    }

    public List<SynQuery> GetChildren()
    {
        return (List<SynQuery>)GetParam("children");
    }

    public static implicit operator SynQuery(string text)
    {
        SynQuery sayQuery = new SynQuery();
        sayQuery.parameters["text"] = text;
        return sayQuery;
    }

    public static SynQuery SayAs(string text, string phonemecode)
    {
        SynQuery sayQuery = FetchAudio(phonemecode);
        if (null == sayQuery) { sayQuery = new SynQuery(); }
        sayQuery.parameters["text"] = text;
        sayQuery.parameters["phonemecode"] = phonemecode;
        sayQuery.parameters["alphabet"] = "acapela";
        return sayQuery;
    }

    public static SynQuery Say(PGPair pg)
    {
        return SayAs(pg.GetGrapheme(), pg.GetPhonemeCode());
    }

    public static SynQuery Spell(string text)
    {
        SynQuery sayQuery = new SynQuery();
        sayQuery.parameters["spell"] = text.ToUpper();
        return sayQuery;
    }

    public static SynQuery Break(float duration)
    {
        SynQuery breakQuery = new SynQuery();
        breakQuery.parameters["break"] = duration;
        return breakQuery;
    }

    public static SynQuery Seq(params SynQuery[] sequence)
    {
        SynQuery seqQuery = new SynQuery();
        seqQuery.parameters["children"] = sequence.ToList();
        return seqQuery;
    }

    public static SynQuery Seq(IEnumerable<SynQuery> sequence)
    {
        SynQuery seqQuery = new SynQuery();
        seqQuery.parameters["children"] = sequence.ToList();
        return seqQuery;
    }

    public static SynQuery Format(string format, params SynQuery[] arguments)
    {
        List<SynQuery> sequence = new List<SynQuery>();
        int ptr = 0;
        while (ptr < format.Length)
        {
            int leftBracePos = format.IndexOf('{', ptr);
            if (leftBracePos < 0)
            {
                sequence.Add(format.Substring(ptr, format.Length - ptr));
                break;
            }
            else if (leftBracePos > ptr)
            {
                sequence.Add(format.Substring(ptr, leftBracePos - ptr));
            }
            int rightBracePos = format.IndexOf('}', leftBracePos + 1);
            if (rightBracePos < 0) {
                Debug.Log("SynQuery.Format: incorrect format");
                break;
            }
            int argID;
            if (!int.TryParse(format.Substring(leftBracePos + 1, rightBracePos - leftBracePos - 1), out argID)) {
                Debug.Log("SynQuery.Format: incorrect format");
                break;
            }
            if (argID < 0 || argID >= arguments.Length) {
                Debug.Log("SynQuery.Format: incorrect argument ID");
                break;
            }
            sequence.Add(arguments[argID]);
            ptr = rightBracePos + 1;
        }
        return Seq(sequence);
    }

    public static SynQuery Rate(SynQuery synQuery, float rate)
    {
        if (1 == rate) return synQuery;
        SynQuery clone = synQuery.Clone();
        clone.parameters["rate"] = rate * synQuery.GetRate();
        return clone;
    }

    public static SynQuery Del(SynQuery synQuery, params string[] parameters)
    {
        SynQuery mod = synQuery.Clone();
        foreach (string param in parameters)
        {
            mod.parameters.Remove(param);
        }
        return mod;
    }

    public static SynQuery Mod(SynQuery synQuery, params object[] keyValues)
    {
        SynQuery mod = synQuery.Clone();
        for (int i = 0; i < keyValues.Length; i += 2)
        {
            mod.parameters[(string)keyValues[i]] = keyValues[i + 1];
        }
        return mod;
    }

    public static List<List<SynQuery>> GroupSequenceByProsody(List<SynQuery> sequence)
    {
        List<List<SynQuery>> grouping = new List<List<SynQuery>>();
        List<SynQuery> currentGroup = new List<SynQuery>();
        if (0 != sequence.Count) { currentGroup.Add(sequence[0]); }
        for (int i = 1; i < sequence.Count; ++i)
        {
            if (sequence[i - 1].GetRate() != sequence[i].GetRate())
            {
                grouping.Add(currentGroup);
                currentGroup = new List<SynQuery>();
            }
            currentGroup.Add(sequence[i]);
        }
        grouping.Add(currentGroup);
        return grouping;
    }

    public static List<SynQuery> ToCanonicSequence(SynQuery synQuery)
    {
        List<SynQuery> flatSequence = new List<SynQuery>();
        GrowFlatSequence(synQuery, flatSequence, 1f);
        return GroupBetweenAudios(flatSequence);
    }

    public static string BuildSSML(SynQuery synQuery)
    {
        StringBuilder requestBuilder = new StringBuilder();
        AddSynQueryToRequest(synQuery, requestBuilder);
        return requestBuilder.ToString();
    }

    private static void AddSynQueryToRequest(SynQuery synQuery, StringBuilder requestBuilder)
    {
        bool hasProsodyHeader = AddProsodyToRequest(synQuery, requestBuilder);
        if (synQuery.ContainsKey("phonemecode"))
        {
            AddPhonemeCodeToRequest(synQuery, requestBuilder);
        }
        else if (synQuery.ContainsKey("text"))
        {
            requestBuilder.Append((string)synQuery.GetParam("text"));
        }
        else if (synQuery.ContainsKey("spell"))
        {
            AddCodeForSpell(synQuery, requestBuilder);
        }
        else if (synQuery.ContainsKey("break"))
        {
            AddCodeForBreak(synQuery, requestBuilder);
        }
        else if (synQuery.ContainsKey("audio"))
        {
            AddCodeForAudio(synQuery, requestBuilder);
        }
        else
        {
            foreach (SynQuery child in synQuery.GetChildren())
            {
                AddSynQueryToRequest(child, requestBuilder);
            }
        }
        if (hasProsodyHeader) { requestBuilder.Append("</prosody>"); }
    }

    private static bool AddProsodyToRequest(SynQuery speechItem, StringBuilder stringBuilder)
    {
        if (1 == speechItem.GetRate()) return false;
        stringBuilder.Append($"<prosody rate=\"{speechItem.GetRate():0.##}\">");
        return true;
    }

    private static void AddPhonemeCodeToRequest(SynQuery phonemeCodeItem, StringBuilder requestBuilder)
    {
        object textObj = phonemeCodeItem.GetParam("text");
        if (null != textObj)
        {
            requestBuilder.Append("<phoneme alphabet='");
            requestBuilder.Append((string)phonemeCodeItem.GetParam("alphabet"));
            requestBuilder.Append("' ph='");
            requestBuilder.Append((string)phonemeCodeItem.GetParam("phonemecode"));
            requestBuilder.Append("'>");
            requestBuilder.Append((string)textObj);
            requestBuilder.Append("</phoneme>");
        }
        else
        {
            requestBuilder.Append("<phoneme alphabet='");
            requestBuilder.Append((string)phonemeCodeItem.GetParam("alphabet"));
            requestBuilder.Append("' ph='");
            requestBuilder.Append((string)phonemeCodeItem.GetParam("phonemecode"));
            requestBuilder.Append("'/>");
        }
    }

    private static void AddCodeForSpell(SynQuery spellCodeItem, StringBuilder requestBuilder)
    {
        requestBuilder.Append("<say-as interpret-as=\"characters\">");
        requestBuilder.Append((string)spellCodeItem.GetParam("spell"));
        requestBuilder.Append("</say-as>");
    }

    private static void AddCodeForBreak(SynQuery breakItem, StringBuilder requestBuilder)
    {
        requestBuilder.Append("<break time=\"");
        requestBuilder.Append((int)(breakItem.GetFloatParam("break", 0) * 1000));
        requestBuilder.Append("ms\"/>");
    }

    private static void AddCodeForAudio(SynQuery audioItem, StringBuilder requestBuilder)
    {
        requestBuilder.Append("<audio a='");
        requestBuilder.Append((string)audioItem.GetParam("audio"));
        requestBuilder.Append("'/>");
    }

    private static void GrowFlatSequence(SynQuery synQuery, List<SynQuery> flatSequence, float currentRate)
    {
        if (synQuery.parameters.ContainsKey("children")) {
            if (synQuery.parameters.ContainsKey("rate")) { currentRate *= (float)synQuery.parameters["rate"]; }
            List<SynQuery> children = (List<SynQuery>)synQuery.parameters["children"];
            foreach (SynQuery child in children)
            {
                GrowFlatSequence(child, flatSequence, currentRate);
            }
        } else
        {
            synQuery = TrimTextAfterAudio(synQuery, flatSequence);
            if (null == synQuery) return;
            flatSequence.Add(Rate(synQuery, currentRate));
        }
    }

    private static SynQuery TrimTextAfterAudio(SynQuery synQuery, List<SynQuery> flatSequence)
    {
        if (!synQuery.ContainsKey("text")
            || synQuery.ContainsKey("phonemecode")
            || !EndsWithAudio(flatSequence)) { return synQuery; }
        string text = (string)synQuery.GetParam("text");
        int trimPos = TrimTextAfterAudio(text);
        if (trimPos == text.Length) return null;
        if (0 == trimPos) return synQuery;
        return Mod(synQuery, "text", text.Substring(trimPos));
    }

    private static bool EndsWithAudio(List<SynQuery> flatSequence)
    {
        for (int i = flatSequence.Count - 1; i >= 0; --i)
        {
            if (flatSequence[i].ContainsKey("break")) continue;
            if (flatSequence[i].ContainsKey("audio")) return true;
            return false;
        }
        return false;
    }

    private static int TrimTextAfterAudio(string text)
    {
        for (int i = 0; i < text.Length; ++i)
        {
            if (char.IsLetterOrDigit(text[i])) return i;
        }
        return text.Length;
    }

    private static List<SynQuery> GroupBetweenAudios(List<SynQuery> flatSequence)
    {
        List<SynQuery> groupedSeq = new List<SynQuery>();
        int ptr = 0;
        while (true)
        {
            int nextAudio = flatSequence.FindIndex(ptr, synQ => synQ.parameters.ContainsKey("audio"));
            if (nextAudio < 0)
            {
                groupedSeq.Add(Seq(flatSequence.Skip(ptr).Take(flatSequence.Count - ptr)));
                return groupedSeq;
            }
            else
            {
                if (nextAudio > ptr)
                {
                    groupedSeq.Add(Seq(flatSequence.Skip(ptr).Take(nextAudio - ptr)));
                }
                groupedSeq.Add(flatSequence[nextAudio]);
                ptr = nextAudio + 1;
            }
        }
    }

    private static SynQuery FetchAudio(string phonemecode)
    {
        string basicPhonemecode = PhonemeUtil.Unaccentuated(phonemecode);
        AudioClip audio = PhonemeUtil.SoundOf(basicPhonemecode);
        if (null == audio) return null;
        SynQuery synQuery = new SynQuery();
        synQuery.parameters["audio"] = audio;
        return synQuery;
    }

    private Dictionary<string, object> parameters = new Dictionary<string, object>(); 
}
