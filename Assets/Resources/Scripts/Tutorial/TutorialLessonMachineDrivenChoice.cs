using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialLessonMachineDrivenChoice: IStandaloneTutorialLesson
{
    public string Name { get; } = "machine-driven-choice";

    public string[] Prerequisites { get; } = { };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        touchManager = stageObject.GetComponent<TouchManager>();
        GameObject drawerObject = GameObject.FindWithTag("WordDrawer");
        wordDrawer = drawerObject.GetComponent<WordDrawer>();
        scaffolder = stageObject.GetComponent<Scaffolder>();
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "canvas" == stage;
    }

    public IEnumerator GiveLesson()
    {
        Environment environment = GameObject.FindWithTag("StageObject").GetComponent<Environment>();
        touchManager.Constrain();
        List<MachineDriverThemeChoiceButton> themeChoiceButtons = MachineDriver.GetThemeChoiceButtons();
        while (!themeChoiceButtons.All(button => button.IsDeployed())) yield return null;
        while (synthesizer.IsSpeaking()) yield return null;
        MachineDriverThemeChoiceButton peopleTheme = themeChoiceButtons.FirstOrDefault(button => button.GetIdea()?.GetID() == IdeaMaster.KNOWN_PEOPLE_IDEA);
        yield return tutorial.InviteToTap(mainTappable: peopleTheme,
            otherTappables: themeChoiceButtons,
            pointingAxis: Vector2.up,
            prompt: null,
            tapInvitation: null != peopleTheme ? "I think you might like this one. Tap one of the buttons to pick what our picture will be about!" : "Tap one of these buttons to pick!",
            mandatoryTap: true,
            cause: "tutorial:mdriven-choice:pick-theme");
        int tappedIndex = themeChoiceButtons.FindIndex(button => button.GetConfirmButton().activeSelf);
        ITappable confirmButton = themeChoiceButtons[tappedIndex].GetConfirmButton().GetComponent<ITappable>();
        touchManager.AddAllowedToTapDelayed(confirmButton);
        while (synthesizer.IsSpeaking()) yield return null;
        yield return tutorial.InviteToTap(confirmButton,
            pointingAxis: Vector2.zero,
            prompt: null,
            tapInvitation: "Tap this checkmark to start making words for this idea!",
            mandatoryTap: true,
            cause: "tutorial:mdriven-choice:confirm-theme");
        List<MachineDriverChoiceButton> choiceButtons = MachineDriver.GetChoiceButtons();
        while (choiceButtons.All(button => !button.gameObject.activeSelf)) yield return null;
        yield return CoroutineUtils.WaitCoroutine(0.5f);
        while (synthesizer.IsSpeaking()) yield return null;
        touchManager.AddAllowedToTapDelayed(choiceButtons);
        MachineDriverChoiceButton userButton = choiceButtons.FirstOrDefault(button => button.GetWordSuggestion().GetWordSense() == environment.GetUser()?.GetNameSense());
        yield return tutorial.InviteToTap(mainTappable: userButton,
            otherTappables: choiceButtons,
            pointingAxis: Vector2.up,
            prompt: null,
            tapInvitation: null != userButton ? "I think you might like this one. Tap one of the buttons to pick the word!" : "Tap one of these buttons to pick the word!",
            mandatoryTap: true,
            cause: "tutorial:mdriven-choice:pick-word");
        while (null == GameObject.FindWithTag("WriteButton")) yield return null;
        while (synthesizer.IsSpeaking()) yield return null;
        GameObject writeButton = GameObject.FindWithTag("WriteButton");
        yield return tutorial.InviteToTap(writeButton.GetComponent<ITappable>(),
            pointingAxis: Vector2.up,
            prompt: null,
            tapInvitation: "Tap the plus to make the word!",
            mandatoryTap: true,
            cause: "tutorial:mdriven-choice:pick-word");
        while (!wordDrawer.IsDisplayingKeyboard()) yield return null;
        touchManager.Unconstrain();
    }

    public bool CheckCompletion()
    {
        return null != scaffolder.GetTarget();
    }

    private Tutorial tutorial;
    private SynthesizerController synthesizer;
    private TouchManager touchManager;
    private WordDrawer wordDrawer;
    private Scaffolder scaffolder;
}
