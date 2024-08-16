using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// The letter/character pages are not currently used, but may be resurrected at some point in the future.
// I commented out the code to avoid the need to modify it as the rest of the code evolves.
public class WriteButton : MonoBehaviour, ITappable
{
    private string wordSense = null;
    private Action retractCoroutine = null;

    public void Setup(string wordSense, Action retractCoroutine, float height, string sortingLayer)
    {
        this.wordSense = wordSense;
        this.retractCoroutine = retractCoroutine;
        float currentHeight = GetComponent<SpriteRenderer>().size.y;
        float scale = height / currentHeight;
        transform.localScale = new Vector3(scale, scale, 1);
        ZSorting.SetSortingLayer(gameObject, sortingLayer);
    }

    public void OnTap(TouchInfo touchInfo)
    {
        if (null != retractCoroutine) retractCoroutine();
        Scaffolder scaffolder = GameObject.FindWithTag("StageObject").GetComponent<Scaffolder>();
        if (wordSense != scaffolder.GetTargetWordSense() || scaffolder.IsComplete())
        {
            scaffolder.SetTarget(wordSense, cause: transform.parent.GetComponent<Logging>().GetLogID());
        }
        else
        {
            scaffolder.Reprompt();
        }
    }

    public string GetWordSense() { return wordSense; }
}
