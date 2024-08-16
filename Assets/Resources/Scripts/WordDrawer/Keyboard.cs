using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Keyboard : MonoBehaviour {
    [SerializeField]
    private int n_rows = 3;
    [SerializeField]
    private float rel_space = 0.1f;
    private List<GameObject> letters = new List<GameObject>();

	// Use this for initialization
	void Start () {
        if (0 != letters.Count) return;
        GameObject keyPrefab = Resources.Load<GameObject>("Prefabs/KeyboardKey");
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        float width = spriteRenderer.size.x;
        float height = spriteRenderer.size.y;
        int n_letr = 'Z' - 'A' + 1;
        int max_n_in_row = (int)Mathf.Ceil((float)n_letr / n_rows);
        float cell_width = Mathf.Min(width / (max_n_in_row + rel_space * (max_n_in_row + 1)),
                                     height / (n_rows + rel_space * (n_rows + 1)));
        float space_width = cell_width * rel_space;
        float space_height = (height - n_rows * cell_width) / (n_rows + 1);
        for (int c = 0; c < n_letr; ++c)
        {
            int rowI = c / max_n_in_row;
            int iInRow = c % max_n_in_row;
            int cap = (rowI + 1) * max_n_in_row;
            int n_in_row = cap <= n_letr ? max_n_in_row : max_n_in_row - (cap - n_letr);
            float x_offset = -0.5f * (cell_width * n_in_row + space_width * (n_in_row - 1));
            float y_offset = 0.5f * (cell_width * n_rows + space_height * (n_rows - 1));
            float x = x_offset + (cell_width + space_width) * iInRow + 0.5f * cell_width;
            float y = y_offset - (cell_width + space_height) * rowI - 0.5f * cell_width;
            char letr = (char)('A' + c);
            GameObject letterButton = CreateLetterButton(letr, keyPrefab);
            letterButton.transform.SetParent(transform, false);
            letterButton.transform.localPosition = new Vector3(x, y, -1f);
            float scale = cell_width / letterButton.GetComponent<KeyboardKey>().GetWidth();
            letterButton.transform.localScale = new Vector3(scale, scale, 1);
            letters.Add(letterButton);
        }
	}

    public List<GameObject> GetLetters() {
        if (0 == letters.Count) Start();
        return letters;
    }
	
    private GameObject CreateLetterButton(char letter, GameObject keyPrefab)
    {
        letter = char.ToLower(letter);
        string letterString = letter.ToString();
        GameObject keyObject = Instantiate(keyPrefab);
        KeyboardKey key = keyObject.GetComponent<KeyboardKey>();
        key.Setup(new PGPair(PhonemeUtil.GetDefaultPhoneme(letterString), letterString), "kb-setup");
        ZSorting.SetSortingLayer(keyObject, "word_drawer");
        return keyObject;
    }
}
