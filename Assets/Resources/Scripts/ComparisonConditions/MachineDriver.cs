using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;

public class MachineDriver : MonoBehaviour
{
    private GameObject handleObject = null;
    private Environment environment;
    private WordDrawer wordDrawer;
    private IdeaMaster ideaMaster;
    private SynthesizerController synthesizerHelper;
    private StageOrchestrator stageOrchestrator;
    private Scaffolder scaffolder;
    private Tutorial tutorial;
    private Vocab vocab;
    private IdeaTemplate selectedIdea = null;
    private ResultBox resultBox = null;
    private List<MachineDriverThemeChoiceButton> themeChoiceButtons = new List<MachineDriverThemeChoiceButton>();
    private List<MachineDriverChoiceButton> choiceButtons = new List<MachineDriverChoiceButton>();

    private CoroutineRunner machineDriverRunner = new CoroutineRunner();

    // Start is called before the first frame update
    void Start()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        environment = stageObject.GetComponent<Environment>();
        ideaMaster = stageObject.GetComponent<IdeaMaster>();
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        scaffolder = stageObject.GetComponent<Scaffolder>();
        stageOrchestrator = stageObject.GetComponent<StageOrchestrator>();
        tutorial = stageObject.GetComponent<Tutorial>();
        vocab = stageObject.GetComponent<Vocab>();
        wordDrawer = GameObject.FindWithTag("WordDrawer").GetComponent<WordDrawer>();
        choiceButtons = GetChoiceButtons().OrderBy(button => button.transform.localPosition.x).ToList();
        themeChoiceButtons = GetThemeChoiceButtons()
                                        .OrderBy(button => button.transform.localPosition.x)
                                        .Select(button => button.GetComponent<MachineDriverThemeChoiceButton>())
                                        .ToList();
        handleObject = GameObject.FindWithTag("WordDrawer").transform.Find("up-handle").gameObject;
        resultBox = GameObject.FindWithTag("ResultBox").GetComponent<ResultBox>();
    }

    public void ConfigureUI()
    {
        enabled = !environment.GetUser().InChildDrivenCondition();
    }

    public void SelectIdea(IdeaTemplate idea)
    {
        this.selectedIdea = idea;
    }

    public static List<MachineDriverThemeChoiceButton> GetThemeChoiceButtons()
    {
        GameObject topicChoiceAssembly = GameObject.FindWithTag("TopicChoiceAssembly");
        List<MachineDriverThemeChoiceButton> themeChoiceButtons = new List<MachineDriverThemeChoiceButton>();
        foreach (Transform child in topicChoiceAssembly.transform)
        {
            MachineDriverThemeChoiceButton themeChoiceButton = child.GetComponent<MachineDriverThemeChoiceButton>();
            if (null != themeChoiceButton) { themeChoiceButtons.Add(themeChoiceButton); }
        }
        return themeChoiceButtons;
    }

    public static List<MachineDriverChoiceButton> GetChoiceButtons()
    {
        List<MachineDriverChoiceButton> choiceButtons = new List<MachineDriverChoiceButton>();
        WordDrawer wordDrawer = GameObject.FindWithTag("WordDrawer").GetComponent<WordDrawer>();
        foreach (Transform stagingAreaChild in wordDrawer.GetStagingArea().transform)
        {
            MachineDriverChoiceButton choiceButton = stagingAreaChild.GetComponent<MachineDriverChoiceButton>();
            if (null != choiceButton) { choiceButtons.Add(choiceButton); }
        }
        return choiceButtons;
    }

    public void GetObjectsOfAttention(List<GameObject> objectsOfAttention, string stage)
    {
        if (null == environment.GetUser() || environment.GetUser().InChildDrivenCondition()) return;
        if ("canvas" == stage)
        {
            objectsOfAttention.AddRange(themeChoiceButtons.Where(button => button.IsDeployed()).Select(button => button.gameObject));
        }
        else if ("staging_area" == stage)
        {
            objectsOfAttention.AddRange(choiceButtons.Where(button => button.IsDeployed()).Select(button => button.gameObject));
        }
    }

    // Update is called once per frame
    void Update()
    {
        CheckRestartConditions();
        machineDriverRunner.Update();
    }

    private void CheckRestartConditions()
    {
        if (machineDriverRunner.IsRunning())
        {
            string stage = stageOrchestrator.GetStage();
            if (stage == "gallery" || stage == "title_page" || stage == "login")
            {
                machineDriverRunner.SetCoroutine(null);
            }
        }
        else
        {
            if (null == environment.GetUser() || environment.GetUser().InChildDrivenCondition()) return;
            string stage = stageOrchestrator.GetStage();
            if (stage == "canvas")
            {
                machineDriverRunner.SetCoroutine(MachineDriverSequence());
            }
        }
    }

    private IEnumerator MachineDriverSequence()
    {
        HideChoiceButtons();
        Composition composition = GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>();
        List<WordSuggestion> allSuggestions = new List<WordSuggestion>();
        List<WordSuggestion> unusedSuggestions = new List<WordSuggestion>();
        handleObject.SetActive(false);
        yield return AwaitConditionsForDeliveringIdeas();
        yield return DeliverIdeas(composition);
        handleObject.SetActive(true);
        InitiateWordSuggestions(composition, selectedIdea, allSuggestions, unusedSuggestions);
        while (true)
        {
            yield return AwaitConditionsForDeliveringWord();
            yield return DeliverChoices(selectedIdea, allSuggestions, unusedSuggestions, composition);
            while (null == scaffolder.GetTarget()) yield return null;
            while (!scaffolder.IsComplete()) yield return null;
            while (null == resultBox.GetSpawnedPictureBlock()) yield return null;
            resultBox.GetSpawnedPictureBlock().GetComponent<PictureBlock>().SetTheme(selectedIdea.GetID());
            MarkCompletedWordSense(scaffolder.GetTargetWordSense(), unusedSuggestions);
            MachineDriverChoiceButton.DestroyWriteButtons();
            HideChoiceButtons();
        }
    }

    // returns the number of existing ideas
    private int GetIdeas(Composition composition, List<IdeaTemplate> ideas)
    {
        List<PictureBlock> pictureBlocks = composition.GetAllPictureBlocks();
        List<string> existingThemeIDs = pictureBlocks.OrderBy(pblock => pblock.GetTimestamp()).Select(pblock => pblock.GetTheme()).Where(theme => null != theme).ToList();
        HashSet<string> themeIDSet = new HashSet<string>();
        for (int i = existingThemeIDs.Count - 1; i >= 0; --i)
        {
            if (themeIDSet.Contains(existingThemeIDs[i])) { existingThemeIDs.RemoveAt(i); }
            else { themeIDSet.Add(existingThemeIDs[i]); }
        }
        for (int i = Math.Max(existingThemeIDs.Count - 2, 0); i < existingThemeIDs.Count; ++i)
        {
            ideas.Add(ideaMaster.FetchOpeningIdeaByID(existingThemeIDs[i]));
        }
        int existingIdeasNum = ideas.Count;
        for (int i = ideas.Count; i < themeChoiceButtons.Count; ++i)
        {
            if (!tutorial.IsLessonCompleted("spelling-complete") && !ideas.Any(idea => idea.GetID() == IdeaMaster.KNOWN_PEOPLE_IDEA))
            {
                ideas.Add(ideaMaster.FetchOpeningIdeaByID(IdeaMaster.KNOWN_PEOPLE_IDEA));
            }
            else
            {
                while (true)
                {
                    IdeaTemplate idea = ideaMaster.FetchOpeningIdea();
                    if (!ideas.Any(otherIdea => otherIdea.GetID() == idea.GetID()))
                    {
                        ideas.Add(idea);
                        break;
                    }
                }
            }
        }
        return existingIdeasNum;
    }

    private IEnumerator AwaitConditionsForDeliveringIdeas()
    {
        while (true)
        {
            string stage = stageOrchestrator.GetStage();
            if ("canvas" == stage) { yield break; }
            yield return null;
        }
    }

    private IEnumerator DeliverIdeas(Composition composition)
    {
        if (null != scaffolder.GetTarget())
        {
            scaffolder.UnsetTarget();
            wordDrawer.InvokeWordBank(instant: true);
        }
        environment.GetRoboPartner().LookAtChild();
        List<IdeaTemplate> ideas = new List<IdeaTemplate>();
        int existingIdeaCount = GetIdeas(composition, ideas);
        for (int i = 0; i < themeChoiceButtons.Count; ++i) { themeChoiceButtons[i].Setup(ideas[i]); }
        selectedIdea = null;
        synthesizerHelper.Speak(MakeTopicChoicePrompt(ideas, existingIdeaCount), cause: $"machine-driver:topic-choice", boundToStages: "canvas");
        while (null == selectedIdea) yield return null;
        foreach (MachineDriverThemeChoiceButton button in themeChoiceButtons) { button.Hide(); }
        wordDrawer.Deploy(deployInstantly: false);
    }

    private SynQuery MakeTopicChoicePrompt(List<IdeaTemplate> ideas, int existingIdeaCount)
    {
        StringBuilder promptBuilder = new StringBuilder();
        string[] WOULD_YOU_LIKE_TO = { "Would you like to", "Do you want to" };
        promptBuilder.Append(RandomUtil.PickOne("mdriver-topic1", WOULD_YOU_LIKE_TO));
        if (0 == existingIdeaCount)
        {
            string[] MAKE = { "make", "build" };
            promptBuilder.Append($" {RandomUtil.PickOne("mdriver-topic2", MAKE)} a picture about ");
            for (int i = 0; i < ideas.Count; ++i)
            {
                if (0 != i) { promptBuilder.Append(", or "); }
                promptBuilder.Append(ideas[i].GetTitle());
            }
        }
        else
        {
            string[] MAKING = { "making", "building" };
            promptBuilder.Append($" keep {RandomUtil.PickOne("mdriver-topic2", MAKING)} a picture about ");
            for (int i = 0; i < existingIdeaCount; ++i)
            {
                if (0 != i) { promptBuilder.Append(", or "); }
                promptBuilder.Append(ideas[i].GetTitle());
            }
            promptBuilder.Append($", or add something about ");
            for (int i = existingIdeaCount; i < ideas.Count; ++i)
            {
                if (existingIdeaCount != i) { promptBuilder.Append(", or "); }
                promptBuilder.Append(ideas[i].GetTitle());
            }
        }
        promptBuilder.Append("?");
        return promptBuilder.ToString();
    }

    private IEnumerator AwaitConditionsForDeliveringWord()
    {
        while (true)
        {
            string stage = stageOrchestrator.GetStage();
            if ("staging_area" == stage) yield break;
            yield return null;
        }
    }

    private IEnumerator DeliverChoices(IdeaTemplate idea, List<WordSuggestion> allSuggestions, List<WordSuggestion> unusedSuggestions, Composition composition)
    {
        bool allChoicesUsed;
        List<WordSuggestion> choices = GenerateChoices(allSuggestions, unusedSuggestions, composition, out allChoicesUsed);
        SynQuery prompt = GetChoicesPrompt(allChoicesUsed, choices);
        while (synthesizerHelper.IsSpeaking()) yield return null;
        int speechID = synthesizerHelper.Speak(prompt, cause: $"machine-driver:word:{idea.GetID()}", boundToStages: "canvas+staging_area");
        yield return ActivateChoiceButtons(choices);
        while (synthesizerHelper.IsSpeaking(speechID)) yield return null;
    }

    private SynQuery GetChoicesPrompt(bool noNewSuggestions, List<WordSuggestion> choices)
    {
        List<SynQuery> sequence = new List<SynQuery>();
        string[] WOULD_YOU_LIKE_TO = { "Would you like to", "Do you want to" };
        string[] MAKE = { "make", "build", "spell" };
        sequence.Add($"{RandomUtil.PickOne("mdriver-prompt2", WOULD_YOU_LIKE_TO)} {RandomUtil.PickOne("mdriver-prompt3", MAKE)}. ");
        for (int i = 0; i < choices.Count; ++i)
        {
            if (0 != i) { sequence.Add(", or "); }
            sequence.Add(vocab.GetPronunciation(Vocab.GetWord(choices[i].GetWordSense())));
        }
        sequence.Add("? ");
        if (noNewSuggestions)
        {
            string[] dont_have_ideas = { "I don't have any more ", "I have no more " };
            sequence.Add(RandomUtil.PickOne("mdriver-prompt1", dont_have_ideas));
            sequence.Add("new ideas. ");
            string[] if_you_want_to = { "If you want to", "If you'd like," };
            string[] you_can_use_cross_button = { "you can use the cross buttons", "you can tap the crosses" };
            string[] to_go_back = { "to go back", "to head all the way back", "to return" };
            string[] to_picturebook = { "to your picturebook", "to the page where we keep all your pictures" };
            string[] and_start_new_pic = { "and start a new picture", "and begin a new picture" };
            sequence.Add($"{RandomUtil.PickOne("mdriver-noideas-c0", if_you_want_to)} {RandomUtil.PickOne("mdriver-noideas-c1", you_can_use_cross_button)} {RandomUtil.PickOne("mdriver-noideas-c2", to_go_back)} {RandomUtil.PickOne("mdriver-noideas-c3", to_picturebook)} {RandomUtil.PickOne("mdriver-noideas-c4", and_start_new_pic)}!");
        }
        return SynQuery.Seq(sequence);
    }

    private void InitiateWordSuggestions(Composition composition, IdeaTemplate ideaTemplate, List<WordSuggestion> allSuggestions, List<WordSuggestion> unusedSuggestions)
    {
        allSuggestions.Clear();
        unusedSuggestions.Clear();
        List<string> usedWordSenses = GetWordSensesOnTheScene(composition);
        List<WordSuggestion> wordSuggestions = ideaTemplate.GetWordSuggestions();
        foreach (WordSuggestion wordSuggestion in wordSuggestions)
        {
            string wordSense = wordSuggestion.GetWordSense();
            if (!IsLegit(wordSense)) continue;
            allSuggestions.Add(wordSuggestion);
            if (!SenseWasUsed(wordSense, usedWordSenses)) {
                unusedSuggestions.Add(wordSuggestion);
            }
        }
    }

    private bool SenseWasUsed(string wordSense, List<string> usedSenses)
    {
        string collapsedWordSense = Vocab.CollapseCompositeWord(wordSense);
        return usedSenses.Any(sense => Vocab.SenseMatchesQuery(collapsedWordSense, sense));
    }

    private List<string> GetWordSensesOnTheScene(Composition composition)
    {
        List<string> allWordSenses = new List<string>();
        foreach (PictureBlock pictureBlock in composition.GetAllPictureBlocks())
        {
            allWordSenses.Add(pictureBlock.GetTermWordSense());
        }
        return allWordSenses;
    }

    private bool IsLegit(string wordSense)
    {
        if (!vocab.IsInVocab(wordSense)) return false;
        string imageable = vocab.GetIconicImageable(wordSense);
        if (imageable.EndsWith(".noimg")) return false;
        //Sprite sprite = Resources.Load<Sprite>("Images/" + imageable);
        //if (null == sprite) return false;
        return true;
    }

    private List<WordSuggestion> GenerateChoices(List<WordSuggestion> allSuggestions, List<WordSuggestion> unusedSuggestions, Composition composition, out bool allChoicesUsed)
    {
        List<WordSuggestion> choices = new List<WordSuggestion>();
        if (unusedSuggestions.Count <= 3)
        {
            choices.AddRange(unusedSuggestions);
        }
        else
        {
            choices.AddRange(unusedSuggestions.Take(2));
            choices.Add(RandomUtil.PickOne("mdriver-pickchoices-1", unusedSuggestions.Skip(2).ToArray()));
        }
        if (choices.Count < 3)
        {
            choices.AddRange(CreateAssociationChoices(composition, allSuggestions, 3 - choices.Count));
        }
        allChoicesUsed = 0 == choices.Count;
        if (choices.Count < 3)
        {
            choices.AddRange(RandomUtil.Shuffle("mdriver-pickchoices-2", allSuggestions.Where(suggestion => !choices.Contains(suggestion))).Take(3 - choices.Count));
        }
        return choices;
    }

    private List<WordSuggestion> CreateAssociationChoices(Composition composition, List<WordSuggestion> allSuggestions, int maxChoices)
    {
        List<string> allSceneWordSenses = GetWordSensesOnTheScene(composition);
        List<string> allCoveredWordSenses = new List<string>(allSceneWordSenses);
        allCoveredWordSenses.AddRange(allSuggestions.Select(suggestion => suggestion.GetWordSense()));
        Dictionary<string, List<string>> voteDictionary = new Dictionary<string, List<string>>();
        AssociationLibrary assocLib = GameObject.FindWithTag("StageObject").GetComponent<AssociationLibrary>();
        foreach (string sceneWordSense in allSceneWordSenses)
        {
            if (Vocab.IsInNameSense(sceneWordSense)) continue;
            foreach (string associatedWordSense in assocLib.GetAssociations(sceneWordSense))
            {
                if (!IsLegit(associatedWordSense)) continue;
                if (SenseWasUsed(associatedWordSense, allCoveredWordSenses)) continue;
                DictUtil.GetOrSpawn(voteDictionary, associatedWordSense).Add(sceneWordSense);
            }
        }
        IOrderedEnumerable<string> candidates = RandomUtil.Shuffle("mdriver-assocs-1", voteDictionary.Keys).OrderByDescending(word => voteDictionary[word].Count);
        return candidates.Take(maxChoices).Select(sense => CreateAssocSuggestion(sense, voteDictionary)).ToList();
    }

    private WordSuggestion CreateAssocSuggestion(string wordSense, Dictionary<string, List<string>> voteDictionary)
    {
        string associatedWordSense = RandomUtil.PickOne("mdriver-assocs-2", voteDictionary[wordSense]);
        return new WordSuggestion(word_sense: wordSense,
                                    reason: $"I think {Vocab.GetWord(wordSense)} can go well with {Vocab.GetWord(associatedWordSense)}.");
    }

    private IEnumerator ActivateChoiceButtons(List<WordSuggestion> choices)
    {
        List<MachineDriverChoiceButton> activeChoiceButtons = new List<MachineDriverChoiceButton>();
        if (choices.Count > 1) { activeChoiceButtons.Add(choiceButtons[0]); }
        if (choices.Count != 2) { activeChoiceButtons.Add(choiceButtons[1]); }
        if (choices.Count > 1) { activeChoiceButtons.Add(choiceButtons[2]); }
        for (int i = 0; i < choices.Count; ++i)
        {
            activeChoiceButtons[i].Setup(choices[i]);
            yield return null;
        }
    }

    private void MarkCompletedWordSense(string wordSense, List<WordSuggestion> unusedSuggestions)
    {
        for (int i = 0; i < unusedSuggestions.Count; ++i)
        {
            if (unusedSuggestions[i].GetWordSense() == wordSense)
            {
                unusedSuggestions.RemoveAt(i);
                break;
            }
        }
    }

    private void HideChoiceButtons()
    {
        foreach (MachineDriverThemeChoiceButton button in themeChoiceButtons) { button.gameObject.SetActive(false); }
        foreach (MachineDriverChoiceButton choiceButton in choiceButtons) { choiceButton.gameObject.SetActive(false); }
    }
}
