using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SimpleJSON;

public class PhonemeUtil {
    private static Dictionary<string, AudioClip> phonemeSounds = new Dictionary<string, AudioClip>();
    private static Dictionary<string[], string[]> graphemeBreakdowns = new Dictionary<string[], string[]>();
    private static char[] vowelLetters = { 'a', 'o', 'i', 'e', 'u' };
    private static List<string> consPhonemes = new List<string>();
    private static List<string> vowlPhonemes = new List<string>();

    public static Color RED = new Color(1, 0, 0);
    public static Color BLUE = new Color(0, 172f / 255, 230f / 255);

    public static Color ColorForLetter(char letr) {
        if (IsVowelLetter(letr)) {
            return new Color(RED.r, RED.g, RED.b);
        } else {
            return new Color(BLUE.r, BLUE.g, BLUE.b);
        }
    }

    public static bool HasAccent(string phoneme) {
        return phoneme.Length > 1 && char.IsDigit(phoneme[phoneme.Length - 1]);
    }

    public static bool IsValidPhonemeCode(string phonemeCode)
    {
        if (null == phonemeCode) return false;
        string[] phonemes = phonemeCode.Split(';');
        foreach (string phoneme in phonemes)
        {
            if (HasAccent(phoneme))
            {
                char accent = phoneme[phoneme.Length - 1];
                if ('0' != accent && '1' != accent && '2' != accent) return false;
            }
            if (null == Config.GetConfig("PhonemeConfig")[Unaccentuated(phoneme)]) return false;
        }
        return true;
    }

    public static string GetAccent(string phoneme)
    {
        if (char.IsDigit(phoneme[phoneme.Length - 1])) return phoneme.Substring(phoneme.Length - 1);
        return "";
    }

    public static string Unaccentuated(string phonemecode)
    {
        if (!phonemecode.Any(chr => char.IsDigit(chr))) return phonemecode;
        return string.Join("", phonemecode.Where(chr => !char.IsDigit(chr)));
    }

    public static bool IsVowelPhoneme(string phoneme) {
        JSONNode config = GetSuitableConfig(phoneme);
        if (null == config) return false;
        return config["c_or_v"] == "VOWL";
    }

    public static bool HasConsonantAspect(string phoneme)
    {
        return !IsVowelPhoneme(phoneme) || phoneme == "r=";
    }

    public static bool HasConsonantAspect(PGPair pgPair)
    {
        return pgPair.GetPhonemes().Any(ph => HasConsonantAspect(ph));
    }

    public static List<string> PhonemesFromGroups(IEnumerable<string> phGroups) {
        List<string> phonemes = new List<string>();
        foreach (string phGroup in phGroups)
        {
            phonemes.AddRange(phGroup.Split(';'));
        }
        return phonemes;
    }

    public static List<string> GetVowelPhonemes() {
        InitConsAndVowlLists();
        return new List<string>(vowlPhonemes);
    }

    public static List<string> GetConsonantPhonemes() {
        InitConsAndVowlLists();
        return new List<string>(consPhonemes);
    }

    public static List<string> GetConsonantLetters() {
        List<string> consonants = new List<string>();
        JSONNode letterConfig = Config.GetConfig("LettersConfig");
        foreach (string key in letterConfig.Keys)
        {
            if (!IsVowelPhoneme(letterConfig[key]["sounds"]))
            {
                consonants.Add(key);
            }
        }
        return consonants;
    }

    public static bool IsVowelLetter(char letter) {
        return vowelLetters.Contains(char.ToLower(letter));
    }

    public static bool CanBeVowelLetter(char letter)
    {
        letter = char.ToLower(letter);
        return 'y' == letter || vowelLetters.Contains(letter);
    }

    public static bool CanActAsVowel(string phonemecode)
    {
        return IsVowelPhoneme(phonemecode) || "w" == phonemecode;
    }

    public static bool IsWellAccentuated(IEnumerable<string> phonemes) {
        bool vowelsHaveAccents = phonemes.All(ph => HasAccent(ph) || !IsVowelPhoneme(ph));
        if (!vowelsHaveAccents) return false;
        return phonemes.Any(ph => ph.EndsWith("1")) || phonemes.All(ph => !IsVowelPhoneme(ph));
    }

    public static string GetDefaultPhoneme(string letter)
    {
        JSONNode letterConfig = Config.GetConfig("LettersConfig")[letter];
        if (null == letterConfig) return null;
        return letterConfig["sounds"][0];
    }

    public static string LetterName(string letter)
    {
        JSONNode letterConfig = Config.GetConfig("LettersConfig")[letter];
        if (null == letterConfig) return null;
        return (string)letterConfig["name"];
    }

    public static string GetDefaultGrapheme(string phoneme) {
        JSONNode phonemeConfig = GetSuitableConfig(phoneme);
        if (null == phonemeConfig) return null;
        return phonemeConfig["common-spellings"][0];
    }

    public static bool IsCommonSpelling(string phoneme, string grapheme)
    {
        JSONNode phonemeConfig = GetSuitableConfig(phoneme);
        if (null == phonemeConfig) return false;
        JSONNode commonSpellings = phonemeConfig["common-spellings"];
        if (null == commonSpellings) return false;
        for (int i = 0; i < commonSpellings.Count; ++i) { if (grapheme == commonSpellings[i]) return true; }
        return false;
    }

    public static bool IsInventedSpelling(string phoneme, string grapheme)
    {
        JSONNode phonemeConfig = GetSuitableConfig(phoneme);
        if (null == phonemeConfig) return false;
        JSONNode inventedSpellings = phonemeConfig["invented-spellings"];
        if (null == inventedSpellings) return false;
        for (int i = 0; i < inventedSpellings.Count; ++i) { if (grapheme == inventedSpellings[i]) return true; }
        return false;
    }

    public static List<string> GetPlausibleSpellings(string phoneme)
    {
        List<string> spellings = new List<string>();
        JSONNode phonemeConfig = GetSuitableConfig(phoneme);
        if (null == phonemeConfig) { return spellings; }
        JSONNode commonSpellings = phonemeConfig["common-spellings"];
        if (null != commonSpellings) {
            for (int i = 0; i < commonSpellings.Count; ++i) { spellings.Add(commonSpellings[i]); }
        }
        JSONNode inventedSpellings = phonemeConfig["invented-spellings"];
        if (null != inventedSpellings) {
            for (int i = 0; i < inventedSpellings.Count; ++i) { spellings.Add(inventedSpellings[i]); }
        }
        return spellings;
    }

    public static List<string> GetCoreGraphemes(string phoneme)
    {
        List<string> coreGraphemes = new List<string>();
        JSONNode phonemeConfig = GetSuitableConfig(phoneme);
        if (null == phonemeConfig) { return coreGraphemes; }
        JSONNode commonSpellings = phonemeConfig["common-spellings"];
        if (null != commonSpellings)
        {
            for (int i = 0; i < commonSpellings.Count; ++i) { coreGraphemes.Add(commonSpellings[i]); }
        }
        JSONNode inventedSpellings = phonemeConfig["extra-spellings"];
        if (null != inventedSpellings)
        {
            for (int i = 0; i < inventedSpellings.Count; ++i) { coreGraphemes.Add(inventedSpellings[i]); }
        }
        return coreGraphemes;
    }

    public static string[] GetGraphemeBreakdown(string phoneme, string grapheme, bool useDefault = true)
    {
        phoneme = Unaccentuated(phoneme);
        string[] key = new string[] { phoneme, grapheme };
        if (graphemeBreakdowns.ContainsKey(key))
        {
            return graphemeBreakdowns[key];
        }
        List<string> coreGraphemes = GetCoreGraphemes(phoneme);
        if (null == coreGraphemes) return useDefault ? new string[] { "", grapheme, "" } : null;
        int max_length = 0;
        string max_grapheme = null;
        int position = 0;
        foreach (string coreGrapheme in coreGraphemes)
        {
            if (coreGrapheme.Length <= max_length) continue;
            int coreGraphemePos = grapheme.IndexOf(coreGrapheme);
            if (coreGraphemePos >= 0)
            {
                max_grapheme = coreGrapheme;
                position = coreGraphemePos;
                max_length = coreGrapheme.Length;
            }
        }
        if (null == max_grapheme) return useDefault ? new string[] { "", grapheme, "" } : null;
        string[] breakdown = new string[] { grapheme.Substring(0, position),
                                            max_grapheme,
                                            grapheme.Substring(position + max_grapheme.Length) };
        graphemeBreakdowns[key] = breakdown;
        return breakdown;
    }

    public static List<string> Accentuate(IEnumerable<string> phonemes) {
        List<string> accentuated = new List<string>();
        bool vowelEncountered = false;
        foreach (string ph in phonemes)
        {
            string phoneme = Unaccentuated(ph);
            if (Config.GetConfig("PhonemeConfig")[phoneme]["c_or_v"].Equals("VOWL"))
            {
                if (!vowelEncountered)
                {
                    phoneme = phoneme + "1";
                    vowelEncountered = true;
                } else {
                    phoneme = phoneme + "0";
                }
            }
            accentuated.Add(phoneme);
        }
        return accentuated;
    }

    public static string GetAccentuatedPhonemeCode(IEnumerable<string> phGroups) {
        List<string> phonemes = PhonemesFromGroups(phGroups);
        phonemes = Accentuate(phonemes);
        return string.Join(";", phonemes);
    }

    public static AudioClip SoundOf(string phoneme) {
        if (null == phoneme || "" == phoneme) return null;
        phoneme = Unaccentuated(phoneme);
        if (!phonemeSounds.ContainsKey(phoneme))
        {
            JSONNode phonemeConfig = Config.GetConfig("PhonemeConfig")[phoneme];
            if (null != phonemeConfig)
            {
                string phonemeFile = Config.GetConfig("PhonemeConfig")[phoneme]["file"];
                AudioClip sound = Resources.Load<AudioClip>("Phonemes/" + phonemeFile);
                phonemeSounds[phoneme] = sound;
                return sound;
            }
            else
            {
                phonemeSounds[phoneme] = null;
                return null;
            }
        }
        else
        {
            return phonemeSounds[phoneme];
        }
    }

    public static List<AudioClip> SoundSequence(string phonemecode)
    {
        List<AudioClip> sequence = new List<AudioClip>();
        AudioClip seqSound = SoundOf(phonemecode);
        if (null != seqSound)
        {
            sequence.Add(seqSound);
            return sequence;
        }
        else
        {
            string[] phonemes = phonemecode.Split(';');
            foreach (string phoneme in phonemes)
            {
                sequence.Add(SoundOf(phoneme));
            }
            return sequence;
        }
    }

    public static Dictionary<string, object> LoadPhonemeConversion(string assetName)
    {
        Dictionary<string, object> phonemeConversion = new Dictionary<string, object>();
        TextAsset mappingFile = Resources.Load<TextAsset>(assetName);
        string[] lines = mappingFile.text.Split('\n');
        foreach (string line in lines)
        {
            string trimline = line.Trim();
            if (0 == trimline.Length) continue;
            string[] mapping_entry = trimline.Split(' ');
            string[] phonemes = mapping_entry[0].Split(';');
            Dictionary<string, object> treeNode = phonemeConversion;
            foreach (string phoneme in phonemes)
            {
                if (!treeNode.ContainsKey(phoneme))
                {
                    Dictionary<string, object> newNode = new Dictionary<string, object>();
                    treeNode[phoneme] = newNode;
                    treeNode = newNode;
                }
                else
                {
                    treeNode = (Dictionary<string, object>)treeNode[phoneme];
                }
            }
            treeNode[""] = mapping_entry[1].Replace(';', ',');
        }
        Resources.UnloadAsset(mappingFile);
        return phonemeConversion;
    }

    public static string ConvertPhonemes(Dictionary<string, object> phonemeConversion, string phonemecode)
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (!ConvertPhonemes(phonemeConversion, phonemecode.Split(';'), stringBuilder)) return null;
        return stringBuilder.ToString();
    }

    private static bool ConvertPhonemes(Dictionary<string, object> phonemeConversion, string[] phonemes, StringBuilder stringBuilder)
    {
        int position = 0;
        string[] basicPhonemes = phonemes.Select(ph => Unaccentuated(ph)).ToArray();
        while (position < basicPhonemes.Length)
        {
            int out_position;
            string jibo_code = GetConvCode(phonemeConversion, basicPhonemes, position, out out_position);
            if (null == jibo_code) return false;
            if (0 != stringBuilder.Length) { stringBuilder.Append(";"); }
            stringBuilder.Append(jibo_code);
            string last_phoneme = phonemes[out_position - 1];
            char last_char = last_phoneme[last_phoneme.Length - 1];
            if (char.IsDigit(last_char)) { stringBuilder.Append(last_char); }
            position = out_position;
        }
        return true;
    }

    private static string GetConvCode(Dictionary<string, object> treeNode, string[] phonemes, int in_position, out int out_position)
    {
        if (in_position < phonemes.Length && treeNode.ContainsKey(phonemes[in_position]))
        {
            string lookupCode = GetConvCode((Dictionary<string, object>)treeNode[phonemes[in_position]], phonemes, in_position + 1, out out_position);
            if (null != lookupCode) return lookupCode;
        }
        out_position = in_position;
        if (treeNode.ContainsKey("")) return (string)treeNode[""];
        return null;
    }

    private static void InitConsAndVowlLists() {
        if (0 != consPhonemes.Count) return;
        JSONNode phonemeConfig = Config.GetConfig("PhonemeConfig");
        foreach (string key in phonemeConfig.Keys) {
            if (phonemeConfig[key]["c_or_v"].Equals("VOWL")) {
                vowlPhonemes.Add(key);
            } else {
                consPhonemes.Add(key);
            }
        }
    }

    private static JSONNode GetSuitableConfig(string phoneme)
    {
        phoneme = Unaccentuated(phoneme);
        JSONNode phonemeConfig = Config.GetConfig("PhonemeConfig")[phoneme];
        if (null != phonemeConfig) return phonemeConfig;
        if (!phoneme.StartsWith("V;")) return null;
        phonemeConfig = Config.GetConfig("PhonemeConfig")[phoneme.Substring(2)];
        if (null != phonemeConfig) return phonemeConfig;
        return null;
    }
}