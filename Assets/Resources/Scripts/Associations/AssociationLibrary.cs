using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AssociationLibrary : MonoBehaviour {
    private Dictionary<string, string[]> assocDict = new Dictionary<string, string[]>();
    private Vocab vocab = null;

    void Start() {
        if (0 != assocDict.Count) return;
        TextAsset associationsFile = Resources.Load<TextAsset>("Models/associations");
        foreach (string line in associationsFile.text.Split('\n')) {
            string[] words = line.Split(',');
            assocDict[words[0]] = words.Skip(1).Distinct().ToArray();
        }
        Resources.UnloadAsset(associationsFile);
        vocab = GetComponent<Vocab>();
    }

    public List<string> GetAssociations(string wordSense) {
        if (Vocab.IsInNameSense(wordSense))
        {
            return vocab.GetCustomNameSenses().Where(otherSense => otherSense != wordSense).ToList();
        }
        else
        {
            if (0 == assocDict.Count) Start();
            string word = Vocab.GetWord(wordSense);
            if (!assocDict.ContainsKey(word)) { return new List<string>(); }
            return assocDict[word].Where(wrd => vocab.IsInVocab(wrd) && vocab.IsImageable(wrd)).ToList();
        }
    }
}
