public class NaturalLanguageUtil
{
    public const int NOMINATIVE = 0;
    public const int POSSESIVE = 1;
    public const int POSSESIVE_ADDR = 2;
    public const int PLURAL = 3;

    private static string[] ES_ENDINGS = { "s", "sh", "ch", "x", "z" };

    public static string Pluralize(string word)
    {
        foreach (string esEnding in ES_ENDINGS)
        {
            if (word.EndsWith(esEnding)) { return word + "es"; }
        }
        return word + "s";
    }

    public static string AddSToVerb(string verb)
    {
        foreach (string esEnding in ES_ENDINGS)
        {
            if (verb.EndsWith(esEnding)) { return verb + "es"; }
        }
        if (verb.EndsWith("y"))
        {
            return verb.Substring(0, verb.Length - 1) + "ies";
        }
        else
        {
            return verb + "s";
        }
    }

    public static string AlterVerb(string verb, string governor, Environment environment)
    {
        if (IsCurrentPlayer(governor, environment))
        {
            return verb;
        }
        else
        {
            return AddSToVerb(verb);
        }
    }


    public static string AlterNominative(string wordSense, int mode, Environment environment)
    {
        return AlterNominative(wordSense, mode, environment, dependent: null);
    }
    public static string AlterNominative(string wordSense, int mode, Environment environment, string dependent)
    {
        string word = Vocab.GetWord(wordSense);
        if (mode == PLURAL)
        {
            word = Pluralize(word);
        }
        else if (mode == POSSESIVE || mode == POSSESIVE_ADDR)
        {
            word = word + "'s";
        }
        if (Vocab.IsInNameSense(wordSense))
        {
            if (IsCurrentPlayer(wordSense, environment))
            {
                if (POSSESIVE == mode)
                {
                    return "your";
                }
                else if (POSSESIVE_ADDR == mode)
                {
                    return "yours";
                }
                else
                {
                    return "you";
                }
            }
            else
            {
                return word;
            }
        }
        else
        {
            if (null != dependent) { word = $"{dependent} {word}"; }
            return word;
        }
    }

    private static bool IsCurrentPlayer(string wordSense, Environment environment)
    {
        return environment.GetUser().GetNameSense() == wordSense;
    }
}
