using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using System.IO;

public class Syllabifier : MonoBehaviour
{
    private Vocab vocab;
    private string customSyllScan;

    void Start()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        vocab = stageObject.GetComponent<Vocab>();
        customSyllScan = Resources.Load<TextAsset>("Models/custom-syll").text;
        //TestSyllabification();
    }

    public int[] Syllabify(string word, List<PGPair> mapping)
    {
        //string word = PGPair.Word(mapping);
        //int[] recordedSyllabification = vocab.GetSyllableBreakdown(word);
        //if (null != recordedSyllabification) return recordedSyllabification;
        string recordedSyll = DScan.LookupDScan(word.Replace("-", ""), customSyllScan);
        if (null != recordedSyll) { word = recordedSyll; }
        return DefaultSyllabification(word, mapping);
    }

    public static string Breakdown(List<PGPair> mapping, int[] syllabification)
    {
        StringBuilder stringBuilder = new StringBuilder();
        int position = 0;
        for (int i = 0; i < syllabification.Length; ++i)
        {
            if (position > 0) { stringBuilder.Append("-"); };
            stringBuilder.Append(string.Join("", mapping.Skip(position).Take(syllabification[i]).Select(pg => pg.GetGrapheme())));
            position += syllabification[i];
        }
        return stringBuilder.ToString();
    }

    private int[] DefaultSyllabification(string word, List<PGPair> mapping)
    {
        List<int> wordBoundaries = FindSubwordBoundaries(word, mapping);
        List<int> syllableLengths = new List<int>();
        int currentPosition = 0;
        while (currentPosition < mapping.Count)
        {
            int nextSyllableLength = FindNextSyllableLength(mapping, currentPosition, wordBoundaries);
            syllableLengths.Add(nextSyllableLength);
            currentPosition += nextSyllableLength;
        }
        return syllableLengths.ToArray();
    }

    private List<int> FindSubwordBoundaries(string word, List<PGPair> mapping)
    {
        List<int> lengths = word.Split('-').Select(subword => subword.Length).ToList();
        int lengthI = 0;
        List<int> boundaries = new List<int>();
        for (int i = 0; i < mapping.Count; ++i)
        {
            lengths[lengthI] -= mapping[i].GetGrapheme().Length;
            if (0 == lengths[lengthI])
            {
                lengthI += 1;
                boundaries.Add(i + 1);
            }
        }
        return boundaries;
    }

    private int FindNextVowel(List<PGPair> mapping, int start)
    {
        for (int i = start; i < mapping.Count; ++i)
        {
            if (mapping[i].GetPhonemes().Any(ph => PhonemeUtil.IsVowelPhoneme(ph))) return i;
        }
        return -1;
    }

    private int FindNextSyllableLength(List<PGPair> mapping, int start, List<int> wordBoundaries)
    {
        int thisVowel = FindNextVowel(mapping, start);
        if (thisVowel < 0) return mapping.Count - start;
        int nextVowel = FindNextVowel(mapping, thisVowel + 1);
        if (nextVowel < 0) return mapping.Count - start;
        int wordBoundary = FindWordBoundary(wordBoundaries, thisVowel, nextVowel);
        if (wordBoundary > start) return wordBoundary - start;
        if (nextVowel - thisVowel > 2) // this section is to account for words like PENGUIN, where [w] acts like a vowel
        {
            for (int i = nextVowel - 1; i > thisVowel; --i)
            {
                if (PhonemeUtil.CanActAsVowel(mapping[i].GetPhonemeCode()))
                {
                    nextVowel = i;
                }
            }
        }
        string thisGrapheme = mapping[thisVowel].GetGrapheme();
        if (nextVowel - thisVowel > 2 && PhonemeUtil.CanBeVowelLetter(thisGrapheme[thisGrapheme.Length - 1])) // the second condition is added to handle words like bu-tter-fly (otherwise would be bu-tterf-ly)
        {
            return nextVowel - 1 - start;
        }
        else
        {
            return thisVowel + 1 - start;
        }
    }

    private int FindWordBoundary(List<int> wordBoundaries, int start, int end)
    {
        for (int i = start + 1; i <= end; ++i)
        {
            if (wordBoundaries.Contains(i)) return i;
        }
        return -1;
    }

    //private void TestSyllabification()
    //{
    //    TextAsset refdictAsset = Resources.Load<TextAsset>("Models/reference-dictionary");
    //    string[] lines = refdictAsset.text.Split('\n');
    //    StreamWriter streamWriter = new StreamWriter($"{Application.persistentDataPath}/syllable_test.txt");
    //    foreach (string line in lines)
    //    {
    //        string trim_line = line.Trim();
    //        if (0 == trim_line.Length) continue;
    //        string word = trim_line.Substring(0, trim_line.IndexOf(' '));
    //        PGMapping pgMapping = vocab.GetMapping(word);
    //        if (null != pgMapping)
    //        {
    //            List<PGPair> pgs = pgMapping.pgs;
    //            try
    //            {
    //                int[] syllabification = Syllabify(word, pgs);
    //                streamWriter.WriteLine(Breakdown(pgs, syllabification));
    //            }
    //            catch
    //            {
    //                streamWriter.WriteLine($"PROBLEM WORD: {word}");
    //            }
    //        }
    //        else
    //        {
    //            streamWriter.WriteLine($"MISSING WORD: {word}");
    //        }
    //    }
    //    streamWriter.Flush();
    //    streamWriter.Close();
    //}
}
