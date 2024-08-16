using System.Collections;
using UnityEngine;
using System.Linq;

public class TutorialLessonAssociationsIdeaButton: IStandaloneTutorialLesson
{
    public string Name { get; } = "associations-idea-button";

    public string[] Prerequisites { get; } = { "associations-spell" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        environment = stageObject.GetComponent<Environment>();
        tutorial = stageObject.GetComponent<Tutorial>();
        touchManager = stageObject.GetComponent<TouchManager>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        associationsPanel = GameObject.FindWithTag("AssociationsPanel").GetComponent<AssociationsPanel>();
        ideaButton = associationsPanel.transform.Find("idea_button_holder").Find("idea_button").gameObject;
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "canvas" == stage && ideaButton.activeInHierarchy;
    }

    public IEnumerator GiveLesson()
    {
        environment.GetRoboPartner().LookAtTablet();
        touchManager.Constrain();
        Composition composition = GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>();
        touchManager.AddAllowedToTouch(composition.GetAllPictureBlocks().Select(pblock => pblock.GetComponent<Draggable>()));
        GameObject writeButton = associationsPanel.transform.Find("write_button_holder").Find("write_button").gameObject;
        touchManager.AddAllowedToTapDelayed(writeButton.GetComponent<ITappable>());
        touchManager.AddAllowedToTapDelayed(associationsPanel.GetAssociationButtons());
        GameObject closeButton = associationsPanel.transform.Find("close_button_holder").Find("close_button").gameObject;
        touchManager.AddAllowedToTapDelayed(closeButton.GetComponent<ITappable>());
        while (synthesizer.IsSpeaking()) yield return null;
        yield return tutorial.InviteToTap(ideaButton.GetComponent<ITappable>(),
            pointingAxis: Vector2.up,
            prompt: $"If you want to see things that might go well with {Vocab.GetWord(associationsPanel.GetSelectedButton().GetWordSense())}",
            tapInvitation: "tap here",
            mandatoryTap: false,
            cause: "tutorial:associations-idea-button");
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
    private GameObject ideaButton;
    private bool completed = false;
}