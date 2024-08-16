using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelpModuleGallery : IHelpModule
{
    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        gallery = GameObject.FindWithTag("Gallery").GetComponent<Gallery>();
        environment = stageObject.GetComponent<Environment>();
    }

    public bool HelpAppliesToCurrentContext(string currentStage)
    {
        return currentStage == "gallery";
    }

    public IEnumerator GiveHelp()
    {
        environment.GetRoboPartner().LookAtTablet();
        bool interrupted = false;
        yield return synthesizerHelper.SpeechCoroutine("On this page, we keep the pictures that you made.",
            cause: "help:gallery:intro",
            onInterrupt: () => interrupted = true);
        if (interrupted) yield break;
        CoroutineResult<bool> demoResult = new CoroutineResult<bool>();
        GameObject newSceneButton;
        List<GameObject> currentSceneButtons = gallery.GetButtonsForCurrentScenesAndNewScene(out newSceneButton);
        if (gallery.ButtonsOnCurrentPage() > 1)
        {
            yield return tutorial.DemonstrateUIElementInterruptible(currentSceneButtons,
                pointingAxis: Vector2.left,
                prompt: "Here they are. Tap on them if you want to change anything.",
                cause: "help:gallery:scene-buttons",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
        }
        if (gallery.CurrentPageID() == gallery.PageCount() - 1)
        {
            yield return tutorial.DemonstrateUIElementInterruptible(newSceneButton,
                prompt: "Tap here if you want to start a new one.",
                cause: "help:gallery:new-button",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
        }
        if (gallery.PageCount() > 1)
        {
            List<GameObject> arrows = gallery.GetArrows();
            yield return tutorial.DemonstrateUIElementInterruptible(arrows,
                pointingAxis: Vector2.left,
                prompt: "There are more pictures on other pages. Tap on these arrows to go between the pages.",
                cause: "help:gallery:handles",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
        }
        List<GameObject> ideaButtons = gallery.GetPageElements("idea_button");
        if (ideaButtons.Count > 0 && ideaButtons[0].activeSelf)
        {
            yield return tutorial.DemonstrateUIElementInterruptible(ideaButtons,
                prompt: $"Tap the lightbulb if you want to hear some ideas about what you can make.",
                cause: "help:gallery:idea-button",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
        }
        if (gallery.ButtonsOnCurrentPage() > 1)
        {
            List<GameObject> recyclingBins = gallery.GetPageElements("recycling-bin");
            yield return tutorial.DemonstrateUIElementInterruptible(recyclingBins,
                prompt: "If you want to get rid of some of the pictures, drag them here with your finger.",
                cause: "help:gallery:recycling-bin",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
        }
        List<GameObject> exitButtons = gallery.GetPageElements("cross_button");
        yield return tutorial.DemonstrateUIElementInterruptible(exitButtons,
            prompt: "And if you are done playing, you can press here to exit the game.",
            cause: "help:gallery:exit",
            result: demoResult);
        if (!demoResult.WasSuccessful()) yield break;
    }

    private Tutorial tutorial;
    private SynthesizerController synthesizerHelper = null;
    private Gallery gallery;
    private Environment environment;
}