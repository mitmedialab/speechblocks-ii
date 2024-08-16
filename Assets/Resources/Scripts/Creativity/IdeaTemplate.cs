using System;
using System.Text;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;
using UnityEngine;

public class IdeaTemplate
{
    private string id;
    private string title = null;
    private string icon = null;
    private string promptPattern;
    private List<WordSuggestion> wordSuggestions = new List<WordSuggestion>();
    private List<IdeaSlot> ideaSlots = new List<IdeaSlot>();
    private Dictionary<string, string> roles = new Dictionary<string, string>();
    private string anchorType = null;
    private double logPriority = 0.0;

    public IdeaTemplate(string id, string title, string icon, string prompt, IEnumerable<WordSuggestion> suggestions)
    {
        this.id = id;
        this.title = title;
        this.icon = icon;
        promptPattern = prompt;
        wordSuggestions = new List<WordSuggestion>(suggestions);
    }

    public IdeaTemplate(Environment environment, string id, JSONNode description)
    {
        this.id = id;
        title = description["title"];
        icon = description["icon"];
        promptPattern = AdaptPromptToCondition(environment, description["prompt"]);
        LoadWordSuggestions(description);
    }

    public IdeaTemplate(Environment environment, string id, JSONNode description, Dictionary<string, int> typeCounts, int imageableCount)
    {
        this.id = id;
        LoadWordSuggestions(description);
        LoadRoles(description["roles"]);
        DetermineAnchorType(typeCounts);
        ConstructPattern(AdaptPromptToCondition(environment, description["prompt"]));
        DeterminePriority(typeCounts, imageableCount);
    }

    public string GetID()
    {
        return id;
    }

    public string GetTitle()
    {
        return title;
    }

    public string GetIcon()
    {
        return icon;
    }

    public double GetLogPriority()
    {
        return logPriority;
    }

    public string GetAnchorType()
    {
        return anchorType;
    }

    public bool IsApplicable(Dictionary<string, List<PictureBlock>> pictureBlocksByType)
    {
        return roles.Values.All(type => pictureBlocksByType.ContainsKey(type));
    }

    public string GetPrompt()
    {
        return promptPattern;
    }

    public string GetPrompt(Dictionary<string, List<PictureBlock>> pictureBlocksByType)
    {
        Dictionary<string, PictureBlock> roleAssignment = SampleRoleAssignment(pictureBlocksByType);
        List<string> ideaSlotApplications = new List<string>();
        foreach (IdeaSlot ideaSlot in ideaSlots)
        {
            ideaSlotApplications.Add(ideaSlot.Apply(roleAssignment[ideaSlot.GetTargetRole()].GetTermWordSense()));
        }
        return string.Format(promptPattern, ideaSlotApplications.ToArray());
    }

    public bool HasWordSuggestions()
    {
        return wordSuggestions.Count != 0;
    }

    public List<WordSuggestion> GetWordSuggestions()
    {
        return wordSuggestions;
    }

    private void LoadWordSuggestions(JSONNode description)
    {
        JSONNode wordSuggestionsJSON = description["suggested-words"];
        if (null == wordSuggestionsJSON) return;
        foreach (JSONNode suggestionJSON in wordSuggestionsJSON)
        {
            wordSuggestions.Add(new WordSuggestion(suggestionJSON));
        }
    }

    private void DetermineAnchorType(Dictionary<string, int> typeCounts)
    {
        anchorType = LinqUtil.MinBy(roles.Values, type => DictUtil.GetOrDefault(typeCounts, type, defaultValue: 1), valIfEmpty: null);
    }

    private void DeterminePriority(Dictionary<string, int> typeCounts, int imageableCount)
    {
        foreach (string type in roles.Values)
        {
            logPriority += Math.Log((double)imageableCount / (double)DictUtil.GetOrDefault(typeCounts, type, 1)) / 2;
        }
    }

    private void LoadRoles(JSONNode rolesDescription)
    {
        foreach (string key in rolesDescription.Keys) {
            roles[key] = (string)(rolesDescription[key]);
        }
    }

    private string AdaptPromptToCondition(Environment environment, string prompt)
    {
        StringBuilder promptBuilder = new StringBuilder();
        int pos = 0;
        while (true)
        {
            int openingBraceLoc = prompt.IndexOf('{', startIndex: pos);
            if (openingBraceLoc < 0)
            {
                promptBuilder.Append(prompt.Substring(pos, prompt.Length - pos));
                return promptBuilder.ToString();
            }
            promptBuilder.Append(prompt.Substring(pos, openingBraceLoc - pos));
            int closingBraceLoc = prompt.IndexOf('}', startIndex: openingBraceLoc);
            string variablePart = prompt.Substring(openingBraceLoc + 1, closingBraceLoc - openingBraceLoc - 1);
            promptBuilder.Append(ChooseOption(variablePart, environment));
            pos = closingBraceLoc + 1;
        }
    }

    private string ChooseOption(string variablePart, Environment environment)
    {
        string[] variants = variablePart.Split('|');
        foreach (string variant in variants)
        {
            int splitterLoc = variant.IndexOf(':');
            string variantKey = variant.Substring(0, splitterLoc);
            if (VariantMatches(variantKey, environment))
            {
                return variant.Substring(splitterLoc + 1);
            }
        }
        return "";
    }

    private bool VariantMatches(string variantKey, Environment environment)
    {
        switch (variantKey)
        {
            case "CHILD-DRIVEN":
                return environment.GetUser().InChildDrivenCondition();
            case "MACHINE-DRIVEN":
                return !environment.GetUser().InChildDrivenCondition();
            case "EXPRESSIVE":
                return environment.GetUser().InExpressiveCondition();
            case "REWARD":
                return !environment.GetUser().InExpressiveCondition();
            default:
                Debug.Log("UNEXPECTED VARIANT KEY");
                return false;
        }
    }

    private void ConstructPattern(string prompt)
    {
        StringBuilder patternBuilder = new StringBuilder();
        int pointer = 0;
        while (true)
        {
            int nextIdeaSlotStart = prompt.IndexOf('<', startIndex: pointer);
            if (nextIdeaSlotStart < 0)
            {
                patternBuilder.Append(prompt.Substring(pointer, prompt.Length - pointer));
                promptPattern = patternBuilder.ToString();
                return;
            }
            else
            {
                patternBuilder.Append(prompt.Substring(pointer, nextIdeaSlotStart - pointer));
                patternBuilder.Append($"{{{ideaSlots.Count}}}");
                int nextIdeaSlotEnd = prompt.IndexOf('>', startIndex: nextIdeaSlotStart + 1);
                string ideaSlotDescription = prompt.Substring(nextIdeaSlotStart + 1, nextIdeaSlotEnd - nextIdeaSlotStart - 1);
                IdeaSlot ideaSlot = new IdeaSlot(ideaSlotDescription);
                if (!roles.ContainsKey(ideaSlot.GetTargetRole())) { throw new System.Exception($"UNKNOWN ROLE: {ideaSlot.GetTargetRole()}"); }
                ideaSlots.Add(ideaSlot);
                pointer = nextIdeaSlotEnd + 1;
            }
        }
    }

    private Dictionary<string, PictureBlock> SampleRoleAssignment(Dictionary<string, List<PictureBlock>> pictureBlocksByType)
    {
        Dictionary<string, PictureBlock> roleAssignment = new Dictionary<string, PictureBlock>();
        List<string> roleOrder = roles.Keys.OrderBy(role => pictureBlocksByType[roles[role]].Count).ToList();
        List<PictureBlock> selected = new List<PictureBlock>();
        foreach (string role in roleOrder)
        {
            List<PictureBlock> candidates = pictureBlocksByType[roles[role]].Where(pBlock => !selected.Contains(pBlock)).ToList();
            if (0 == candidates.Count) return null;
            PictureBlock selection = RandomUtil.PickOne("idea-temp-role1", candidates);
            roleAssignment[role] = selection;
            selected.Add(selection);
        }
        return roleAssignment;
    }
}
