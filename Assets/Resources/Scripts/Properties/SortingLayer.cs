using UnityEngine;
using System.Collections.Generic;

public class ZSorting : MonoBehaviour
{
    // TODO: local variable controlling sorting layer (kind of like the Opacity class)
    public static string GetSortingLayer(GameObject gameObject) {
        Renderer spriteRenderer = gameObject.GetComponent<Renderer>();
        if (null != spriteRenderer) {
            return spriteRenderer.sortingLayerName;
        }
        foreach (Transform child in gameObject.transform) {
            string childLayer = GetSortingLayer(child.gameObject);
            if (null != childLayer) return childLayer;
        }
        return null;
    }

    public static void SetSortingLayer(GameObject gameObject, string sortingLayer) {
        Renderer spriteRenderer = gameObject.GetComponent<Renderer>();
        if (null != spriteRenderer) {
            spriteRenderer.sortingLayerName = sortingLayer;
        }
        foreach (Transform child in gameObject.transform) {
            SetSortingLayer(child.gameObject, sortingLayer);
        }
    }

    // TODO: local variable controlling sorting layer (kind of like the Opacity class)
    public static int GetSortingOrder(GameObject gameObject)
    {
        Renderer spriteRenderer = gameObject.GetComponent<Renderer>();
        if (null != spriteRenderer)
        {
            return spriteRenderer.sortingOrder;
        }
        foreach (Transform child in gameObject.transform)
        {
            int childOrder = GetSortingOrder(child.gameObject);
            if (childOrder >= 0) return childOrder;
        }
        return -1;
    }

    public static void SetSortingOrder(GameObject gameObject, int sortingOrder)
    {
        Renderer spriteRenderer = gameObject.GetComponent<Renderer>();
        if (null != spriteRenderer)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }
        foreach (Transform child in gameObject.transform)
        {
            SetSortingOrder(child.gameObject, sortingOrder);
        }
    }

    public static int SortingCompare(GameObject respA, GameObject respB)
    {
        string sortingLayerA = GetSortingLayer(respA);
        string sortingLayerB = GetSortingLayer(respB);
        if (null != sortingLayerA && null != sortingLayerB && sortingLayerA != sortingLayerB)
        {
            return SortingLayer.GetLayerValueFromName(sortingLayerA).CompareTo(SortingLayer.GetLayerValueFromName(sortingLayerB));
        }
        int sortingOrderA = ZSorting.GetSortingOrder(respA);
        int sortingOrderB = ZSorting.GetSortingOrder(respB);
        if (sortingOrderA >= 0 && sortingOrderB >= 0 && sortingOrderA != sortingOrderB)
        {
            return sortingOrderA - sortingOrderB;
        }
        return Comparer<float>.Default.Compare(respB.transform.position.z, respA.transform.position.z);
    }
}
