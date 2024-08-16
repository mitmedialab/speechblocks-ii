using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineDriverChoiceButton : MonoBehaviour, ITappable
{
    public void Setup(WordSuggestion word_suggestion)
    {
        if (null == writeButtonPrefab) { writeButtonPrefab = Resources.Load<GameObject>("Prefabs/WriteButton"); }
        Logging.LogChoiceButtonAssignment(gameObject, word_suggestion.GetWordSense());
        gameObject.SetActive(true);
        transform.localScale = new Vector3(0.1f, 0.1f, 1);
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        animaster = stageObject.GetComponent<AnimationMaster>();
        animaster.StartScale(gameObject, 1f, 0.25f);
        environment = stageObject.GetComponent<Environment>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        vocab = stageObject.GetComponent<Vocab>();
        this.wordSuggestion = word_suggestion;
        float picSize = 1.5f * GetComponent<CircleCollider2D>().radius;
        GetComponent<Picture>().Setup(wordSuggestion.GetWordSense(), picSize, picSize, "word_drawer");
    }

    public WordSuggestion GetWordSuggestion()
    {
        return wordSuggestion;
    }

    public bool IsDeployed()
    {
        return gameObject.activeSelf && null == GetComponent<Scale>();
    }

    public void OnTap(TouchInfo touchInfo)
    {
        environment.GetRoboPartner().LookAtTablet();
        DestroyWriteButtons();
        GameObject writeButton = Instantiate(writeButtonPrefab);
        writeButton.name = "write-button";
        writeButton.transform.SetParent(transform, false);
        writeButton.transform.localPosition = new Vector3(0, 0, -3);
        ZSorting.SetSortingLayer(writeButton, "word_drawer");
        WordDrawer wordDrawer = GameObject.FindWithTag("WordDrawer").GetComponent<WordDrawer>();
        writeButton.GetComponent<WriteButton>().Setup(wordSuggestion.GetWordSense(), () => wordDrawer.InvokeKeyboard(false), 2f, "word_drawer");
        Opacity.SetOpacity(writeButton, 0);
        animaster.StartFade(writeButton, 1, 0.25f);
        List<SynQuery> sequence = new List<SynQuery>();
        sequence.Add(vocab.GetPronunciation(Vocab.GetWord(wordSuggestion.GetWordSense())));
        if (null != wordSuggestion.GetReason())
        {
            sequence.Add(SynQuery.Break(0.3f));
            sequence.Add(wordSuggestion.GetReason());
        }
        synthesizer.Speak(SynQuery.Seq(sequence), cause: "m-choice-button-tap");
    }

    public static void DestroyWriteButtons()
    {
        GameObject[] writeButtons = GameObject.FindGameObjectsWithTag("WriteButton");
        foreach (GameObject wButton in writeButtons) { Destroy(wButton); }
    }

    private Environment environment;
    private Vocab vocab;
    private SynthesizerController synthesizer;
    private AnimationMaster animaster;
    private WordSuggestion wordSuggestion;
    private static GameObject writeButtonPrefab;
}
