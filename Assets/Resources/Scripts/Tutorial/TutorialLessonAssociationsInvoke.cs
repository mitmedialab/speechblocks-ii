using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialLessonAssociationsInvoke : IStandaloneTutorialLesson
{
    public string Name { get; } = "associations-invoke";

    public string[] Prerequisites { get; } = { "gallery" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        touchManager = stageObject.GetComponent<TouchManager>();
        environment = stageObject.GetComponent<Environment>();
        tutorial = stageObject.GetComponent<Tutorial>();
        composition = GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>();
        assocPanel = GameObject.FindWithTag("AssociationsPanel").GetComponent<AssociationsPanel>();
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "canvas" == stage
                && 0 == touchManager.GetTouchCount()
                && composition.GetTopPictureBlocks().Any(pblock => !Vocab.IsInNameSense(pblock.GetImageWordSense()));
    }

    public IEnumerator GiveLesson()
    {
        if (assocPanel.AssociationEverInvoked()) { yield break; }
        List<PictureBlock> targetPBlocks = composition.GetTopPictureBlocks().Where(pblock => !Vocab.IsInNameSense(pblock.GetImageWordSense())).ToList();
        environment.GetRoboPartner().LookAtChild();
        int speechID = -1;
        yield return synthesizer.SpeechCoroutine("Did you know that if you tap on something on your picture, you can get ideas on what goes well with it?", cause: "tutorial:assoc-tap:intro", out speechID);
        if (!synthesizer.WasInterrupted(speechID))
        {
            tutorial.PointAt(targetPBlocks, axis: Vector2.zero, cause: "tutorial:assoc-tap:intro");
        }
    }

    public bool CheckCompletion()
    {
        return assocPanel.AssociationEverInvoked();
    }

    private Composition composition;
    private TouchManager touchManager;
    private Tutorial tutorial;
    private Environment environment;
    private SynthesizerController synthesizer;
    private AssociationsPanel assocPanel;
}
