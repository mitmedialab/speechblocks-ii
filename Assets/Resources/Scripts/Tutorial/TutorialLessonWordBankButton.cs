
using System.Collections;
using System.Linq;
using UnityEngine;

public class TutorialLessonWordBankButton : IStandaloneTutorialLesson
{
    public string Name { get; } = "word-bank-button";

    public string[] Prerequisites { get; } = { "word-bank-categories" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        GameObject drawerObject = GameObject.FindWithTag("WordDrawer");
        wordDrawer = drawerObject.GetComponent<WordDrawer>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        touchManager = stageObject.GetComponent<TouchManager>();
        environment = stageObject.GetComponent<Environment>();
        wordsArea = GameObject.FindWithTag("WordBankWordsArea").GetComponent<WordsArea>();
        categories = GameObject.FindGameObjectsWithTag("CategoryButton").Select(obj => obj.GetComponent<FixedCategoryButton>()).Where(category => null != category).ToArray();
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "word_bank" == stage && wordDrawer.IsDisplayingWordBank() && categories.Any(category => category.ButtonIsActive());
    }

    public IEnumerator GiveLesson()
    {
        FixedCategoryButton selectedCategory = categories.Where(category => category.ButtonIsActive()).First();
        touchManager.Constrain();
        environment.GetRoboPartner().LookAtTablet();
        while (!wordsArea.DeploymentComplete()) yield return null;
        string studentNameSense = environment.GetUser().GetNameSense();
        WordBankButton[] wordBankButtons = GameObject.FindGameObjectsWithTag("WordBankButton").Select(button => button.GetComponent<WordBankButton>()).ToArray();
        WordBankButton selectedButton = Enumerable.FirstOrDefault(wordBankButtons.Where(button => button.GetWordSense() == studentNameSense));
        string prompt = $"Aha! These are different {selectedCategory.GetName()} that we can spell!";
        string invitationToTap;
        if (null != selectedButton)
        {
            prompt = prompt + " I think I know which one you will like most.";
            invitationToTap = "Try tapping this one!";
        }
        else
        {
            invitationToTap = "Pick the one you like and tap it!";
        }
        yield return tutorial.InviteToTap(mainTappable: selectedButton,
                                            otherTappables: wordBankButtons,
                                            pointingAxis: Vector2.up + Vector2.left,
                                            prompt: prompt,
                                            tapInvitation: invitationToTap,
                                            mandatoryTap: true,
                                            cause: "tutorial:word-bank-button:tap-here");
        touchManager.ResetConstraints();
        yield return AwaitWriteButton();
        if (writeButton.GetWordSense() != studentNameSense)
        {
            prompt = null;
            invitationToTap = "Do you see a plus button that popped up? Tap this button to spell the word that you picked.";
        }
        else
        {
            prompt = "Wow, wasn't it your name?";
            invitationToTap = "Let's press the plus button to spell it!";
        };
        yield return tutorial.InviteToTap(  tappable: writeButton,
                                            pointingAxis: Vector2.zero,
                                            prompt: prompt,
                                            tapInvitation: invitationToTap,
                                            mandatoryTap: true,
                                            cause: "tutorial:word-bank-button:tap-here");
        touchManager.Unconstrain();
    }

    public bool CheckCompletion()
    {
        return wordDrawer.IsDisplayingKeyboard();
    }

    private IEnumerator AwaitWriteButton()
    {
        writeButton = null;
        while (true)
        {
            GameObject writeButtonObject = GameObject.FindWithTag("WriteButton");
            if (null != writeButtonObject)
            {
                writeButton = writeButtonObject.GetComponent<WriteButton>();
                while (synthesizerHelper.IsSpeaking()) yield return null;
                yield break;
            }
            else
            {
                yield return null;
            }
        }
    }

    private Tutorial tutorial;
    private TouchManager touchManager;
    private WordDrawer wordDrawer;
    private FixedCategoryButton[] categories;
    private SynthesizerController synthesizerHelper;
    private WriteButton writeButton;
    private Environment environment;
    private WordsArea wordsArea;
}