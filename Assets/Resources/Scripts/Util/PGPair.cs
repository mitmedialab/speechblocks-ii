using System.Collections.Generic;
using System.Linq;

public class PGPair
{
    private string grapheme;
    private string phonemecode;

    public PGPair(string phonemecode, string grapheme)
    {
        this.phonemecode = phonemecode;
        this.grapheme = grapheme;
    }

    public string GetGrapheme() {
        return grapheme;
    }

    public string[] GetPhonemes() {
        return phonemecode.Split(';');
    }

    public IEnumerable<string> GetUnaccentuatedPhonemes() {
        return PhonemeUtil.Unaccentuated(phonemecode).Split(';');
    }

    public string GetUnaccentuatedPhonemeCode() {
        return PhonemeUtil.Unaccentuated(phonemecode);
    }

    public string GetPhonemeCode() {
        return phonemecode;
    }

    public PGPair Unaccentuated() {
        return new PGPair(PhonemeUtil.Unaccentuated(phonemecode), grapheme);
    }

    public string Code() {
        return $"{phonemecode}>{grapheme}";
    }

    public bool IsSkipPair() {
        return "" == GetPhonemes()[0];
    }

    public bool Equals(PGPair other) {
        return GetPhonemeCode().Equals(other.GetPhonemeCode()) && grapheme.Equals(other.grapheme);
    }

    public bool UnaccentuatedEquals(PGPair other) {
        return GetUnaccentuatedPhonemeCode().Equals(other.GetUnaccentuatedPhonemeCode()) && grapheme.Equals(other.grapheme);
    }

    public static List<PGPair> ParseMapping(string mapping) {
        try
        {
            List<PGPair> parsed = new List<PGPair>();
            foreach (string mpart in mapping.Split('|'))
            {
                int splitterPos = mpart.IndexOf('>');
                parsed.Add(new PGPair(mpart.Substring(0, splitterPos),
                                      mpart.Substring(splitterPos + 1, mpart.Length - splitterPos - 1)));
            }
            return parsed;
        }
        catch { return null; }
    }

    public static bool Validate(string word, List<PGPair> mapping)
    {
        if (word != Word(mapping)) return false;
        if (mapping.Any(pg => "" == pg.GetPhonemeCode() || !PhonemeUtil.IsValidPhonemeCode(pg.GetPhonemeCode()) || "" == pg.GetGrapheme())) return false;
        return true;
    }

    public static string Word(List<PGPair> mapping)
    {
        return string.Join("", mapping.Select(pg => pg.grapheme).ToArray());
    }

    public static string UnaccentuatedPhonemeCode(List<PGPair> mapping)
    {
        return string.Join(";", mapping.Select(pg => pg.GetUnaccentuatedPhonemeCode()));
    }

    public static string AccentuatedPhonemeCode(List<PGPair> mapping)
    {
        return PhonemeUtil.GetAccentuatedPhonemeCode(mapping.Select(pg => pg.GetPhonemeCode()));
    }

    public static string Transcription(List<PGPair> mapping) {
        return string.Join("|", mapping.Select(pg => pg.Code()).ToArray());
    }

    public static List<string> Phonemes(List<PGPair> mapping) {
        return mapping.SelectMany(pgpair => pgpair.GetPhonemes()).ToList();
    }
}