using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WordsArea : MonoBehaviour {
    private GameObject wordBankButtonPrefab;
    private List<ButtonDistributor<string>> buttonDistributors = new List<ButtonDistributor<string>>(); // one distributor for imageable, and one - for non-imageable buttons
    private AnimationMaster animaster;
    private Vocab vocab;
    private ButtonsArranger buttonsArranger;

    private void Start() {
        wordBankButtonPrefab = Resources.Load<GameObject>("Prefabs/word_bank_button");
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        animaster = stageObject.GetComponent<AnimationMaster>();
        vocab = stageObject.GetComponent<Vocab>();
        GameObject areaBg = transform.Find("words_area_bg").gameObject;
        SpriteRenderer bgRenderer = areaBg.GetComponent<SpriteRenderer>();
        float wordBankButtonSize = ButtonsArranger.GetButtonWidth(wordBankButtonPrefab);
        float areaWidth = bgRenderer.size.x * areaBg.transform.localScale.x - 0.2f;
        float areaHeight = bgRenderer.size.y * areaBg.transform.localScale.y - 0.2f;
        buttonsArranger = new ButtonsArranger(  x0: 0,
                                                y0: 0,
                                                areaWidth: areaWidth,
                                                areaHeight: areaHeight,
                                                buttonWidth: wordBankButtonSize,
                                                buttonHeight: wordBankButtonSize);
        GameObject buttonsHolder = transform.Find("buttons_holder").gameObject;
        for (int i = 0; i < 2; ++i)
        {
            buttonDistributors.Add(new ButtonDistributor<string>(   buttonsHolder: buttonsHolder,
                                                                    buttonInitializer: SpawnButton,
                                                                    buttonArranger: buttonsArranger,
                                                                    animaster: animaster,
                                                                    previousDistributor: 0 == i ? null : buttonDistributors[i-1]));
        }
    }

    private void Update()
    {
        for (int i = 0; i < buttonDistributors.Count; ++i) { buttonDistributors[i].Update(); }
    }

    public void Deploy(GameObject origin, List<string> wordSenses) {
        Clear();
        DeployAdditive(origin, wordSenses);
    }

    public bool DeploymentComplete()
    {
        return buttonDistributors.All(distributor => distributor.IsCompleted());
    }

    public List<string> DeployAdditive(GameObject origin, List<string> wordSenses)
    {
        List<string> imageableWordSenses = wordSenses.Where(sense => vocab.IsImageable(Vocab.GetWord(sense))).ToList();
        List<string> nonImageableWordSenses = wordSenses.Where(sense => !imageableWordSenses.Contains(sense)).ToList();
        List<string> actuallyAdded = buttonDistributors[0].AddButtons(imageableWordSenses, origin);
        actuallyAdded.AddRange(buttonDistributors[1].AddButtons(nonImageableWordSenses, origin));
        return actuallyAdded;
    }

    public void Clear() {
        for (int i = 0; i < buttonDistributors.Count; ++i) { buttonDistributors[i].Clear(); }
    }

    public int TargetWordCount()
    {
        return buttonDistributors[buttonDistributors.Count - 1].EndingPosition();
    }

    public int Capacity()
    {
        return buttonsArranger.MaxButtons();
    }

    private GameObject SpawnButton(string wordSense)
    {
        GameObject button = Instantiate(wordBankButtonPrefab);
        button.GetComponent<WordBankButton>().Setup(wordSense);
        return button;
    }
 }
