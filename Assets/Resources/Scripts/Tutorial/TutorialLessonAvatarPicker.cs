using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialLessonAvatarPicker : IStandaloneTutorialLesson, IHelpModule
{
    public string Name { get; } = "avatar-picker";

    public string[] Prerequisites { get; } = { "spelling-complete" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    // {0} = affix, {1} = property, {2} = nominative, {3} = possessive address
    private string[] PICKER_PROMPT_TEMPLATES = new string[]
    {
        "Which one of these people has {0} {1} that looks like {3}? Please tap that person!",
        "Pick the person who has the same {1} as {2}!",
        "Which person has {0} {1} similar to {3}? Press that person!",
        "Tap the person who has the same {1} as {2}!"
    };

    private List<string> UNUSUAL_PROPERTIES = new List<string>(){ "Hat", "Accessory", "FacialHair" };
    // {0} = affix, {1} = property, {2} = nominative, {3} = possessive address, {4} = "Do" or "Does"
    private string UNUSUAL_PICKER_PROMPT_TEMPLATE_PART_1 = "{4} {2} have {0} {1}? If yes, pick {0} {1} that looks like {3}!";

    private static Dictionary<string, string> PROPERTY_DESCRIPTORS = new Dictionary<string, string>() {     {"Accessory", "glasses" },
                                                                                                            {"AccessoryColor", "glasses color" },
                                                                                                            {"Eyebrows;Eyes;Mouth", "face"},
                                                                                                            {"Hair", "hair"},
                                                                                                            {"HairColor", "hair color"},
                                                                                                            {"FacialHair", "beard or moustache"},
                                                                                                            {"FacialHairColor", "beard or moustache color"},
                                                                                                            {"Hat", "hat"},
                                                                                                            {"HatColor", "hat color"},
                                                                                                            {"Clothes", "clothes"},
                                                                                                            {"ClothesColor", "clothes color"},
                                                                                                            {"SkinColor", "skin color"}};

    private string[] NON_COLOR_PROPERTIES = { "Accessory", "Eyebrows;Eyes;Mouth", "Hair", "FacialHair", "Hat", "Clothes" };
    private string[] COLOR_PROPERTIES = { "AccessoryColor", "HairColor", "FacialHairColor", "HatColor", "ClothesColor", "SkinColor" };

    public void Init(GameObject stageObject)
    {
        if (null != synthesizer) return;
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        touchManager = stageObject.GetComponent<TouchManager>();
        stageOrchestrator = stageObject.GetComponent<StageOrchestrator>();
        avatarPicker = GameObject.FindWithTag("AvatarPicker").GetComponent<AvatarPicker>();
        avatarSelectorPanel = avatarPicker.transform.Find("AvatarPanel").GetComponent<AvatarSelectorPanel>();
        resultBox = GameObject.FindWithTag("ResultBox").GetComponent<ResultBox>();
        editButton = resultBox.transform.Find("edit-button").gameObject;
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "keyboard" == stage && Vocab.IsInNameSense(resultBox.GetWordSense());
    }

    public IEnumerator GiveLesson()
    {
        touchManager.Constrain();
        string nameSense = resultBox.GetWordSense();
        string nominative = NaturalLanguageUtil.AlterNominative(nameSense, NaturalLanguageUtil.NOMINATIVE, environment);
        string possessive = NaturalLanguageUtil.AlterNominative(nameSense, NaturalLanguageUtil.POSSESIVE, environment);
        string looks = NaturalLanguageUtil.AlterVerb("look", nameSense, environment);
        string buttonPrompt = $"Let's show the game how {nominative} {looks} like!";
        yield return tutorial.InviteToTap(  tappable: editButton.GetComponent<ITappable>(),
                                            pointingAxis: Vector2.zero,
                                            prompt: buttonPrompt,
                                            tapInvitation: "Press the pencil button!",
                                            mandatoryTap: true,
                                            cause: "tutorial:avatar-picker:invitation");
        yield return DemonstratePropertySelection(property: "SkinColor",
            introPrompt: $"Let's pick the skin color on {possessive} portrait!",
            tapInvitation: "Do you see the button in the upper left corner that has the same color as the skin of the person on the picture? Let's tap it!",
            nameSense: nameSense);
        yield return DemonstratePropertySelection(property: "Hair",
            introPrompt: $"Excellent! Now, let's choose how the hair on {possessive} portrait will look like!",
            tapInvitation: "Press the button that looks like a haircut.",
            nameSense: nameSense);
        yield return DemonstratePropertySelection(property: "HairColor",
            introPrompt: $"Great! Now, let's pick the hair color!",
            tapInvitation: "Tap the colored button next to the hair button!",
            nameSense: nameSense);
        touchManager.Unconstrain();
        yield return synthesizer.SpeechCoroutine("Love it! You can try other buttons to change other things.",
            cause: "tutorial:avatar-picker",
            canInterrupt: false);
        GameObject commitButton = avatarPicker.transform.Find("close_button").gameObject;
        while (!avatarSelectorPanel.IsRetracted()) { yield return null; }
        if ("avatar_picker" == stageOrchestrator.GetStage())
        {
            touchManager.Constrain();
            touchManager.AddAllowedToTapDelayed(GetColorPropertyButtons().Concat(GetNonColorPropertyButtons()).Select(obj => obj.GetComponent<ITappable>()));
            yield return tutorial.InviteToTap(commitButton.GetComponent<ITappable>(),
                pointingAxis: POINTING_AXIS,
                prompt: $"When you are done you can save {possessive} portrait",
                tapInvitation: "by pressing the checkmark button!",
                mandatoryTap: false,
                cause: "tutorial:avatar-picker:save");
            touchManager.Unconstrain();
        }
        while (OnAvatarPanelOrInTransit()) yield return null;
        yield return synthesizer.SpeechCoroutine("Nice!", canInterrupt: false, keepPauses: true, cause: "tutorial:avatar-picker:end", boundToStages: null);
        if (!touchManager.IsUnconstrained()) { touchManager.Unconstrain(); }
        completed = true;
    }

    public bool CheckCompletion()
    {
        return completed;
    }

    public bool HelpAppliesToCurrentContext(string currentStage)
    {
        return currentStage == "avatar_picker" || currentStage == "avatar_selector_panel";
    }

    public IEnumerator GiveHelp()
    {
        environment.GetRoboPartner().LookAtTablet();
        bool interrupted = false;
        CoroutineResult<bool> demoResult = new CoroutineResult<bool>();
        if (!avatarSelectorPanel.IsDeployed())
        {
            yield return synthesizer.SpeechCoroutine($"This is where we make {NaturalLanguageUtil.AlterNominative(avatarPicker.GetNameSense(), NaturalLanguageUtil.POSSESIVE, environment)} portrait.",
                cause: "help:avatar-picker:intro",
                onInterrupt: () => interrupted = true);
            if (interrupted) yield break;
            List<GameObject> nonColorPropertyButtons = GetNonColorPropertyButtons();
            yield return tutorial.DemonstrateUIElementInterruptible(nonColorPropertyButtons,
                pointingAxis: new Vector2(-1, 1),
                prompt: "When you tap here, you can change different things on the portrait.",
                cause: "help:avatar-picker:property-btns",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
            List<GameObject> colorPropertyButtons = GetColorPropertyButtons();
            yield return tutorial.DemonstrateUIElementInterruptible(colorPropertyButtons,
                pointingAxis: new Vector2(-1, 1),
                prompt: "When you tap here, you can change different colors on the portrait.",
                cause: "help:avatar-picker:color-btns",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
            GameObject commitButton = avatarPicker.transform.Find("close_button").gameObject;
            yield return tutorial.DemonstrateUIElementInterruptible(commitButton,
                prompt: "When you are done, press this button to save the portrait!",
                cause: "help:avatar-picker:save",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
        }
        else
        {
            GameObject[] avatarButtons = GameObject.FindGameObjectsWithTag("AvatarSelectorButton");
            string prompt = GetPrompt(avatarSelectorPanel.InvokedWithProperty(), avatarPicker.GetNameSense());
            yield return tutorial.DemonstrateUIElementInterruptible(avatarButtons,
                pointingAxis: Vector2.up,
                prompt: prompt,
                cause: "help:avatar-picker:select-avatar",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
            GameObject closeButton = avatarSelectorPanel.transform.Find("cross_button").gameObject;
            yield return tutorial.DemonstrateUIElementInterruptible(closeButton,
                prompt: "Or you can press here to go back",
                cause: "help:avatar-picker:back",
                result: demoResult);
            if (!demoResult.WasSuccessful()) yield break;
        }
    }

    private bool OnAvatarPanelOrInTransit()
    {
        string activePanel = stageOrchestrator.GetStage();
        return "avatar_picker" == activePanel || "avatar_selector_panel" == activePanel || null == activePanel;
    }

    private IEnumerator DemonstratePropertySelection(string property, string introPrompt, string tapInvitation, string nameSense)
    {
        ITappable propertyButton = avatarPicker.transform.Find(property).GetComponent<ITappable>();
        touchManager.ResetConstraints();
        yield return tutorial.InviteToTap(tappable: propertyButton,
                        pointingAxis: POINTING_AXIS,
                        prompt: introPrompt,
                        tapInvitation: tapInvitation,
                        mandatoryTap: true,
                        cause: $"tutorial:avatar-picker:demo:{property}");
        while (!avatarSelectorPanel.IsDeployed()) yield return null;
        while (synthesizer.IsSpeaking()) yield return null;
        touchManager.ResetConstraints();
        List<ITappable> selectorButtons = GameObject.FindGameObjectsWithTag("AvatarSelectorButton").Select(obj => obj.GetComponent<ITappable>()).ToList();
        touchManager.AddAllowedToTap(selectorButtons);
        yield return tutorial.InviteToTap(mainTappable: null,
                                            otherTappables: selectorButtons,
                                            pointingAxis: Vector2.zero,
                                            prompt: null,
                                            tapInvitation: GetPrompt(property, nameSense),
                                            mandatoryTap: true,
                                            cause: $"tutorial:avatar-picker:selection:{property}",
                                            doPoint: false);
        touchManager.ResetConstraints();
        while (!avatarSelectorPanel.IsRetracted()) { yield return null; }
    }

    private string GetPrompt(string property, string nameSense)
    {
        string nominative = NaturalLanguageUtil.AlterNominative(nameSense, NaturalLanguageUtil.NOMINATIVE, environment);
        string possessiveAddr = NaturalLanguageUtil.AlterNominative(nameSense, NaturalLanguageUtil.POSSESIVE_ADDR, environment);
        string affixA = ("Clothes" == property || "Accessory" == property) ? "" : "a";
        if (!UNUSUAL_PROPERTIES.Contains(property))
        {
            return string.Format(RandomUtil.PickOne("lesn-ava-pick1", PICKER_PROMPT_TEMPLATES), affixA, PROPERTY_DESCRIPTORS[property], nominative, possessiveAddr);
        }
        else
        {
            string doOrDoes = NaturalLanguageUtil.AlterVerb("Do", nameSense, environment);
            return string.Format(UNUSUAL_PICKER_PROMPT_TEMPLATE_PART_1, affixA, PROPERTY_DESCRIPTORS[property], nominative, possessiveAddr, doOrDoes);
        }
    }

    private List<GameObject> GetNonColorPropertyButtons()
    {
        return NON_COLOR_PROPERTIES.Select(property => avatarPicker.transform.Find(property).gameObject).ToList();
    }

    private List<GameObject> GetColorPropertyButtons()
    {
        return COLOR_PROPERTIES.Select(property => avatarPicker.transform.Find(property).gameObject).ToList();
    }

    private Tutorial tutorial;
    private Environment environment;
    private SynthesizerController synthesizer;
    private TouchManager touchManager;
    private StageOrchestrator stageOrchestrator;
    private AvatarPicker avatarPicker;
    private AvatarSelectorPanel avatarSelectorPanel;
    private ResultBox resultBox;
    private GameObject editButton;
    private bool completed = false;

    private Vector2 POINTING_AXIS = new Vector2(-0.5f, 1);
}