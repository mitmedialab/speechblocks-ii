using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Inscription : MonoBehaviour
{
    private static GameObject inscriptionPrefab = null;

    public static GameObject Create(string word, float allowedWidth, float allowedHeight, Color color, string sortingLayer)
    {
        if (null == inscriptionPrefab)
        {
            inscriptionPrefab = Resources.Load<GameObject>("Prefabs/Inscription");
        }
        GameObject inscription = GameObject.Instantiate(inscriptionPrefab);
        inscription.GetComponent<Inscription>().Setup(word, allowedWidth, allowedHeight, color, sortingLayer);
        return inscription;
    }

    public void Setup(string word, float allowedWidth, float allowedHeight, Color color, string sortingLayer)
    {
        float charSize = Mathf.Min(allowedWidth / word.Length, allowedHeight);
        float totalWidth = charSize * word.Length;
        for (int i = 0; i < word.Length; ++i)
        {
            if (!char.IsLetter(word[i])) continue;
            GameObject letterObject = new GameObject();
            letterObject.transform.SetParent(transform, false);
            letterObject.transform.localPosition = new Vector3(-0.5f * totalWidth + (0.5f + i) * charSize, 0, -1);
            letterObject.AddComponent<Letter>().Setup(word[i], i, color, 1, charSize);
            ZSorting.SetSortingLayer(letterObject, sortingLayer);
        }
    }

    public static void GatherInscriptions(GameObject gameObject, List<GameObject> inscriptions)
    {
        if (null != gameObject.GetComponent<Inscription>())
        {
            inscriptions.Add(gameObject);
        }
        else
        {
            foreach (Transform child in gameObject.transform)
            {
                GatherInscriptions(child.gameObject, inscriptions);
            }
        }
    }
}
