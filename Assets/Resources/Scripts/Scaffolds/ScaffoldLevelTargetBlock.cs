using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SimpleJSON;

public class ScaffoldLevelTargetBlock : IScaffoldLevel
{
    private string[] ANNOUNCERS_OF_ALTERNATIVE = { "or", "and", "and also", "and sometimes" };

    private SynQuery BREAK = SynQuery.Break(0.25f);

    public SynQuery Prompt(PGMapping target, int[] syllableBreakdown, int targetPGSlot, bool giveQuestion, int scaffolderTargetID, int scaffoldingInteractionID)
    {
        Logging.LogScaffoldingPromptLevel("target-block", scaffolderTargetID, scaffoldingInteractionID, targetPGSlot, giveQuestion);
        PGPair targetPG = target.pgs[targetPGSlot];
        string pcombo = targetPG.GetUnaccentuatedPhonemeCode();
        string grapheme = targetPG.GetGrapheme();
        string[] graphemeBreakdown = PhonemeUtil.GetGraphemeBreakdown(pcombo, grapheme);
        string coreGrapheme = graphemeBreakdown[1];
        string defaultGrapheme = PhonemeUtil.GetDefaultGrapheme(pcombo);
        List<SynQuery> mainSequence = new List<SynQuery>();
        SynQuery targetSoundQuery = SynQuery.SayAs(coreGrapheme, pcombo);
        mainSequence.Add(MakeDefaultGraphemePrompt(targetSoundQuery, defaultGrapheme, 1 < defaultGrapheme.Length || 1 < coreGrapheme.Length));
        if (coreGrapheme != defaultGrapheme)
        {
            mainSequence.Add(", " + RandomUtil.PickOne("scaf-tblock-prompt1", ANNOUNCERS_OF_ALTERNATIVE) + " ");
            mainSequence.Add(SynQuery.Spell(coreGrapheme));
        }
        mainSequence.Add(".");
        return SynQuery.Seq(mainSequence);
    }

    private SynQuery MakeDefaultGraphemePrompt(SynQuery targetSoundQuery, string defaultGrapheme, bool multiletter)
    {
        int dice = RandomUtil.Range("scaf-tblock-gr1", 0, 2);
        switch (dice)
        {
            case 0:
                return SynQuery.Format("The sound {0} is made by {1}.", SynQuery.Seq(BREAK, targetSoundQuery, BREAK), SynQuery.Spell(defaultGrapheme));
            default:
                string sOrNone = multiletter ? "s" : "";
                string noneOrS = multiletter ? "" : "s";
                string areOrIs = multiletter ? " are " : " is ";
                return SynQuery.Format($"The letter{sOrNone} that make{noneOrS} {{0}} {areOrIs} {{1}}", SynQuery.Seq(BREAK, targetSoundQuery, BREAK), SynQuery.Spell(defaultGrapheme));
        }
    }
}