using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Letter : MonoBehaviour {
    public void Setup(char letter, int graphemePosition, Color color, float opacityLevel, float height) {
        letter = char.ToLower(letter);
        this.letter = letter;
        this.graphemePosition = graphemePosition;
        SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        Texture2D texture = Resources.Load<Texture2D>("Visual/Letters/" + char.ToUpper(letter));
        float pixelsPerUnit = texture.height / height;
        spriteRenderer.sprite = Sprite.Create(texture,
                                              new Rect(0, 0, texture.width, texture.height),
                                              new Vector2(0.5f, 0.5f),
                                              pixelsPerUnit);
        //sprite.packingRotation
        spriteRenderer.color = color;
        Opacity opacity = gameObject.AddComponent<Opacity>();
        Opacity.SetOpacity(gameObject, opacityLevel);
        gameObject.name = "letter-" + letter;
    }

    public char GetLetter() {
        return letter;
    }

    public int GetGraphemePosition() {
        return graphemePosition;
    }

    private int graphemePosition;
    private char letter;
}
