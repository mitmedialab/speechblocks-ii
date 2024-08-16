using System.Collections;
using System.Linq;
using UnityEngine;

public class HelpModuleKeyboard : IHelpModule
{
    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        environment = stageObject.GetComponent<Environment>();
        resultBox = GameObject.FindWithTag("ResultBox").GetComponent<ResultBox>();
        wordBox = GameObject.FindWithTag("WordBox").GetComponent<WordBox>();
        wordBoxBackground = wordBox.transform.Find("background").gameObject;
        editButton = resultBox.transform.Find("edit-button").gameObject;
    }

    public bool HelpAppliesToCurrentContext(string currentStage)
    {
        return currentStage == "keyboard";
    }

    public IEnumerator GiveHelp()
    {
        environment.GetRoboPartner().LookAtTablet();
        bool interrupted = false;
        CoroutineResult<bool> demoResult = new CoroutineResult<bool>();
        GameObject spawnedPictureBlock = resultBox.GetSpawnedPictureBlock();
        if (null != spawnedPictureBlock)
        {
            bool spelledName = Vocab.IsInNameSense(resultBox.GetWordSense());
            bool newPortrait = spelledName && null == environment.GetAvatar(resultBox.GetWordSense());
            if (spelledName)
            {
                yield return tutorial.DemonstrateUIElementInterruptible(editButton,
                    prompt: $"Press this button {(newPortrait ? "to" : "if you want to")} change {NaturalLanguageUtil.AlterNominative(resultBox.GetWordSense(), NaturalLanguageUtil.POSSESIVE, environment)} portrait.",
                    cause: "help:keyboard:edit-button",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
            }
            if (!newPortrait)
            {
                yield return tutorial.DemonstrateUIElementInterruptible(spawnedPictureBlock,
                    prompt: "This is the image for the word that you made. Tap on it to put it on your picture.",
                    cause: "help:keyboard:pictureblock",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
            }
        }
        else
        {
            yield return synthesizerHelper.SpeechCoroutine("This is the page where we make words.",
                cause: "help:keyboard:intro",
                onInterrupt: () => interrupted = true);
            if (interrupted) yield break;
            GameObject[] keyboardKeys = GameObject.FindGameObjectsWithTag("KeyboardKey").Where(key => key.GetComponent<KeyboardKey>().IsActive()).ToArray();
            yield return tutorial.DemonstrateUIElementInterruptible(keyboardKeys,
                pointingAxis: new Vector2(-1, 1),
                prompt: "We make them out of these blocks.",
                cause: "help:keyboard:keys",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
            yield return tutorial.DemonstrateUIElementInterruptible(wordBoxBackground,
                prompt: "We drag blocks into this box.",
                cause: "help:keyboard:word-box",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
            yield return synthesizerHelper.SpeechCoroutine($"But not every block will work.",
                cause: "help:keyboard:scaffolder-help-not-every-block",
                onInterrupt: () => interrupted = true);
            if (interrupted) yield break;
            GameObject helpButton = GameObject.FindWithTag("HelpButton");
            yield return tutorial.DemonstrateUIElementInterruptible(helpButton,
                prompt: $"If you don't know which block to pick, you can tap the lightbulb, and I will help you.",
                cause: "help:keyboard:scaffolder-help",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
            GameObject backButton = GameObject.FindWithTag("WordDrawer").GetComponent<WordDrawer>().GetKeyboard().transform.Find("dn-handle")?.gameObject;
            if (null != backButton && backButton.activeSelf)
            {
                yield return tutorial.DemonstrateUIElementInterruptible(backButton,
                    prompt: "You can tap this button if you want to go back to your picture.",
                    cause: "help:keyboard:back",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
            }
        }
    }

    private Tutorial tutorial;
    private Environment environment;
    private SynthesizerController synthesizerHelper = null;
    private ResultBox resultBox;
    private WordBox wordBox;
    private GameObject wordBoxBackground;
    private GameObject editButton;
}