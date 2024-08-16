using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BlockBase : MonoBehaviour, IDetailedLogging {
    private PGPair pgPair = new PGPair("", "");

    private List<Letter> letters = new List<Letter>();

    private SpriteRenderer blockSprite = null;
    private float blockScale = 1;

    public static float COMPANION_OPACITY = 0.5f;
    public static float MORPH_LENGTH = 1.0f;

    public virtual void Setup(PGPair pgPair, string cause) // for blocks in letter mode
    {
        this.pgPair = pgPair;
        blockSprite = transform.Find("block").GetComponent<SpriteRenderer>();
        blockScale = blockSprite.transform.localScale.x;
        blockSprite.size = new Vector2(pgPair.GetGrapheme().Length, 1);
        Clear();
        CreateLetters();
        ZSorting.SetSortingLayer(gameObject, blockSprite.sortingLayerName);
        GetComponent<BoxCollider2D>().size = new Vector2(pgPair.GetGrapheme().Length * blockScale, blockScale);
        Logging.LogPGChange(gameObject, cause);
    }

    public object[] GetLogDetails() {
        return new object[] { "ph", pgPair.GetPhonemeCode(), "gr", pgPair.GetGrapheme() };
    }

    public string GetPhonemeCode()
    {
        return pgPair.GetPhonemeCode();
    }

    public string GetGrapheme()
    {
        return pgPair.GetGrapheme();
    }

    public void ChangePhonemecode(string phonemecode, string cause) {
        this.pgPair = new PGPair(phonemecode, pgPair.GetGrapheme());
        Logging.LogPGChange(gameObject, cause);
    }

    public PGPair GetPGPair() {
        return pgPair;
    }

    public float GetWidth() {
        return blockScale * blockSprite.size.x;
    }

    public float GetHeight() {
        return blockScale * blockSprite.size.y;
    }

    public void PlayAudio(string cause)
    {
        Logging.LogBlockPlayAudio(gameObject, cause);
        SoundUtils.PlaySound(GetSharedAudioSource(), PhonemeUtil.SoundOf(pgPair.GetUnaccentuatedPhonemeCode()));
    }

    public static bool AnyBlockIsPlaying()
    {
        return GetSharedAudioSource().isPlaying;
    }

    private void CreateLetters()
    {
        string grapheme = pgPair.GetGrapheme();
        for (int i = 0; i < grapheme.Length; ++i)
        {
            Letter letter = CreateLetter(grapheme[i], i);
            float offset = -0.5f * (grapheme.Length - 1) + i;
            letter.transform.SetParent(transform, false);
            letter.transform.localPosition = new Vector3(blockScale * offset, 0, -1);
            letters.Add(letter);
        }
    }

    private Letter CreateLetter(char letter, int grPos)
    {
        Color color = PhonemeUtil.ColorForLetter(letter);
        GameObject letterHolder = new GameObject();
        Letter letterScript = letterHolder.AddComponent<Letter>();
        letterScript.Setup(letter, grPos, color, 1, blockScale * (0.85f * blockSprite.size.y));
        return letterScript;
    }

    private void Clear() {
        foreach (Letter letter in letters) {
            if (null != letter.gameObject) { Destroy(letter.gameObject); }
        }
        letters.Clear();
    }

    private static AudioSource GetSharedAudioSource()
    {
        return GameObject.FindWithTag("StageObject").transform.Find("block-base-audio-source").GetComponent<AudioSource>();
    }
}
