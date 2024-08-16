using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScaffoldLevelTargetSound : IScaffoldLevel
{
    private ScaffoldLevelSyllabic syllabicLevel = null;

    private SynQuery BREAK = SynQuery.Break(0.25f);

    public ScaffoldLevelTargetSound(ScaffoldLevelSyllabic syllabicLevel)
    {
        this.syllabicLevel = syllabicLevel;
    }

    public SynQuery Prompt(PGMapping target, int[] syllableBreakdown, int targetPGSlot, bool giveQuestion, int scaffolderTargetID, int scaffoldingInteractionID)
    {
        if (syllabicLevel.IsOnePGSyllableCase()) return null;
        Logging.LogScaffoldingPromptLevel("target-sound", scaffolderTargetID, scaffoldingInteractionID, targetPGSlot, giveQuestion);
        PGPair targetPG = target.pgs[targetPGSlot];
        List<SynQuery> mainSequence = new List<SynQuery>();
        SynQuery targetSoundQuery = SynQuery.SayAs(targetPG.GetGrapheme(), targetPG.GetUnaccentuatedPhonemeCode());
        mainSequence.Add(GiveTheTargetSound(targetSoundQuery));
        return SynQuery.Seq(mainSequence);
    }

    private SynQuery GiveTheTargetSound(SynQuery targetSoundQuery)
    {
        int dice = RandomUtil.Range("scaf-tsound-sound1", 0, 2);
        SynQuery requestedPieceCode = syllabicLevel.GetRequestedPieceCode();
        bool requestingFirstSound = syllabicLevel.IsRequestingFirstSound();
        string requestedPieceType = syllabicLevel.GetRequestedPieceType();
        switch (dice)
        {
            case 0:
                string startOrEnd = requestingFirstSound ? RandomUtil.PickOne("scaf-tsound-sound2", ScaffoldUtils.START) : RandomUtil.PickOne("scaf-tsound-sound3", ScaffoldUtils.END);
                return SynQuery.Format($"{{0}} {startOrEnd}s with {{1}}.", requestedPieceCode, SynQuery.Seq(BREAK, targetSoundQuery));
            default:
                string firstOrLast = requestingFirstSound ? "first" : "last";
                return SynQuery.Format($"The {firstOrLast} sound in the {requestedPieceType} {{0}} is {{1}}.", requestedPieceCode, SynQuery.Seq(BREAK, targetSoundQuery));
        }
    }
}