using System.Collections.Generic;
using UnityEngine;

public class IdeaSlot
{
    private const int MODE_VERB = -1;

    private string targetRole;
    private string dependent = null;
    private int mode;

    private Environment environment;

    public IdeaSlot(string description)
    {
        if (description.Contains(":"))
        {
            int splitIndex = description.IndexOf(":");
            targetRole = description.Substring(0, splitIndex);
            dependent = description.Substring(splitIndex + 1);
            mode = MODE_VERB;
        }
        else if (description.EndsWith("'s"))
        {
            mode = NaturalLanguageUtil.POSSESIVE;
            targetRole = description.Substring(0, description.Length - 2);
        }
        else if (description.EndsWith("+s"))
        {
            mode = NaturalLanguageUtil.PLURAL;
            targetRole = description.Substring(0, description.Length - 2);
        }
        else
        {
            mode = NaturalLanguageUtil.NOMINATIVE;
            targetRole = description;
        }
        if (targetRole.StartsWith("the+"))
        {
            targetRole = targetRole.Substring(4);
            dependent = "the";
        }
        environment = GameObject.FindWithTag("StageObject").GetComponent<Environment>();
    }

    public string GetTargetRole()
    {
        return targetRole;
    }

    public string Apply(string wordSense)
    {
        if (mode == MODE_VERB)
        {
            return NaturalLanguageUtil.AlterVerb(dependent, wordSense, environment);
        }
        else
        {
            return NaturalLanguageUtil.AlterNominative(wordSense, mode, environment, dependent);
        }
    }
}
