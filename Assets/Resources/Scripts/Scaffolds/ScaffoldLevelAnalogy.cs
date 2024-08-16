using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SimpleJSON;

public class ScaffoldLevelAnalogy : IScaffoldLevel
{
    private Vocab vocab = null;
    ScaffoldLevelSyllabic syllabicLevel = null;

    private const int MAX_SAMPLES = 3;
    private SynQuery BREAK = SynQuery.Break(0.25f);

    public ScaffoldLevelAnalogy(ScaffoldLevelSyllabic syllabicLevel)
    {
        this.syllabicLevel = syllabicLevel;
        vocab = GameObject.FindWithTag("StageObject").GetComponent<Vocab>();
    }

    public SynQuery Prompt(PGMapping target, int[] syllableBreakdown, int targetPGSlot, bool giveQuestion, int scaffolderTargetID, int scaffoldingInteractionID)
    {
        if (!giveQuestion) return null; // skip this level when it is not the terminal level in the hint
        Logging.LogScaffoldingPromptLevel("analogy", scaffolderTargetID, scaffoldingInteractionID, targetPGSlot, giveQuestion);
        SynQuery analogy = FormAnalogy(target.pgs[targetPGSlot].GetUnaccentuatedPhonemeCode(), target.collapsedWord);
        if (null == analogy) return null;
        SynQuery prompt = SynQuery.Format($"{{0}} {{1}}", DescribeWhatWeNeed(), analogy);
        return prompt;
    }

    private SynQuery DescribeWhatWeNeed()
    {
        if (syllabicLevel.IsOnePGSyllableCase())
        {
            return RandomUtil.PickOne("scaf-tsound-onesyll", ScaffoldUtils.WE_NEED_STARTERS);
        }
        else
        {
            int dice = RandomUtil.Range("scaf-tsound-sound1", 0, 2);
            SynQuery requestedPieceCode = syllabicLevel.GetRequestedPieceCode();
            bool requestingFirstSound = syllabicLevel.IsRequestingFirstSound();
            string requestedPieceType = syllabicLevel.GetRequestedPieceType();
            switch (dice)
            {
                case 0:
                    string startOrEnd = requestingFirstSound ? RandomUtil.PickOne("scaf-tsound-sound2", ScaffoldUtils.START) : RandomUtil.PickOne("scaf-tsound-sound3", ScaffoldUtils.END);
                    return SynQuery.Format($"{{0}} {startOrEnd}s with", requestedPieceCode);
                default:
                    string firstOrLast = requestingFirstSound ? "first" : "last";
                    return SynQuery.Format($"The {firstOrLast} sound in the {requestedPieceType} {{0}} is ", requestedPieceCode);
            }
        }
    }

    private SynQuery FormAnalogy(string targetPhoneme, string targetWord)
    {
        List<string> prefixSamples = new List<string>();
        List<string> infixSamples = new List<string>();
        List<string> postfixSamples = new List<string>();
        GetAnalogousWords(targetPhoneme, targetWord, prefixSamples, infixSamples, postfixSamples);
        if (0 != prefixSamples.Count)
        {
            string sOrNone = 1 == prefixSamples.Count ? "s" : "";
            return SynQuery.Format($"the same sound that {{0}} start{sOrNone} with.", ProvideSamples(prefixSamples));
        }
        else if (0 != postfixSamples.Count)
        {
            string sOrNone = 1 == prefixSamples.Count ? "s" : "";
            return SynQuery.Format($"the same sound that {{0}} end{sOrNone} with.", ProvideSamples(postfixSamples));
        }
        else if (0 != infixSamples.Count)
        {
            return SynQuery.Format("the same sound that is in the middle of {0}.", ProvideSamples(infixSamples));
        }
        else
        {
            return null;
        }
    }

    private SynQuery ProvideSamples(List<string> targetSamples)
    {
        if (targetSamples.Count > MAX_SAMPLES)
        {
            targetSamples = RandomUtil.Shuffle("scaf-analog-samples1", targetSamples).Take(MAX_SAMPLES).ToList();
        }
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(targetSamples[0]);
        for (int i = 1; i < targetSamples.Count - 1; ++i)
        {
            stringBuilder.Append(", ");
            stringBuilder.Append(targetSamples[i]);
        }
        if (targetSamples.Count > 1)
        {
            stringBuilder.Append(", and ");
            stringBuilder.Append(targetSamples[targetSamples.Count - 1]);
        }
        return SynQuery.Seq(SynQuery.Break(0.001f), SynQuery.Rate(stringBuilder.ToString(), 0.8f), SynQuery.Break(0.001f));
    }

    private void GetAnalogousWords(string targetPhoneme, string targetWord, List<string> prefixSamples, List<string> infixSamples, List<string> postfixSamples)
    {
        JSONNode phonemesConfig = Config.GetConfig("PhonemeConfig");
        JSONNode thePhonemeConfig = phonemesConfig[targetPhoneme];
        if (null == thePhonemeConfig) return;
        JSONArray samplesArray = (JSONArray)thePhonemeConfig["samples"];
        foreach (JSONNode sampleNode in samplesArray)
        {
            string sample = (string)sampleNode;
            if (targetWord == sample) continue;
            PGMapping sampleMapping = vocab.GetMapping(sample);
            if (null == sampleMapping) continue; // the example is no longer in the dictionary
            if (sampleMapping.pgs[0].GetUnaccentuatedPhonemeCode() == targetPhoneme)
            {
                prefixSamples.Add(sample);
            }
            else if (sampleMapping.pgs[sampleMapping.pgs.Count - 1].GetUnaccentuatedPhonemeCode() == targetPhoneme)
            {
                postfixSamples.Add(sample);
            }
            else if (sampleMapping.pgs.Count <= 3)
            {
                infixSamples.Add(sample);
            }
        }
    }
}