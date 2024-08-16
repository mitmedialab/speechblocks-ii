using System.Collections;
using UnityEngine;
using System.Linq;

public class TutorialLessonAssociationsSpell: IStandaloneTutorialLesson
{
    public string Name { get; } = "associations-spell";

    public string[] Prerequisites { get; } = { "associations" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        environment = stageObject.GetComponent<Environment>();
        tutorial = stageObject.GetComponent<Tutorial>();
        touchManager = stageObject.GetComponent<TouchManager>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        associationsPanel = GameObject.FindWithTag("AssociationsPanel").GetComponent<AssociationsPanel>();
        writeButton = associationsPanel.transform.Find("write_button_holder").Find("write_button").gameObject;
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "canvas" == stage && writeButton.activeInHierarchy;
    }

    public IEnumerator GiveLesson()
    {
        environment.GetRoboPartner().LookAtTablet();
        touchManager.Constrain();
        Composition composition = GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>();
        touchManager.AddAllowedToTouch(composition.GetAllPictureBlocks().Select(pblock => pblock.GetComponent<Draggable>()));
        touchManager.AddAllowedToTapDelayed(associationsPanel.GetAssociationButtons());
        GameObject closeButton = associationsPanel.transform.Find("close_button_holder").Find("close_button").gameObject;
        touchManager.AddAllowedToTapDelayed(closeButton.GetComponent<ITappable>());
        while (synthesizer.IsSpeaking()) yield return null;
        yield return tutorial.InviteToTap(writeButton.GetComponent<ITappable>(),
                                            pointingAxis: Vector2.up,
                                            prompt: null,
                                            tapInvitation: "Tap here to spell it.",
                                            mandatoryTap: false,
                                            cause: "tutorial:association-spell");
        completed = true;
        touchManager.Unconstrain();
    }

    public bool CheckCompletion()
    {
        return completed;
    }

    private Environment environment;
    private Tutorial tutorial;
    private TouchManager touchManager;
    private SynthesizerController synthesizer;
    private AssociationsPanel associationsPanel;
    private GameObject writeButton;
    private bool completed = false;
}