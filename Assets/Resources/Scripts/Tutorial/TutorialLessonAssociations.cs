using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialLessonAssociations: IStandaloneTutorialLesson
{
    public string Name { get; } = "associations";

    public string[] Prerequisites { get; } = { "pictureblock-drag" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        touchManager = stageObject.GetComponent<TouchManager>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        associationsPanel = GameObject.FindWithTag("AssociationsPanel").GetComponent<AssociationsPanel>();
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "canvas" == stage && 0 != associationsPanel.GetAssociationButtons().Count;
    }

    public IEnumerator GiveLesson()
    {
        touchManager.Constrain();
        Composition composition = GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>();
        touchManager.AddAllowedToTouch(composition.GetAllPictureBlocks().Select(pblock => pblock.GetComponent<Draggable>()));
        while (associationsPanel.IsBeingDeployed()) yield return null;
        GameObject closeButton = associationsPanel.transform.Find("close_button_holder").Find("close_button").gameObject;
        touchManager.AddAllowedToTapDelayed(associationsPanel.GetAssociationButtons());
        touchManager.AddAllowedToTapDelayed(closeButton.GetComponent<ITappable>());
        while (synthesizer.IsSpeaking()) yield return null;
        List<GameObject> associationButtons = associationsPanel.GetAssociationButtons().Select(button => button.gameObject).ToList();
        yield return tutorial.DemonstrateUIElement(closeButton,
            pointingAxis: Vector2.up,
            prompt: "You can tap here to hide them.",
            cause: "tutorial:associations:hide");
        yield return tutorial.DemonstrateUIElement(associationButtons,
            pointingAxis: Vector2.up,
            prompt: "Or tap one of them to make it.",
            cause: "tutorial:associations:spell");
        tutorial.CeasePointing();
        completed = true;
        touchManager.Unconstrain();
    }

    public bool CheckCompletion()
    {
        return completed;
    }

    private Tutorial tutorial;
    private TouchManager touchManager;
    private SynthesizerController synthesizer;
    private AssociationsPanel associationsPanel;
    private bool completed = false;
}