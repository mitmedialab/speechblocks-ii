using UnityEngine;

public class Opacity : MonoBehaviour
{
    private bool inHierarchy = true;
    public float localOpacity = 1;

    public void SetInHierarchy(bool inHierarchy) {
        if (inHierarchy != this.inHierarchy) {
            this.inHierarchy = inHierarchy;
            SetOpacity(gameObject, localOpacity);
        }
    }

    public bool IsInHierarchy() {
        return inHierarchy;
    }

    public static float GetOpacity(GameObject gameObject) {
        if (null == gameObject) return 1.0f;
        Opacity opacityScript = gameObject.GetComponent<Opacity>();
        if (null != opacityScript) return opacityScript.localOpacity;
        float opacity = RecursiveGetOpacity(gameObject);
        if (opacity < 0) return 1.0f;
        return opacity;
    }

    public static void SetOpacity(GameObject gameObject, float opacity) {
        if (null == gameObject) return;
        Logging.LogOpacity(gameObject, opacity, Logging.DEFAULT_CAUSE);
        Opacity opacityScript = gameObject.GetComponent<Opacity>();
        if (null != opacityScript) {
            opacityScript.localOpacity = opacity;
            RecursiveSetOpacity(GetContextOpacity(gameObject.transform.parent), gameObject.transform);
        } else {
            RecursiveSetOpacity(opacity, gameObject.transform);
        }
    }

    private static float RecursiveGetOpacity(GameObject gameObject)
    {
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (null != spriteRenderer) return spriteRenderer.color.a;
        foreach (Transform child in gameObject.transform)
        {
            if (null != child.GetComponent<Opacity>()) continue;
            float opacity = RecursiveGetOpacity(child.gameObject);
            if (opacity >= 0) return opacity;
        }
        return -1;
    }

    private static void RecursiveSetOpacity(float contextOpacity, Transform transform)
    {
        Opacity opacityScript = transform.GetComponent<Opacity>();
        if (null != opacityScript) {
            if (opacityScript.inHierarchy) {
                contextOpacity *= opacityScript.localOpacity;
            } else {
                contextOpacity = opacityScript.localOpacity;
            }
        }
        SpriteRenderer spriteRenderer = transform.gameObject.GetComponent<SpriteRenderer>();
        if (null != spriteRenderer)
        {
            Color color = spriteRenderer.color;
            color.a = contextOpacity;
            spriteRenderer.color = color;
        }
        foreach (Transform child in transform)
        {
            RecursiveSetOpacity(contextOpacity, child);
        }
    }

    private static float GetContextOpacity(Transform transform) {
        if (null == transform) {
            return 1;
        } else {
            Opacity opacity = transform.GetComponent<Opacity>();
            if (null != opacity) {
                if (!opacity.inHierarchy) {
                    return opacity.localOpacity;
                } else {
                    return opacity.localOpacity * GetContextOpacity(transform.parent);
                }
            } else {
                return GetContextOpacity(transform.parent);
            }
        }
    }
}
