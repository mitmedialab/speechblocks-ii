using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HelpModuleWordBank : IHelpModule
{
    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        environment = stageObject.GetComponent<Environment>();
        wordDrawer = GameObject.FindWithTag("WordDrawer").GetComponent<WordDrawer>();
    }

    public bool HelpAppliesToCurrentContext(string currentStage)
    {
        return currentStage == "word_bank" || currentStage == "staging_area";
    }

    public IEnumerator GiveHelp()
    {
        environment.GetRoboPartner().LookAtTablet();
        CoroutineResult<bool> demoResult = new CoroutineResult<bool>();
        GameObject writeButton = GameObject.FindWithTag("WriteButton");
        bool childDriven = environment.GetUser().InChildDrivenCondition();
        if (null != writeButton)
        {
            yield return tutorial.DemonstrateUIElementInterruptible(writeButton,
                pointingAxis: Vector2.up,
                prompt: $"Tap this button to spell the word you picked.",
                cause: "help:word-bank:write-btn",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
        }
        else {
            if (childDriven) {
                GameObject[] wordBankButtons = GameObject.FindGameObjectsWithTag("WordBankButton");
                if (0 == wordBankButtons.Length)
                {
                    GameObject[] categoryButtons = GameObject.FindGameObjectsWithTag("CategoryButton");
                    yield return tutorial.DemonstrateUIElementInterruptible(categoryButtons,
                        pointingAxis: Vector2.up,
                        prompt: $"Tap these buttons to see different kinds of things that you can make.",
                        cause: "help:word-bank:categories",
                        result: demoResult);
                    if (!demoResult.WasSuccessful()) yield break;
                    GameObject speechRecoButton = GameObject.FindWithTag("SpeechRecoButton");
                    yield return tutorial.DemonstrateUIElementInterruptible(speechRecoButton,
                        prompt: $"Or you can tap the microphone and tell me the word that you want to make.",
                        cause: "help:word-bank:speech-btn",
                        result: demoResult);
                    if (!demoResult.WasSuccessful()) yield break;
                }
                else
                {
                    yield return tutorial.DemonstrateUIElementInterruptible(wordBankButtons,
                        pointingAxis: Vector2.up,
                        prompt: $"Tap on these buttons to pick a word to spell.",
                        cause: "help:word-bank:word-bank-btns",
                        result: demoResult);
                    if (!demoResult.WasSuccessful()) yield break;
                }
            }
            else
            {
                List <MachineDriverChoiceButton> machineDriverChoiceButtons = MachineDriver.GetChoiceButtons().Where(button => button.IsDeployed()).ToList();
                if (0 != machineDriverChoiceButtons.Count)
                {
                    yield return tutorial.DemonstrateUIElementInterruptible(machineDriverChoiceButtons,
                            pointingAxis: Vector2.up,
                            prompt: $"Tap on these buttons to pick a word to spell.",
                            cause: "help:word-bank:choice-buttons",
                            result: demoResult);
                    if (!demoResult.WasSuccessful()) yield break;
                }
            }
        }
        if (!synthesizerHelper.IsSpeaking())
        {
            string backButtonRoot = childDriven ? "word_bank_area" : "staging_area";
            GameObject backButton = wordDrawer.transform.Find("scroller")?.Find(backButtonRoot)?.Find("dn-handle")?.gameObject;
            if (null != backButton && backButton.activeInHierarchy)
            {
                yield return tutorial.DemonstrateUIElementInterruptible(backButton,
                    prompt: $"You can tap this button if you want to go back.",
                    cause: "help:word-bank:back",
                    result: demoResult);
            }
            if (!demoResult.WasSuccessful()) yield break;
        }
    }

    private Tutorial tutorial;
    private Environment environment;
    private SynthesizerController synthesizerHelper = null;
    private WordDrawer wordDrawer;
}