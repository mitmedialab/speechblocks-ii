using System.IO;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class AvatarControl : MonoBehaviour
{
    private Dictionary<string, object> description = new Dictionary<string, object>();

    private static Dictionary<string, string> DEPENDENT_COLORS = new Dictionary<string, string>() { { "HairColor", "FacialHairColor" }, { "ClothesColor", "HatColor" } };

    private static HashSet<string> HATS_HIDING_HAIR = new HashSet<string>() { "Hijab", "Turban", "WinterHat1", "WinterHat2", "WinterHat3" };

    public void SetupByNameSense(string nameSense)
    {
        Debug.Log($"SETTING UP AVATAR BY NAME {nameSense}");
        Environment environment = GameObject.FindWithTag("StageObject").GetComponent<Environment>();
        JSONNode avatarRecord = environment.GetAvatar(nameSense);
        if (null != avatarRecord)
        {
            Debug.Log("RECORD FOUND");
            Setup(avatarRecord);
        }
        else
        {
            Debug.Log("SETTING UP AS DEFAULT AVATAR");
            Setup(Config.GetConfig("DefaultAvatar"));
        }
    }

    public void Setup(JSONNode descriptionJSON)
    {
        description = new Dictionary<string, object>();
        foreach (string key in descriptionJSON.Keys)
        {
            if (key.EndsWith("Color"))
            {
                SetColorProperty(key, SerializationUtil.DeserializeColor(descriptionJSON[key]));
            }
            else
            {
                SetProperty(key, (string)descriptionJSON[key]);
            }
        }
        if (null == descriptionJSON["Hat"])
        {
            SetProperty("Hat", "None");
            SetColorProperty("HatColor", GetColorProperty("ClothesColor"));
        }
        if (null == descriptionJSON["AccessoryColor"])
        {
            SetColorProperty("AccessoryColor", new Color32(38, 46, 54, 255));
        }
    }

    public void Alter(string propertiesToAlter, string valuesToSet)
    {
        string[] propertiesToAlterArr = propertiesToAlter.Split(';');
        string[] valuesToSetArr = valuesToSet.Split(';');
        for (int i = 0; i < propertiesToAlterArr.Length; ++i)
        {
            string property = propertiesToAlterArr[i];
            string value = valuesToSetArr[i];
            if (property.EndsWith("Color"))
            {
                Color32 color = SerializationUtil.DeserializeColor(JSONNode.Parse(value));
                string dependentColorProperty = GetDependentColorProperty(property);
                if (null != dependentColorProperty) { SetColorProperty(dependentColorProperty, color); }
                SetColorProperty(property, color);
            }
            else
            {
                SetProperty(property, value);
                if ("Hat" == property && HATS_HIDING_HAIR.Contains(value))
                {
                    SetProperty("Hair", "None");
                }
            }
        }
    }

    public string GetProperty(string property)
    {
        return (string)description[property];
    }

    public Color32 GetColorProperty(string property)
    {
        return (Color32)description[property];
    }

    public string GetDescription()
    {
        JSONObject descriptionJSON = new JSONObject();
        foreach (string key in description.Keys)
        {
            if (key.EndsWith("Color"))
            {
                descriptionJSON[key] = SerializationUtil.SerializeColor((Color32)description[key]);
            }
            else
            {
                descriptionJSON[key] = (string)description[key];
            }    
        }
        return descriptionJSON.ToString();
    }

    public void SetProperty(string property, string value)
    {
        description[property] = value;
        GameObject element = transform.Find(property).gameObject;
        SpriteRenderer element_sprite = element.GetComponent<SpriteRenderer>();
        if (property == "Clothes")
        {
            SpriteRenderer decorationSpriteRenderer = transform.Find("ClothesDecoration").gameObject.GetComponent<SpriteRenderer>();
            if (value.Contains("-"))
            {
                string[] vals = value.Split('-');
                string decorationFilePath = $"Visual/AvatarPicker/{property}-{vals[1]}";
                try
                {
                    Sprite decorationSprite = SpriteUtil.LoadInternalSprite(decorationFilePath, 100);
                    decorationSpriteRenderer.sprite = decorationSprite;
                }
                catch
                {
                    Debug.Log($"Unable to load decoration sprite {decorationFilePath}");
                    decorationSpriteRenderer.sprite = null;
                }
                value = vals[0];
            }
            else
            {
                decorationSpriteRenderer.sprite = null;
            }

        }
        if ("None" == value)
        {
            element_sprite.sprite = null;
        }
        else
        {
            string filePath = $"Visual/AvatarPicker/{property}-{value}";
            try
            {
                Sprite newSprite = SpriteUtil.LoadInternalSprite(filePath, 100);
                element_sprite.sprite = newSprite;
            }
            catch
            {
                Debug.Log($"Unable to load sprite {filePath}");
                element_sprite.sprite = null;
            }
        }
    }

    public void SetColorProperty(string colorProperty, Color32 selection)
    {
        description[colorProperty] = selection;
        string baseProperty = colorProperty.Substring(0, colorProperty.Length - 5);
        SpriteRenderer spriteToRecolor = transform.Find(baseProperty).GetComponent<SpriteRenderer>();
        spriteToRecolor.color = selection;
    }

    private string GetDependentColorProperty(string colorProperty)
    {
        string dependentColorProperty = null;
        if (!DEPENDENT_COLORS.TryGetValue(colorProperty, out dependentColorProperty)) return null;
        if (!ColorUtil.Equal(GetColorProperty(colorProperty), GetColorProperty(dependentColorProperty))) return null;
        return dependentColorProperty;
    }
}