using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.Linq;

public class FixedCategoryButton : CategoryButton, IDetailedLogging
{
	private string categoryName = null;
	private SynthesizerController synthesizer;
	private List<string> categoryWordSenses = new List<string>();
	private Environment environment;

	// Use this for initialization
	public void Setup(JSONNode categoryDescriptor)
	{
		GameObject stageObject = GameObject.FindWithTag("StageObject");
		synthesizer = stageObject.GetComponent<SynthesizerController>();
		environment = stageObject.GetComponent<Environment>();
		this.categoryName = categoryDescriptor["title"];
		this.categoryWordSenses.AddRange(categoryWordSenses);
		GameObject myImage = new GameObject();
		myImage.transform.SetParent(transform, false);
		myImage.transform.localPosition = new Vector3(0, 0, -1);
		SpriteRenderer spriteRenderer = myImage.AddComponent<SpriteRenderer>();
		float scale = transform.localScale.x;
		float size = 0.714f * 0.85f * Block.GetStandardHeight() / scale;
		spriteRenderer.sprite = SpriteUtil.LoadInternalSpriteOfSize("Visual/Categories/" + categoryDescriptor["icon"], size, size);
		spriteRenderer.sortingLayerName = "word_drawer";
		foreach (JSONNode item in categoryDescriptor["cards"]) { categoryWordSenses.Add((string)item); }
	}

	public object[] GetLogDetails()
	{
		return new object[] { "category", categoryName };
	}

	public string GetName()
	{
		return categoryName;
	}

	protected override void DoOnTap()
    {
		if (ButtonIsActive()) return;
		environment.GetRoboPartner().LookAtTablet();
		List<string> targetWordSenses = categoryWordSenses;
		GameObject stageObject = GameObject.FindGameObjectWithTag("StageObject");
		Vocab vocab = stageObject.GetComponent<Vocab>();
		if ("people" == categoryName)
        {
			targetWordSenses = vocab.GetCustomNameSenses().Concat(categoryWordSenses).ToList();
        }
		targetWordSenses = targetWordSenses.Where(wordSense => vocab.IsInVocab(wordSense)).ToList();
		synthesizer.Speak(categoryName, cause: Logging.GetObjectLogID(gameObject), keepPauses: false);
		GameObject.FindWithTag("WordBankWordsArea").GetComponent<WordsArea>().Deploy(gameObject, targetWordSenses);
	}
}
