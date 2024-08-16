using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Glow : MonoBehaviour {
    public void Invoke(float width, float height, string sorting_layer) {
        GetComponent<SpriteRenderer>().size = new Vector2(width + 0.35f, height + 0.35f);
        ZSorting.SetSortingLayer(gameObject, sorting_layer);
    }
}
