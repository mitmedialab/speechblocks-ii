using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;

public class Vocab : MonoBehaviour
{
    public static string NAME_SENSE = "name";

    private bool isInit = false;
    private string phonixScan = null;
    private string swearwordsScan = null;
    private string swearwordInfixScan = null;
    private string swearwordPostfixScan = null;
    private string swearwordPrefixScan = null;
    private string referenceVocabScan = null;
    private string[] suffixes = new string[] { "s", "es", "d", "ed", "ing" };
    private int numberOfImageables = 0;

    private Dictionary<string, List<string>> customNameSenses = new Dictionary<string, List<string>>();
    private HashSet<string> userNameSenses = new HashSet<string>();
    private Dictionary<string, string> minivocab = new Dictionary<string, string>();

    public void SetupMinivocab(Dictionary<string, string> minivocab)
    {
        this.minivocab = minivocab;
        minivocab["jibo"] = "dZ>j|i1>i|b>b|@U0>o";
    }

    public void AddUserNameSenses(IEnumerable<string> userNameSenses)
    {
        this.userNameSenses.UnionWith(userNameSenses);
        AddCustomNameSenses(userNameSenses);
    }

    public void AddCustomNameSenses(IEnumerable<string> customNameSenses)
    {
        foreach (string nameSense in customNameSenses)
        {
            DictUtil.GetOrSpawn(this.customNameSenses, GetWord(nameSense)).Add(nameSense);
        }
    }

    public static string GetWord(string wordOrWordSense)
    {
        return wordOrWordSense.Substring(0, Math.Min(TextUtil.FirstIndexOf(wordOrWordSense, "."), Math.Min(TextUtil.FirstIndexOf(wordOrWordSense, "_"), TextUtil.FirstIndexOf(wordOrWordSense, ":"))));
    }

    public static string GetCoreSense(string wordOrWordSense)
    {
        return wordOrWordSense.Substring(0, Math.Min(TextUtil.FirstIndexOf(wordOrWordSense, ":"), TextUtil.FirstIndexOf(wordOrWordSense, "_")));
    }

    public static string GetNameSenseFromFullName(string fullName)
    {
        int dashLoc = fullName.IndexOf("-");
        if (dashLoc < 0)
        {
            return $"{fullName}.name";
        }
        else
        {
            return $"{fullName.Substring(0, dashLoc)}.name_{fullName.Substring(dashLoc + 1)}";
        }
    }

    public static string GetFullNameFromNameSense(string nameSense)
    {
        int senseLoc = nameSense.IndexOf(".name");
        if (senseLoc < 0)
        {
            Debug.Log("ERROR");
            return nameSense;
        }
        if (senseLoc + 6 >= nameSense.Length)
        {
            return nameSense.Substring(0, senseLoc);
        }
        else
        {
            return $"{nameSense.Substring(0, senseLoc)}-{nameSense.Substring(senseLoc + 6)}";
        }
    }

    public static bool IsInNameSense(string wordSense)
    {
        return null != wordSense && wordSense.Contains(".name");
    }

    public bool IsUserNameSense(string wordSense)
    {
        return userNameSenses.Contains(wordSense);
    }

    public bool IsInMinivocab(string wordOrWordSense)
    {
        string collapsedWord = CollapseCompositeWord(GetWord(wordOrWordSense));
        return minivocab.ContainsKey(collapsedWord);
    }

    public bool IsInVocab(string wordOrWordSense) {
        Init();
        return null != GetMapping(wordOrWordSense);
    }

    public List<string> GetCustomNames()
    {
        return customNameSenses.Keys.ToList();
    }

    public List<string> GetCustomWords()
    {
        return minivocab.Keys.Where(word => !customNameSenses.ContainsKey(word) && null == DScan.LookupDScan(word, phonixScan)).ToList();
    }

    public List<string> GetCustomNameSenses() {
        List<string> outputNameSenses = new List<string>();
        foreach (List<string> nameSenses in customNameSenses.Values)
        {
            outputNameSenses.AddRange(nameSenses);
        }
        outputNameSenses.Sort();
        return outputNameSenses;
    }

    public PGMapping GetMapping(string wordOrWordSense) {
        Init();
        string word = GetWord(wordOrWordSense);
        string collapsedWord = CollapseCompositeWord(word);
        string segmentedWord = SegmentCompositeWord(word);
        List<PGPair> pgs = new List<PGPair>();
        string pcode = null;
        string mappingString = SearchVocab(collapsedWord);
        if (null == mappingString) return null;
        int splitIndex = mappingString.IndexOf(' ');
        if (splitIndex >= 0)
        {
            pgs = PGPair.ParseMapping(mappingString.Substring(0, splitIndex));
            pcode = mappingString.Substring(splitIndex + 1);
            if (!PhonemeUtil.IsValidPhonemeCode(pcode)) return null;
        }
        else
        {
            pgs = PGPair.ParseMapping(mappingString);
            if (null != pgs) { pcode = string.Join(";", pgs.Select(pg => pg.GetPhonemeCode())); }
        }
        if (null == pgs || !PGPair.Validate(word, pgs) || null == pcode) return null;
        return new PGMapping(segmentedWord, collapsedWord, pgs, pcode);
    }

    public string GetIconicImageable(string wordOrWordSense) {
        List<string> alternatives = GetImageableAlternatives(wordOrWordSense);
        if (0 == alternatives.Count) {
            if (IsInNameSense(wordOrWordSense)) return wordOrWordSense;
            return GetWord(wordOrWordSense) + ".noimg";
        }
        else
        {
            if (alternatives.Any(sense => IsInNameSense(sense)) && !alternatives.All(sense => IsInNameSense(sense))) {
                alternatives = alternatives.Where(sense => !IsInNameSense(sense)).ToList();
            }
            return alternatives[0];
        }
    }

    public List<string> GetImageableAlternatives(string seedWordOrWordSense) {
        string collapsedSeed = CollapseCompositeWord(seedWordOrWordSense);
        List<string> imageableRecordEntries = GetImageableRecordEntries(seedWordOrWordSense);
        IEnumerable<string> filteredAlternatives = (collapsedSeed == GetWord(collapsedSeed)) ?
                                                                imageableRecordEntries:
                                                                imageableRecordEntries.Where(sense => SenseMatchesQuery(collapsedQuery: collapsedSeed, sense: sense));
        return filteredAlternatives.Select(sense => ValueSense(sense)).ToList();
    }

    public List<string> GetImageableSenses(string word)
    {
        List<string> imageableSenses = new List<string>();
        List<string> imageableRecordEntries = GetImageableRecordEntries(word);
        imageableSenses.AddRange(imageableRecordEntries.Where(sense => IsInNameSense(sense)).ToList());
        List<string> nonNameSenses = imageableRecordEntries.Where(sense => !IsInNameSense(sense)).ToList();
        if (0 != nonNameSenses.Count)
        {
            List<string> keySenses = nonNameSenses.Where(sense => GetWord(sense) == word).Select(sense => KeySense(sense)).ToList();
            if (0 != keySenses.Count) { imageableSenses.AddRange(keySenses); }
            else { imageableSenses.Add(word); }
        }
        return imageableSenses;
    }

    public int GetNumberOfImageables()
    {
        Init();
        return numberOfImageables;
    }

    public bool IsImageable(string wordOrWordSense) {
        string word = CollapseCompositeWord(GetWord(wordOrWordSense));
        return customNameSenses.ContainsKey(word) || null != DScan.LookupDScan(word, referenceVocabScan);
    }

    public bool IsSwearWord(string wordOrWordSense) {
        Init();
        string word = CollapseCompositeWord(GetWord(wordOrWordSense));
        if (null != DScan.LookupDScan(word, swearwordsScan)) return true;
        foreach (string suffix in suffixes) {
            if (word.EndsWith(suffix)) {
                string subword = word.Substring(0, word.Length - suffix.Length);
                if (null != DScan.LookupDScan(subword, swearwordsScan)) return true;
            }
        }
        for (int i = 1; i <= word.Length; ++i) {
            if (null != DScan.LookupDScan(word.Substring(0, i), swearwordPrefixScan)) return true;
        }
        for (int i = word.Length - 1; i >= 0; --i) {
            if (null != DScan.LookupDScan(word.Substring(i), swearwordPostfixScan)) return true;
        }
        for (int i = 0; i < word.Length; ++i) {
            for (int j = 1; j <= word.Length - i; ++j) {
                if (null != DScan.LookupDScan(word.Substring(i, j), swearwordInfixScan)) return true;
            }
        }
        return false;
    }

    public SynQuery GetPronunciation(string wordOrWordSense, bool giveFullNames = false)
    {
        if (!giveFullNames || !IsInNameSense(wordOrWordSense) || !wordOrWordSense.Contains('_'))
        { return GetWordPronunciation(GetWord(wordOrWordSense)); }
        else
        {
            List<SynQuery> sequence = new List<SynQuery>();
            sequence.Add(GetWordPronunciation(GetWord(wordOrWordSense)));
            string secondName = wordOrWordSense.Substring(wordOrWordSense.IndexOf('_') + 1);
            foreach (string namepart in secondName.Split('-'))
            {
                sequence.Add(" ");
                if (IsInVocab(namepart))
                {
                    sequence.Add(GetWordPronunciation(namepart));
                }
                else
                {
                    sequence.Add(SynQuery.Spell(namepart));
                }
            }
            return SynQuery.Seq(sequence);
        }
    }

    private void Init()
    {
        if (isInit) return;
        isInit = true;
        phonixScan = LoadScan("Models/phonix-acapela");
        InitSwearwords();
        referenceVocabScan = LoadScan("Models/reference-dictionary");
        numberOfImageables = referenceVocabScan.Count(c => c == '\n');
    }

    private void InitSwearwords() {
        swearwordsScan = LoadScan("Models/swearwords");
        swearwordPrefixScan = LoadScan("Models/swearwords-prefix");
        swearwordInfixScan = LoadScan("Models/swearwords-infix");
        swearwordPostfixScan = LoadScan("Models/swearwords-postfix");
    }

    private string LoadScan(string path) {
        TextAsset scanAsset = Resources.Load<TextAsset>(path);
        return scanAsset.text;
    }

    private string SearchVocab(string collapsedWord)
    {
        string mappingString;
        if (minivocab.TryGetValue(collapsedWord, out mappingString)) { return mappingString; }
        return DScan.LookupDScan(collapsedWord, phonixScan);
    }

    private List<string> GetImageableRecordEntries(string wordOrWordSense) {
        List<string> imageableRecordEntries = new List<string>();
        if (!IsInVocab(wordOrWordSense)) return imageableRecordEntries;
        string word = CollapseCompositeWord(GetWord(wordOrWordSense));
        string record = DScan.LookupDScan(word, referenceVocabScan);
        if (null != record) { imageableRecordEntries.AddRange(record.Split(',')); }
        if (customNameSenses.ContainsKey(word)) {
            HashSet<string> keySenses = new HashSet<string>(imageableRecordEntries.Select(KeySense));
            imageableRecordEntries.AddRange(customNameSenses[word].Where(sense => !keySenses.Contains(sense)));
        }
        return imageableRecordEntries;
    }

    public static bool SenseMatchesQuery(string collapsedQuery, string sense)
    {
        int indexOfColon = sense.IndexOf(':');
        if (indexOfColon >= 0)
        {
            sense = sense.Substring(0, indexOfColon);
        }
        if (collapsedQuery.Contains('_'))
        {
            return sense == collapsedQuery;
        }
        else if (collapsedQuery.Contains('.'))
        {
            return sense.Substring(0, TextUtil.FirstIndexOf(sense, "_")) == collapsedQuery;
        }
        else
        {
            return collapsedQuery == CollapseCompositeWord(GetWord(sense));
        }
    }

    private string KeySense(string compoundSense)
    {
        return compoundSense.Substring(0, TextUtil.FirstIndexOf(compoundSense, ":"));
    }

    private string ValueSense(string compoundSense)
    {
        if (!compoundSense.Contains(':')) { return compoundSense; }
        return compoundSense.Substring(compoundSense.IndexOf(':') + 1);
    }

    public static string CollapseCompositeWord(string compositeWord)
    {
        if (!compositeWord.Contains("-")) return compositeWord;
        return compositeWord.Replace("-", "");
    }

    public static string SpaceCompositeWord(string compositeWord)
    {
        return compositeWord.Replace("-", " ");
    }

    private string SegmentCompositeWord(string word)
    {
        if (word.Contains("-")) return word;
        string referenceRecord = DScan.LookupDScan(word, referenceVocabScan);
        if (null == referenceRecord) return word;
        string segmentation = referenceRecord.Split(',').Select(GetWord).FirstOrDefault(refWord => refWord.Replace("-", "") == word);
        if (null == segmentation) return word;
        return segmentation;
    }

    private SynQuery GetWordPronunciation(string compositeWord)
    {
        string collapsedWord = CollapseCompositeWord(compositeWord);
        if (minivocab.ContainsKey(collapsedWord))
        {
            PGMapping mapping = GetMapping(compositeWord);
            if (null != mapping)
            {
                return SynQuery.SayAs(SpaceCompositeWord(mapping.compositeWord), mapping.phonemecode);
            }
        }
        return SpaceCompositeWord(SegmentCompositeWord(compositeWord));
    }
}
