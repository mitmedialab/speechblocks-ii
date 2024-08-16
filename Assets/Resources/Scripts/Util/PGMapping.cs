using System.Collections.Generic;

public class PGMapping
{
    public string compositeWord { get; }
    public string collapsedWord { get; }
    public string phonemecode { get; }
    public List<PGPair> pgs { get; }

    public PGMapping(string compositeWord, string collapsedWord, List<PGPair> mapping, string overridePhonemecode)
    {
        this.pgs = new List<PGPair>(mapping);
        this.compositeWord = compositeWord;
        this.collapsedWord = collapsedWord;
        phonemecode = overridePhonemecode;
    }
}