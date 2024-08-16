using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialLessonSpelling : IPlugInTutorialLesson
{
    public string Topic { get; } = "scaffolder-start";

    public string Name { get; } = "spelling";

    public string[] Prerequisites { get; } = { };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        touchManager = stageObject.GetComponent<TouchManager>();
        helpButton = GameObject.FindWithTag("HelpButton");
        scaffolder = stageObject.GetComponent<Scaffolder>();
    }

    public IEnumerator GiveLesson(List<SynQuery> synSequence, object extraArgoment)
    {
        Logging.LogLessonStart(Name);
        touchManager.Constrain();
        touchManager.AddAllowedToTap("KeyboardKey");
        environment.GetRoboPartner().LookAtChild();
        synSequence.Add("You will need to listen carefully to the sounds inside the word.");
        yield return synthesizerHelper.SpeechCoroutine(SynQuery.Seq(synSequence), cause: "tutorial:spelling:intro", canInterrupt: false, boundToStages: null);
        string prompt = "And then find which of these blocks make the same sounds.";
        GameObject[] keyboardKeys = GameObject.FindGameObjectsWithTag("KeyboardKey").Where(key => key.GetComponent<KeyboardKey>().IsActive()).ToArray();
        environment.GetRoboPartner().LookAtTablet();
        yield return tutorial.DemonstrateUIElement(uiElement: keyboardKeys, pointingAxis: new Vector2(-0.5f, 1), prompt: prompt, cause: "tutorial:spelling:keys");
        prompt = $"Should you need my help, tap the lightbulb!";
        yield return tutorial.DemonstrateUIElement(uiElement: helpButton, pointingAxis: Vector2.zero, prompt: prompt, cause: "tutorial:spelling:scaffolder-help");
        environment.GetRoboPartner().LookAtChild();
        touchManager.Unconstrain();
        Logging.LogLessonEnd(Name);
    }

    public bool CheckCompletion()
    {
        return scaffolder.IsComplete();
    }

    private Environment environment;
    private Tutorial tutorial;
    private SynthesizerController synthesizerHelper;
    private TouchManager touchManager;
    private GameObject helpButton;
    private Scaffolder scaffolder;
}