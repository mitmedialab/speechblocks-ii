using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HelpModuleCanvas : IHelpModule
{
    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        drawerHandle = GameObject.FindWithTag("WordDrawer").transform.Find("up-handle").gameObject;
        ideaButton = Array.Find(GameObject.FindGameObjectsWithTag("IdeaButton"), button => button.transform.parent == null);
        galleryButton = GameObject.FindWithTag("Gallery").transform.Find("GalleryHandle").gameObject;
        compositionRoot = GameObject.FindWithTag("CompositionRoot");
        recycleBin = GameObject.FindWithTag("TrashButton");
        environment = stageObject.GetComponent<Environment>();
        associationsPanel = GameObject.FindWithTag("AssociationsPanel").GetComponent<AssociationsPanel>();
        associationsSpellButton = associationsPanel.transform.Find("write_button_holder").gameObject;
        associationsIdeaButton = associationsPanel.transform.Find("idea_button_holder").gameObject;
        associationsCloseButton = associationsPanel.transform.Find("close_button_holder").gameObject;
        flipper = GameObject.FindWithTag("Flipper");
    }

    public bool HelpAppliesToCurrentContext(string currentStage)
    {
        return currentStage == "canvas";
    }

    public IEnumerator GiveHelp()
    {
        environment.GetRoboPartner().LookAtTablet();
        CoroutineResult<bool> demoResult = new CoroutineResult<bool>();
        if (!environment.GetUser().InChildDrivenCondition())
        {
            if (MachineDriver.GetThemeChoiceButtons().Any(button => button.gameObject.activeSelf)) {
                List<MachineDriverThemeChoiceButton> themeChoiceButtons = MachineDriver.GetThemeChoiceButtons();
                int confirmButtonIndex = themeChoiceButtons.FindIndex(button => button.GetConfirmButton().activeSelf);
                if (confirmButtonIndex < 0)
                {
                    yield return tutorial.DemonstrateUIElementInterruptible(themeChoiceButtons,
                        pointingAxis: Vector2.up,
                        prompt: $"Tap one of these buttons to pick what your picture will be about.",
                        cause: "help:canvas:theme-buttons",
                        result: demoResult);
                    if (!demoResult.WasSuccessful()) yield break;
                }
                else
                {
                    yield return tutorial.DemonstrateUIElementInterruptible(themeChoiceButtons[confirmButtonIndex].GetConfirmButton(),
                        prompt: $"Tap this button to pick this idea for your picture.",
                        cause: "help:canvas:theme-confirm-button",
                        result: demoResult);
                    if (!demoResult.WasSuccessful()) yield break;
                }
            }
        }
        else if (associationsPanel.GetAssociationButtons().Count > 0)
        {
            if (associationsSpellButton.activeSelf)
            {
                yield return tutorial.DemonstrateUIElementInterruptible(associationsSpellButton,
                    prompt: $"Tap this button to spell {Vocab.GetWord(associationsPanel.GetSelectedButton().GetWordSense())}.",
                    cause: "help:canvas:assoc-spell",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
                if (associationsIdeaButton.activeSelf)
                {
                    yield return tutorial.DemonstrateUIElementInterruptible(associationsIdeaButton,
                        prompt: $"Or tap this button to get words that might go well with {Vocab.GetWord(associationsPanel.GetSelectedButton().GetWordSense())}.",
                        cause: "help:canvas:assoc-deepen",
                        result: demoResult);
                    if (!demoResult.WasSuccessful()) yield break;
                }
            }
            else
            {
                string associationSeed = associationsPanel.GetCurrentWordSense();
                string prompt;
                if (null != associationSeed)
                {
                    prompt = $"Tap one of these buttons to pick a word that might go well with {Vocab.GetWord(associationsPanel.GetCurrentWordSense())}.";
                }
                else
                {
                    prompt = "These are ideas on what you can spell. Tap one of them to pick that idea.";
                }
                yield return tutorial.DemonstrateUIElementInterruptible(associationsPanel.GetAssociationButtons().Select(button => button.gameObject),
                    pointingAxis: Vector2.up,
                    prompt: prompt,
                    cause: "help:canvas:assoc-pick",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
                yield return tutorial.DemonstrateUIElementInterruptible(associationsCloseButton,
                    prompt: $"Or tap this button to hide them.",
                    cause: "help:canvas:assoc-hide",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
            }
        }
        else
        {
            bool interrupted = false;
            yield return synthesizerHelper.SpeechCoroutine("On this page, we make pictures.",
                cause: "help:canvas:intro",
                onInterrupt: () => interrupted = true);
            if (interrupted) yield break;
            if (0 != compositionRoot.transform.childCount)
            {
                List<GameObject> topPictureblocks = GetTopPictureBlocks();
                yield return tutorial.DemonstrateUIElementInterruptible(topPictureblocks,
                    pointingAxis: new Vector2(-1, 1),
                    prompt: "You can drag these images with your finger, or stretch and squish them with two fingers.",
                    cause: "help:canvas:pictureblocks",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
                yield return tutorial.DemonstrateUIElementInterruptible(flipper,
                    prompt: "If you want an image to face another way, drag it onto this arrow to flip it.",
                    cause: "help:canvas:flipper",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
                yield return tutorial.DemonstrateUIElementInterruptible(recycleBin,
                    prompt: "If you want to get rid of some image, drag it into this bin.",
                    cause: "help:canvas:recycling-bin",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
            }
            if (ideaButton.activeSelf)
            {
                yield return tutorial.DemonstrateUIElementInterruptible(ideaButton,
                    prompt: $"You can tap the lightbulb to hear some ideas about what we can make.",
                    cause: "help:canvas:idea-button",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
            }
            if (drawerHandle.activeSelf)
            {
                yield return tutorial.DemonstrateUIElementInterruptible(drawerHandle,
                    "You can tap here to add new things to the picture.",
                    cause: "help:canvas:keyboard-handle",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
            }
            if (galleryButton.activeSelf)
            {
                yield return tutorial.DemonstrateUIElementInterruptible(galleryButton,
                    "And you can tap here to go back to your picturebook.",
                    cause: "help:canvas:gallery-handle",
                    result: demoResult);
                if (!demoResult.WasSuccessful()) yield break;
            }
        }
    }

    private List<GameObject> GetTopPictureBlocks()
    {
        List<GameObject> topPictureBlocks = new List<GameObject>();
        foreach (Transform tform in compositionRoot.transform)
        {
            topPictureBlocks.Add(tform.gameObject);
        }
        return topPictureBlocks;
    }

    private Tutorial tutorial;
    private Environment environment;
    private SynthesizerController synthesizerHelper = null;
    private GameObject drawerHandle;
    private GameObject ideaButton;
    private GameObject galleryButton;
    private GameObject compositionRoot;
    private GameObject recycleBin;
    private AssociationsPanel associationsPanel;
    private GameObject associationsCloseButton;
    private GameObject associationsSpellButton;
    private GameObject associationsIdeaButton;
    private GameObject flipper;
}