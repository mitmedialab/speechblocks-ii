using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SimpleJSON;

public class AvatarPicker : Panel {
    private Environment environment = null;
    private AvatarControl avatar = null;
    private AvatarSelectorPanel avatarPanel = null;
    private SynthesizerController synthesizer = null;

    private Dictionary<string, string[]> possibleObjects;
    private Dictionary<string, Color32[]> possibleColors;

    private string nameSense = "";
    private string imageNameSense = "";

    private static Dictionary<string, string> PROPERTY_NAMES = new Dictionary<string, string>() {  {"Accessory", "Glasses" },
                                                                                                    {"AccessoryColor", "Glasses colors" },
                                                                                                    {"Eyebrows;Eyes;Mouth", "Faces"},
                                                                                                    {"Hair", "Hairs"},
                                                                                                    {"HairColor", "Hair colors"},
                                                                                                    {"FacialHair", "Beards and moustaches"},
                                                                                                    {"FacialHairColor", "Beard and moustache colors"},
                                                                                                    {"Hat", "Hats"},
                                                                                                    {"HatColor", "Hat colors"},
                                                                                                    {"Clothes", "Clothes"},
                                                                                                    {"ClothesColor", "Clothes colors"},
                                                                                                    {"SkinColor", "Skin colors"}};


    // Use this for initialization
    public void Start() {
        SetupPanel();

        GameObject stageObject = GameObject.FindWithTag("StageObject");
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        environment = stageObject.GetComponent<Environment>();

        avatar = transform.Find("Avatar").GetComponent<AvatarControl>();
        avatarPanel = transform.Find("AvatarPanel").GetComponent<AvatarSelectorPanel>();

        possibleObjects = new Dictionary<string, string[]>();
        possibleColors = new Dictionary<string, Color32[]>();

        possibleObjects.Add("Accessory", new string[] { "None", "Round", "Rectangular", "Kurt", "RoundSunglasses", "RectangularSunglasses", "KurtSunglasses" });

        possibleObjects.Add("Eyebrows;Eyes;Mouth", new string[] { "DefaultNatural;Default;Twinkle", "ExcitedNatural;Happy;Smile", "ExcitedNatural;Hearts;Smile", "UpDownNatural;Wink;Twinkle", "UpDownNatural;Wink;Tongue", "ConcernedNatural;Cry;Sad", "UnibrowNatural;Default;Serious", "AngryNatural;Default;Concerned", "AngryNatural;Default;Grimace", "ExcitedNatural;Surprised;Disbelief" });

        possibleObjects.Add("Hair", new string[] { "ShortHairShaggyMullet", "ShortHairShaggy", "ShortHairShortWaved", "ShortHairShortCurly", "ShortHairShortRound", "ShortHairTheCaesarSidePart", "LongHairBun", "ShortHairFrizzle", "ShortHairDreads01", "ShortHairDreads02", "LongHairDreads", "LongHairFro", "LongHairCurly", "LongHairCurvy", "LongHairStraight", "LongHairShavedSides", "LongHairStraight2", "LongHairStraightStrand", "LongHairBob", "LongHairMiaWallace", "ShortHairSides", "None"});

        possibleObjects.Add("FacialHair", new string[] { "None", "Moustache", "MoustacheLarge", "BeardSmall", "BeardMedium", "BeardLarge", "BeardXLarge", "BeardLong" });

        possibleObjects.Add("Hat", new string[] { "None", "Hat", "WinterHat1", "WinterHat2", "WinterHat3", "Flowers", "Band", "Hijab", "Turban"});

        possibleObjects.Add("Clothes", new string[] { "ShirtCrewNeck", "ShirtVNeck", "ShirtCrewNeck-Deer", "ShirtCrewNeck-Bat", "ShirtCrewNeck-Pizza", "ShirtCrewNeck-Diamond", "Hoodie-HoodieStrings", "Overall-Buttons", "Blazer-BlazerDeco" });

        possibleColors.Add("SkinColor", new Color32[] {  new Color32(255, 227, 159, 255), // yellow
                                                    new Color32(255, 219, 178, 255), // pale
                                                    new Color32(236, 186, 137, 255), // light
                                                    new Color32(255, 153, 65, 255), // tanned
                                                    new Color32(211, 139, 89, 255), // brown
                                                    new Color32(174, 94, 40, 255), // dark brown
                                                    new Color32(97, 66, 52, 255) //black
                                                 });

        possibleColors.Add("HairColor", new Color32[] { new Color32(237, 220, 187, 255), // platinum
                                                        new Color32(215, 179, 113, 255), // blonde golden
                                                        new Color32(182, 129, 69, 255), // blonde
                                                        new Color32(163, 88, 39, 255), // auburn
                                                        new Color32(113, 64, 52, 255), // brown
                                                        new Color32(73, 48, 44, 255), // dark brown
                                                        new Color32(44, 26, 24, 255), // black
                                                        new Color32(203, 52, 36, 255), // red
                                                        new Color32(247, 151, 150, 255), // pastel pink
                                                        new Color32(255, 179, 71, 255), // pastel orange
                                                        new Color32(166, 255, 195, 255), // pastel green
                                                        new Color32(178, 227, 251, 255), // pastel blue
                                                        new Color32(81, 153, 227, 255), // blue-2
                                                        new Color32(204, 107, 177, 255), // pastel violet
                                                        new Color32(0xcb, 0x5c, 0x0d, 255) // ginger
                                                       });

        possibleColors.Add("FacialHairColor", possibleColors["HairColor"]);

        possibleColors.Add("ClothesColor", new Color32[] {  new Color32(38, 46, 54, 255), // black
                                                            new Color32(146, 149, 154, 255), // gray-2
                                                            new Color32(230, 230, 230, 255), // gray-1
                                                            new Color32(255, 255, 255, 255), // white
                                                            new Color32(255, 92, 92, 255), // red
                                                            new Color32(255, 175, 186, 255), // pastel red
                                                            new Color32(255, 179, 71, 255), // pastel orange
                                                            new Color32(0x87, 0x5c, 0x36, 255), // pastel brown
                                                            new Color32(255, 255, 178, 255), // pastel yellow
                                                            new Color32(0x3c, 0xb0, 0x43, 255), // green
                                                            new Color32(166, 255, 195, 255), // pastel green
                                                            new Color32(178, 227, 251, 255), // pastel blue
                                                            new Color32(101, 201, 247, 255), // blue-1
                                                            new Color32(81, 153, 227, 255), // blue-2
                                                            new Color32(36, 84, 125, 255), // blue-3
                                                            new Color32(255, 72, 142, 255), // pink
                                                            new Color32(204, 107, 177, 255) // pastel violet
                                                            });

        possibleColors.Add("AccessoryColor", possibleColors["ClothesColor"]);

        possibleColors.Add("HatColor", possibleColors["ClothesColor"]);

        InitializeUI();
    }

    public override string GetSortingLayer()
    {
        return "avatar_picker";
    }

    public void Deploy(string nameSense, bool deployInstantly)
    {
        this.nameSense = nameSense;
        this.imageNameSense = GameObject.FindWithTag("StageObject").GetComponent<Vocab>().GetIconicImageable(nameSense);
        avatar.SetupByNameSense(imageNameSense);
        UpdateUI();
        Deploy(deployInstantly);
    }

    public void UpdateUI()
    {
        foreach (string property in possibleObjects.Keys)
        {
            string colorProperty = property + "Color";
            if (possibleColors.ContainsKey(colorProperty))
            {
                transform.Find(colorProperty).gameObject.SetActive("None" != avatar.GetProperty(property));
            }
        }
        foreach (string property in possibleColors.Keys)
        {
            Color32 color = avatar.GetColorProperty(property);
            Transform colorButton = transform.Find(property);
            if (null != colorButton)
            {
                colorButton.Find("sample").GetComponent<SpriteRenderer>().color = color;
            }
        }
    }

    public void OnSaveButtonTap()
    {
        Debug.Log("Save button tap");
        Retract(save: true, retractInstantly: false);
    }

    public void Retract(bool save, bool retractInstantly)
    {
        if (save) { environment.UpdateAvatar(imageNameSense, avatar.GetDescription()); }
        Retract(retractInstantly);
    }

    public AvatarSelectorPanel GetSelectorPanel()
    {
        return transform.Find("AvatarPanel").GetComponent<AvatarSelectorPanel>();
    }

    public string GetNameSense()
    {
        return nameSense;
    }

    private void InitializeUI()
    {
        foreach (string property in possibleObjects.Keys.Concat(possibleColors.Keys))
        {
            GameObject propertyButton = transform.Find(property).gameObject;
            propertyButton.AddComponent<GenericTappable>().AddAction(() => ButtonCallBack(propertyButton, property));
            Logging logging = propertyButton.AddComponent<Logging>();
            logging.Setup($"avatarproperty-{property}");
        }
    }

    private void ButtonCallBack(GameObject button, string property)
    {
        if (!avatarPanel.IsRetracted()) return;
        IEnumerable<string> options = null;
        if (property.EndsWith("Color"))
        {
            options = possibleColors[property].Select(color => SerializationUtil.SerializeColor(color).ToString()).ToList();
        }
        else
        {
            options = possibleObjects[property];
        }
        synthesizer.Speak(PROPERTY_NAMES[property], cause: Logging.GetObjectLogID(button), keepPauses: false, boundToStages: "avatar_picker+avatar_selector_panel");
        avatarPanel.Deploy(button, property, options);
    }
}
