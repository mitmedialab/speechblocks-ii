using UnityEngine;
using SimpleJSON;

public class WordBank : MonoBehaviour {
    private float categoryButtonHeight;

	void Start () { 
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        SetupCategoriesArea();
    }

    private void SetupCategoriesArea() {
        GameObject categoryButtonPrefab = Resources.Load<GameObject>("Prefabs/category_button");
        GameObject categoriesArea = transform.Find("categories_area").gameObject;
        GameObject categoriesAreaBg = categoriesArea.transform.Find("categories_area_bg").gameObject;
        SpriteRenderer categoriesAreaRenderer = categoriesAreaBg.GetComponent<SpriteRenderer>();
        float height = categoriesAreaRenderer.size.y * categoriesAreaBg.transform.localScale.y;
        categoryButtonHeight = Block.GetStandardHeight() * categoryButtonPrefab.transform.localScale.x / 1.75f;
        float space = (height - categoryButtonHeight) / 4;
        JSONNode wordBankConfig = Config.GetConfig("WordBankConfig");
        JSONArray categories = wordBankConfig["categories"] as JSONArray;
        for (int i = 0; i < categories.Count; ++i) {
            GameObject categoryButton = Instantiate(categoryButtonPrefab);
            categoryButton.GetComponent<FixedCategoryButton>().Setup(categories[i]);
            categoryButton.transform.SetParent(categoriesArea.transform, false);
            categoryButton.transform.localPosition = new Vector3(space * (i + 1) + categoryButtonHeight * (i + 0.5f), 0, 0);
        }
        int buttonNumber = categories.Count;
        float categoriesAreaWidth = buttonNumber * categoryButtonHeight + space * (buttonNumber + 1);
        categoriesAreaRenderer.size = new Vector2(categoriesAreaWidth / categoriesAreaBg.transform.localScale.x, categoriesAreaRenderer.size.y);
        categoriesAreaBg.transform.localPosition = new Vector3(0.5f * categoriesAreaWidth, 0, 0);
    }
}
