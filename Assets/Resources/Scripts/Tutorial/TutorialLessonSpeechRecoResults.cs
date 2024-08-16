using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialLessonSpeechRecoResults : IPlugInTutorialLesson, IHelpModule
{
    public string Topic { get; } = "speech-reco-results";

    public string Name { get; } = "speech-reco-results";

    public string[] Prerequisites { get; } = { };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        if (null != synthesizerHelper) return;
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        wordsArea = GameObject.FindWithTag("WordBankWordsArea").GetComponent<WordsArea>();
        speechRecoButton = GameObject.FindWithTag("SpeechRecoButton").GetComponent<SpeechRecoButton>();
        environment = stageObject.GetComponent<Environment>();
    }

    public IEnumerator GiveLesson(List<SynQuery> synSequence, object extraArgument)
    {
        Logging.LogLessonStart(Name);
        synSequence.Add(GetPrompt());
        yield return synthesizerHelper.SpeechCoroutine(SynQuery.Seq(synSequence), cause: "tutorial:speech-reco-results", keepPauses: false);
        completed = true;
        Logging.LogLessonEnd(Name);
    }

    public bool CheckCompletion()
    {
        return completed;
    }

    public bool HelpAppliesToCurrentContext(string currentStage)
    {
        if (currentStage != "word_bank") return false;
        if (!speechRecoButton.ButtonIsActive()) return false;
        return true;
    }

    public IEnumerator GiveHelp()
    {
        environment.GetRoboPartner().LookAtTablet();
        return synthesizerHelper.SpeechCoroutine(GetPrompt(), cause: "help:speech-reco-results", keepPauses: false);
    }

    private string GetPrompt()
    {
        string[] IF_YOU_SEE = { "If you see", "If I've got" };
        string[] THE_WORD_YOU_WANTED = { "the word you wanted", "what you wanted to spell" };
        string[] TAP = { "tap", "press" };
        string[] TO_SPELL_IT = { "to spell it", "to make it", "to build it" };
        string[] IF_NOT = { "If not", "If you don't see it", "If I got it wrong" };
        string TAP_IF_NEEDED = (wordsArea.TargetWordCount() == wordsArea.Capacity()) ? "tap the Jibo button and " : "";
        string[] LOUDLY_AND_CLEARLY = { "loudly and clearly", "with loud and clear voice" };
        return  $"{RandomUtil.PickOne("lesn-sp-reco1", IF_YOU_SEE)} {RandomUtil.PickOne("lesn-sp-reco2", THE_WORD_YOU_WANTED)}, " +
                $"{RandomUtil.PickOne("lesn-sp-reco3", TAP)} {RandomUtil.PickOne("lesn-sp-reco4", TO_SPELL_IT)}. " +
                $"{RandomUtil.PickOne("lesn-sp-reco5", IF_NOT)}, {TAP_IF_NEEDED}say it again {RandomUtil.PickOne("lesn-sp-reco6", LOUDLY_AND_CLEARLY)}.";
    }

    private SynthesizerController synthesizerHelper = null;
    private WordsArea wordsArea;
    private SpeechRecoButton speechRecoButton;
    private Environment environment;
    private bool completed = false;
}