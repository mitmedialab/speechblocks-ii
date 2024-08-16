using SimpleJSON;

public class WordSuggestion
{
    private string word_sense;
    private string reason;

    public WordSuggestion(string word_sense, string reason)
    {
        this.word_sense = word_sense;
        this.reason = reason;
    }

    public WordSuggestion(JSONNode descriptor)
    {
        word_sense = descriptor["word-sense"];
        reason = descriptor["reason"];
    }

    public string GetWordSense() { return word_sense; }

    public string GetReason() { return reason; }
}
