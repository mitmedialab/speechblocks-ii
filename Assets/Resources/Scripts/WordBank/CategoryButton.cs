using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CategoryButton : MonoBehaviour, ITappable {
    private static GameObject glowPrefab = null;
    private GameObject glow = null;

    void Start()
    {
        if (null == glowPrefab) { glowPrefab = Resources.Load<GameObject>("Prefabs/glow"); }
        DoOnStart();
    }

    void Update()
    {

    }

    public void OnTap(TouchInfo touchInfo) {
        DoOnTap();
        TurnOnGlow();
    }

    public void Deactivate()
    {
        if (null != glow)
        {
            Destroy(glow);
            glow = null;
            DoDeactivate();
        }
    }

    public bool ButtonIsActive()
    {
        return null != glow;
    }

    public static void DeactivateButtons()
    {
        IEnumerable<CategoryButton> categoryButtons = GameObject.FindGameObjectsWithTag("CategoryButton")
                                                .Concat(GameObject.FindGameObjectsWithTag("SpeechRecoButton"))
                                                .Select(obj => obj.GetComponent<CategoryButton>());
        foreach (CategoryButton categoryButton in categoryButtons)
        {
            categoryButton.Deactivate();
        }
    }

    protected virtual void DoOnStart() { }

    protected virtual void DoOnTap() { }

    protected virtual void DoDeactivate() { }

    private void TurnOnGlow() {
        if (ButtonIsActive()) return;
        DeactivateButtons();
        glow = Instantiate(glowPrefab);
        float glowSize = 1.25f * Block.GetStandardHeight() / (1.75f * transform.localScale.x);
        glow.GetComponent<Glow>().Invoke(glowSize, glowSize, "word_drawer");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0, 0, -1);
    }
}
