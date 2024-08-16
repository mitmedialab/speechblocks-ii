using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WordBankButton : MonoBehaviour, ITappable, IDetailedLogging
{
    private string wordSense;
    private Vocab vocab;
    private string word;
    private static GameObject writeButtonPrefab = null;
    private WordDrawer wordDrawer;
    private SynthesizerController synthesizerHelper = null;
    private AnimationMaster animaster = null;
    private Environment environment;

	// Use this for initialization
    public void Setup(string wordSense)
    {
        if (null == writeButtonPrefab) { writeButtonPrefab = Resources.Load<GameObject>("Prefabs/WriteButton"); }
        wordDrawer = GameObject.FindWithTag("WordDrawer").GetComponent<WordDrawer>();
        this.wordSense = wordSense;
        word = Vocab.GetWord(wordSense);
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        synthesizerHelper = stageObject.GetComponent<SynthesizerController>();
        animaster = stageObject.GetComponent<AnimationMaster>();
        vocab = stageObject.GetComponent<Vocab>();
        environment = stageObject.GetComponent<Environment>();
        GetComponent<Picture>().Setup(wordSense, 0.9f, 0.9f, "word_drawer");
    }

    public string GetWord()
    {
        return word;
    }

    public string GetWordSense()
    {
        return wordSense;
    }

    public object[] GetLogDetails() {
        return new object[] { "word", word };
    }

    public void OnTap(TouchInfo touchInfo) {
        environment.GetRoboPartner().LookAtTablet();
        GameObject[] writeButtons = GameObject.FindGameObjectsWithTag("WriteButton");
        foreach (GameObject wButton in writeButtons) { Destroy(wButton); }
        SynQuery synQuery = vocab.GetPronunciation(wordSense, giveFullNames: true);
        GameObject writeButton = Instantiate(writeButtonPrefab);
        writeButton.transform.SetParent(transform, false);
        writeButton.transform.localPosition = new Vector3(0, 0, -3);
        ZSorting.SetSortingLayer(writeButton, "word_drawer");
        writeButton.GetComponent<WriteButton>().Setup(wordSense, () => wordDrawer.InvokeKeyboard(false), 0.8f, "word_drawer");
        Opacity.SetOpacity(writeButton, 0);
        animaster.StartFade(writeButton, 1, 0.25f);
        synthesizerHelper.Speak(synQuery, cause: Logging.GetObjectLogID(gameObject), keepPauses: false);
    }
}
