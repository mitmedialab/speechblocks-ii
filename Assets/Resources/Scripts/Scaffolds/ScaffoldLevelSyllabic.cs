using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScaffoldLevelSyllabic : IScaffoldLevel
{
    private PGMapping target = null;
    private int[] syllableBreakdown = null;
    private int targetPGSlot = 0;
    private int targetSyllable = 0;
    private int targetSyllableStart = 0;
    private int targetSyllableEnd = 0;
    private SynQuery requestedPieceCode = null;
    private bool requestingFirstSound = true;
    private bool onePGSyllableCase = false;
    private string requestedPieceType = null;
    private Vocab vocab;

    private SynQuery BREAK = SynQuery.Break(0.25f);
    private SynQuery MINIBREAK = SynQuery.Break(0.05f);

    public ScaffoldLevelSyllabic()
    {
        vocab = GameObject.FindWithTag("StageObject").GetComponent<Vocab>();
    }

    public SynQuery Prompt(PGMapping target, int[] syllableBreakdown, int targetPGSlot, bool giveQuestion, int scaffolderTargetID, int scaffoldingInteractionID)
    {
        Logging.LogScaffoldingPromptLevel("syllabic", scaffolderTargetID, scaffoldingInteractionID, targetPGSlot, giveQuestion);
        bool newTarget = this.target != target;
        this.target = target;
        this.syllableBreakdown = syllableBreakdown;
        this.targetPGSlot = targetPGSlot;
        int oldTargetSyllable = targetSyllable;
        DetermineTargetSyllable();
        List<SynQuery> mainSequence = new List<SynQuery>();
        if (syllableBreakdown.Length > 1)
        {
            if (newTarget && !IsAcronym(target))
            {
                mainSequence.Add(MakeSyllabificationPrompt());
                mainSequence.Add(BREAK);
            }
        }
        if (onePGSyllableCase)
        {
            requestedPieceCode = GetCodeFor(targetSyllableStart, targetSyllableEnd, true);
            requestedPieceType = "sound";
            requestingFirstSound = true;
        }
        else if (IsInfixCase())
        {
            SynQuery targetSyllableCode = GetCodeFor(targetSyllableStart, targetSyllableEnd, true);
            requestedPieceCode = GetCodeFor(targetPGSlot, targetSyllableEnd, true);
            requestedPieceType = "part";
            requestingFirstSound = true;
            mainSequence.Add(SynQuery.Format("What's left of {{0}} is {{1}}.", targetSyllableCode, requestedPieceCode));
            mainSequence.Add(BREAK);
        }
        else
        {
            if (1 == syllableBreakdown.Length)
            {
                requestedPieceCode = vocab.GetPronunciation(target.compositeWord);
                requestedPieceType = "word" ;
            }
            else
            {
                requestedPieceCode = GetCodeFor(targetSyllableStart, targetSyllableEnd, true);
                requestedPieceType = "syllable";
            }
            requestingFirstSound = (targetPGSlot == targetSyllableStart);
        }
        if (giveQuestion)
        {
            SynQuery question = MakeQuestion();
            if (null != question)
            {
                mainSequence.Add(BREAK);
                mainSequence.Add(question);
            }
        }
        return SynQuery.Seq(mainSequence);
    }

    public SynQuery GetRequestedPieceCode()
    {
        return requestedPieceCode;
    }

    public string GetRequestedPieceType()
    {
        return requestedPieceType;
    }

    public bool IsRequestingFirstSound()
    {
        return requestingFirstSound;
    }

    public bool IsOnePGSyllableCase()
    {
        return onePGSyllableCase;
    }

    private bool IsAcronym(PGMapping mapping)
    {
        return mapping.pgs.All(pg => PhonemesMatchLetterName(pg));
    }

    private bool PhonemesMatchLetterName(PGPair pg)
    {
        string g = pg.GetGrapheme();
        if (1 != g.Length) return false;
        return PhonemeUtil.Unaccentuated(PhonemeUtil.LetterName(g)) == pg.GetUnaccentuatedPhonemeCode();
    }

    private void DetermineTargetSyllable()
    {
        targetSyllableStart = 0;
        for (targetSyllable = 0; targetSyllable < syllableBreakdown.Length; ++targetSyllable)
        {
            if (targetSyllableStart + syllableBreakdown[targetSyllable] > targetPGSlot)
            {
                targetSyllableEnd = targetSyllableStart + syllableBreakdown[targetSyllable];
                onePGSyllableCase = (1 == targetSyllableEnd - targetSyllableStart);
                return;
            }
            targetSyllableStart += syllableBreakdown[targetSyllable];
        }
    }

    private SynQuery MakeSyllabificationPrompt()
    {
        SynQuery wordCode = vocab.GetPronunciation(target.compositeWord);
        List<SynQuery> syllableQueries = new List<SynQuery>();
        int syllableStart = 0;
        for (int i = 0; i < syllableBreakdown.Length; ++i)
        {
            syllableQueries.Add(BREAK);
            syllableQueries.Add(GetCodeFor(syllableStart, syllableStart + syllableBreakdown[i], false));
            syllableStart += syllableBreakdown[i];
        }
        SynQuery syllablesSeq = SynQuery.Seq(syllableQueries);
        int dice = RandomUtil.Range("scaf-syl-syls1", 0, 2);
        switch (dice)
        {
            case 0:
                return SynQuery.Format($"{{0}} has syllables: {{1}}. ", wordCode, syllablesSeq);
            default:
                return SynQuery.Format($"Here are the syllables in {{0}}: {{1}}. ", wordCode, syllablesSeq);
        }
    }

    private bool IsInfixCase()
    {
        return targetSyllableStart < targetPGSlot && targetPGSlot < targetSyllableEnd - 1;
    }

    private SynQuery MakeQuestion()
    {
        string when = (0 == targetPGSlot) ? "first" : "now";
        if (onePGSyllableCase)
        {
            string should = (0 == RandomUtil.Range("scaf-syl-one-pg1", 0, 2)) ? "should" : "need to";
            SynQuery targetSyllableCode = GetCodeFor(targetSyllableStart, targetSyllableEnd, true);
            return SynQuery.Format($"{when} we {should} find what makes the sound {{0}}. ", targetSyllableCode);
        }
        else
        {
            int dice = RandomUtil.Range("scaf-syl-q2", 0, 2);
            string weNeedStarter = RandomUtil.PickOne("scaf-syl-q3", ScaffoldUtils.WE_NEED_STARTERS);
            switch (dice)
            {
                case 0:
                    string startOrEnd = requestingFirstSound ? RandomUtil.PickOne("scaf-syl-q4", ScaffoldUtils.START) : RandomUtil.PickOne("scaf-syl-q5", ScaffoldUtils.END);
                    return SynQuery.Format($"{when} {weNeedStarter.ToLower()} the sound that the {requestedPieceType} {{0}} {startOrEnd}s with. ", requestedPieceCode);
                default:
                    string firstOrLast = requestingFirstSound ? "first" : "last";
                    return SynQuery.Format($"{when} {weNeedStarter.ToLower()} the {firstOrLast} sound in the {requestedPieceType} {{0}}. ", requestedPieceCode);
            }
        }
    }

    private SynQuery GetCodeFor(int start, int end, bool putMinibreak)
    {
        List<PGPair> pgs = target.pgs.Skip(start).Take(end - start).ToList();
        SynQuery code = SynQuery.SayAs(PGPair.Word(pgs), PGPair.AccentuatedPhonemeCode(pgs));
        if (1 == end - start)
        {
            code = SynQuery.Seq(BREAK, code, BREAK);
        }
        else if (putMinibreak)
        {
            code = SynQuery.Seq(MINIBREAK, code, MINIBREAK);
        }
        return SynQuery.Rate(code, 0.9f);
    }
}