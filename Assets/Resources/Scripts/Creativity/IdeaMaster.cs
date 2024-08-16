using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SimpleJSON;

public class IdeaMaster : MonoBehaviour
{
    private Environment environment;
    private SynthesizerController synthesizer;
    private List<IdeaTemplate> openingIdeas = null;
    private List<IdeaTemplate> untriedOpeningIdeas = null;

    private Dictionary<string, List<string>> supertypes = new Dictionary<string, List<string>>();
    private Dictionary<string, int> typeCounts = new Dictionary<string, int>();
    private Dictionary<string, List<IdeaTemplate>> contextualIdeas = null;
    private HashSet<string> triedContextualIdeas = null;
    private AssociationsPanel associationsPanel = null;
    private GameObject compositionRoot;
    private Tutorial tutorial;

    private List<string> EMPTY_STR_LIST = new List<string>();
    private List<IdeaTemplate> EMPTY_IDEA_LIST = new List<IdeaTemplate>();
    private StageOrchestrator stageOrchestrator = null;

    private bool gaveAnyIdeas = false;

    public const string KNOWN_PEOPLE_IDEA = "known-people";

    private CoroutineRunner currentIdeaRunner = new CoroutineRunner();

    void Start()
    {
        stageOrchestrator = GetComponent<StageOrchestrator>();
        environment = GetComponent<Environment>();
        synthesizer = GetComponent<SynthesizerController>();
        tutorial = GetComponent<Tutorial>();
        associationsPanel = GameObject.FindWithTag("AssociationsPanel").GetComponent<AssociationsPanel>();
        compositionRoot = GameObject.FindWithTag("CompositionRoot");
        JSONNode ideasConfig = Config.GetConfig("Ideas");
        LoadSupertypes(ideasConfig);
    }

    // Start is called before the first frame update
    public void Reload(List<string> usedStarterIdeas)
    {
        Vocab vocab = GetComponent<Vocab>();
        JSONNode ideasConfig = Config.GetConfig("Ideas");
        LoadStarterIdeas(ideasConfig, usedStarterIdeas);
        LoadContextualIdeas(ideasConfig, vocab);
    }

    void Update()
    {
        currentIdeaRunner.Update();
    }

    public void Ideate(GameObject caller)
    {
        associationsPanel.Retract();
        if (currentIdeaRunner.IsRunning()) return;
        gaveAnyIdeas = true;
        List<PictureBlock> currentPictureBlocks = FindPictureBlocks();
        if (0 == currentPictureBlocks.Count)
        {
            currentIdeaRunner.SetCoroutine(IdeateStoryStarter(caller));
        }
        else
        {
            currentIdeaRunner.SetCoroutine(IdeateAroundContent(currentPictureBlocks, caller));
        }
    }

    public bool GaveAnyIdeas()
    {
        return gaveAnyIdeas;
    }

    public void IdeateAround(PictureBlock pictureBlock)
    {
        associationsPanel.Retract();
        if (currentIdeaRunner.IsRunning()) return;
        currentIdeaRunner.SetCoroutine(IdeateAroundContent(new List<PictureBlock>() { pictureBlock }, caller: pictureBlock.gameObject));
    }

    public IdeaTemplate FetchOpeningIdeaByID(string id)
    {
        foreach (IdeaTemplate ideaTemplate in openingIdeas)
        {
            if (ideaTemplate.GetID() == id) return ideaTemplate;
        }
        return null;
    }

    public IdeaTemplate FetchOpeningIdea()
    {
        if (0 == untriedOpeningIdeas.Count) { ResetUntriedOpeningIdeas(); }
        int ideaNum = RandomUtil.Range("idea-op1", 0, untriedOpeningIdeas.Count);
        return untriedOpeningIdeas[ideaNum];
    }

    private IEnumerator IdeateStoryStarter(GameObject caller)
    {
        environment.GetRoboPartner().LookAtChild();
        if (0 == untriedOpeningIdeas.Count) { ResetUntriedOpeningIdeas(); }
        int ideaNum = RandomUtil.Range("idea-start1", 0, untriedOpeningIdeas.Count);
        IdeaTemplate idea = untriedOpeningIdeas[ideaNum];
        untriedOpeningIdeas.RemoveAt(ideaNum);
        yield return synthesizer.SpeechCoroutine(idea.GetPrompt(), cause: $"ideamaster:starter-{idea.GetID()}:{Logging.GetObjectLogID(caller)}", boundToStages: null);
        if (!tutorial.IsLessonCompleted("gallery")) yield break;
        List<WordSuggestion> wordSuggestions = idea.GetWordSuggestions();
        if (0 != wordSuggestions.Count)
        {
            associationsPanel.Invoke(wordSuggestions, "idea-master");
        }
    }

    private IEnumerator IdeateAroundContent(List<PictureBlock> sceneContent, GameObject caller)
    {
        Dictionary<string, List<PictureBlock>> pBlocksByType = ArrangePicBlocksByTypes(sceneContent);
        IdeaTemplate contextualIdea = FetchContextualIdea(pBlocksByType);
        string prompt = null;
        if (null != contextualIdea) { prompt = contextualIdea.GetPrompt(pBlocksByType); }
        if (null != prompt)
        {
            yield return synthesizer.SpeechCoroutine(prompt, cause: $"ideamaster:contextual-{contextualIdea.GetID()}:{Logging.GetObjectLogID(caller)}", boundToStages: null);
        }
        else
        {
            List<string> terms = sceneContent.Select(pBlock => pBlock.GetTermWordSense()).Where(term => associationsPanel.HasAssociations(term)).ToList();
            if (0 != terms.Count)
            {
                string term = RandomUtil.PickOne("idea-content1", terms);
                associationsPanel.Invoke(term, cause: $"ideamaster:assocs-{term}:{Logging.GetObjectLogID(caller)}");
                while (associationsPanel.IsBeingDeployed()) yield return null;
            }
            else
            {
                yield return IdeateStoryStarter(caller);
            }
        }
    }

    private void LoadStarterIdeas(JSONNode ideasConfig, List<string> usedStarterIdeas)
    {
        openingIdeas = new List<IdeaTemplate>();
        if (!environment.GetUser().InChildDrivenCondition())
        {
            string userNameSense = environment.GetUser().GetNameSense();
            List<WordSuggestion> peopleSuggestions = new List<WordSuggestion>();
            peopleSuggestions.Add(new WordSuggestion(word_sense: userNameSense, reason: "Let's spell your name!"));
            Vocab vocab = GameObject.FindWithTag("StageObject").GetComponent<Vocab>();
            foreach (string nameSense in vocab.GetCustomNameSenses())
            {
                if (nameSense == userNameSense) continue;
                peopleSuggestions.Add(new WordSuggestion(word_sense: nameSense, reason: null));
            }
            IdeaTemplate peopleIdea = new IdeaTemplate(id: KNOWN_PEOPLE_IDEA, title: "people that you know", icon: "friend", prompt: "Let's spell some people that you know!", peopleSuggestions);
            openingIdeas.Add(peopleIdea);
        }
        untriedOpeningIdeas = new List<IdeaTemplate>();
        JSONNode starterIdeasConfig = ideasConfig["starter-ideas"];
        foreach (string starterIdeaID in starterIdeasConfig.Keys)
        {
            IdeaTemplate ideaTemplate = new IdeaTemplate(environment, starterIdeaID, description: starterIdeasConfig[starterIdeaID]);
            if (environment.GetUser().InChildDrivenCondition() || ideaTemplate.HasWordSuggestions())
            {
                openingIdeas.Add(ideaTemplate);
            }
        }
        if (null == usedStarterIdeas)
        {
            untriedOpeningIdeas.AddRange(openingIdeas);
        }
        else
        {
            untriedOpeningIdeas.AddRange(openingIdeas.Where(idea => !usedStarterIdeas.Contains(idea.GetID())));
        }
    }

    private void ResetUntriedOpeningIdeas()
    {
        untriedOpeningIdeas.Clear();
        untriedOpeningIdeas.AddRange(openingIdeas);
    }

    private List<PictureBlock> FindPictureBlocks()
    {
        List<PictureBlock> currentPictureBlocks = new List<PictureBlock>();
        if ("canvas" == stageOrchestrator.GetStage())
        {
            _FindPictureBlocks(compositionRoot.transform, currentPictureBlocks);
        }
        return currentPictureBlocks;
    }

    private void _FindPictureBlocks(Transform root, List<PictureBlock> pictureBlocks)
    {
        foreach (Transform child in root)
        {
            PictureBlock pictureBlock = child.GetComponent<PictureBlock>();
            if (null != pictureBlock)
            {
                pictureBlocks.Add(pictureBlock);
                _FindPictureBlocks(child, pictureBlocks);
            }
        }
    }

    private void LoadSupertypes(JSONNode ideasConfig)
    {
        JSONNode hypernymsConfig = ideasConfig["ideation-hypernyms"];
        Dictionary<string, List<string>> directSupertypes = new Dictionary<string, List<string>>();

        foreach (string hypernym in hypernymsConfig.Keys)
        {
            string[] hyponyms = ((string)hypernymsConfig[hypernym]).Split(',');
            foreach (string hyponym in hyponyms)
            {
                DictUtil.GetOrSpawn(directSupertypes, hyponym).Add(hypernym);
            }
        }
        while (supertypes.Count != directSupertypes.Count)
        {
            foreach (string type in directSupertypes.Keys)
            {
                List<string> directSupertypesOfType = directSupertypes[type];
                if (directSupertypesOfType.Any(supertype => directSupertypes.ContainsKey(supertype) && !supertypes.ContainsKey(supertype))) continue;
                List<string> allSupertypes = new List<string>();
                foreach (string supertype in directSupertypesOfType)
                {
                    allSupertypes.Add(supertype);
                    allSupertypes.AddRange(DictUtil.GetOrDefault(supertypes, supertype, defaultValue: EMPTY_STR_LIST));
                }
                allSupertypes = allSupertypes.Distinct().ToList();
                supertypes[type] = allSupertypes;
                foreach (string supertype in allSupertypes)
                {
                    typeCounts[supertype] = DictUtil.GetOrDefault(typeCounts, supertype) + 1;
                }
            }
        }
    }

    private void LoadContextualIdeas(JSONNode ideasConfig, Vocab vocab)
    {
        contextualIdeas = new Dictionary<string, List<IdeaTemplate>>();
        triedContextualIdeas = new HashSet<string>();
        JSONNode contextualIdeaConfig = ideasConfig["contextual-ideas"];
        foreach (string starterIdeaID in contextualIdeaConfig.Keys)
        {
            IdeaTemplate contextualIdea = new IdeaTemplate(environment, starterIdeaID, description: contextualIdeaConfig[starterIdeaID], typeCounts, vocab.GetNumberOfImageables());
            string anchorType = contextualIdea.GetAnchorType();
            DictUtil.GetOrSpawn(contextualIdeas, anchorType).Add(contextualIdea);
        }
    }

    private IdeaTemplate FetchContextualIdea(Dictionary<string, List<PictureBlock>> pBlocksByType)
    {
        List<IdeaTemplate> ideas = new List<IdeaTemplate>();
        foreach (string anchorType in pBlocksByType.Keys)
        {
            List<IdeaTemplate> recalledIdeas = DictUtil.GetOrDefault(contextualIdeas, anchorType, defaultValue: EMPTY_IDEA_LIST);
            ideas.AddRange(recalledIdeas.Where(idea => !triedContextualIdeas.Contains(idea.GetID()) && idea.IsApplicable(pBlocksByType)));
        }
        ideas = ideas.Distinct().ToList();
        if (0 == ideas.Count) return null;
        IdeaTemplate selectedIdea = RandomUtil.PickOneWeighted("idea-context1", ideas, ideas.Select(an_idea => an_idea.GetLogPriority()).ToList());
        triedContextualIdeas.Add(selectedIdea.GetID());
        return selectedIdea;
    }

    private Dictionary<string, List<PictureBlock>> ArrangePicBlocksByTypes(List<PictureBlock> pictureBlocks)
    {
        Dictionary<string, List<PictureBlock>> pictureBlocksByTypes = new Dictionary<string, List<PictureBlock>>();
        foreach (PictureBlock pictureBlock in pictureBlocks)
        {
            foreach (string type in GetPBlockTypes(pictureBlock))
            {
                DictUtil.GetOrSpawn(pictureBlocksByTypes, type).Add(pictureBlock);
            }
        }
        return pictureBlocksByTypes;
    }

    private List<string> GetPBlockTypes(PictureBlock pictureBlock)
    {
        List<string> pBlockTypes = new List<string>();
        string term = Vocab.GetCoreSense(pictureBlock.GetTermWordSense());
        if (Vocab.IsInNameSense(term))
        {
            RecordTypeAndSupertypes("NAME", pBlockTypes);
        }
        else
        {
            string picSense = Vocab.GetCoreSense(pictureBlock.GetImageWordSense());
            RecordTypeAndSupertypes(term, pBlockTypes);
            if (term != picSense) { RecordTypeAndSupertypes(picSense, pBlockTypes); }
        }
        return pBlockTypes.Distinct().ToList();
    }

    private void RecordTypeAndSupertypes(string type, List<string> allTypes)
    {
        allTypes.Add(type);
        allTypes.AddRange(DictUtil.GetOrDefault(supertypes, type, defaultValue: EMPTY_STR_LIST));
    }
}
