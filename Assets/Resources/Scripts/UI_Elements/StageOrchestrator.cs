using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class StageOrchestrator : MonoBehaviour
{
    private List<Panel> panels;
    private Environment environment;
    private WordDrawer wordDrawer;
    private AvatarSelectorPanel avatarSelectorPanel;
    private string currentStage = "canvas";

    private Dictionary<GameObject, string> stageRoots = new Dictionary<GameObject, string>();

    // Start is called before the first frame update
    void Start()
    {
        List<GameObject> rootObjects = new List<GameObject>();
        Scene scene = SceneManager.GetActiveScene();
        scene.GetRootGameObjects(rootObjects);
        environment = GetComponent<Environment>();
        panels = rootObjects.Select(obj => obj.GetComponent<Panel>()).Where(panel => null != panel).ToList();
        panels = panels.OrderBy(panel => SortingLayer.GetLayerValueFromName(panel.GetSortingLayer())).ToList();
        wordDrawer = GameObject.FindWithTag("WordDrawer").GetComponent<WordDrawer>();
        avatarSelectorPanel = GameObject.FindWithTag("AvatarPicker").GetComponent<AvatarPicker>().GetSelectorPanel();
        InitStageRoots();
    }

    private void Update()
    {
        string stage = GetStage();
        if (currentStage != stage)
        {
            Logging.LogStageChange(stage);
            currentStage = stage;
        }
    }

    public string GetStageLayer()
    {
        for (int i = panels.Count - 1; i >= 0; --i)
        {
            Panel panel = panels[i];
            if (panel.IsDeployed())
            {
                return panel.GetSortingLayer();
            }
            else if (!panel.IsRetracted())
            {
                return null;
            }
        }
        return "Default";
    }

    public string GetStage()
    {
        for (int i = panels.Count - 1; i >= 0; --i)
        {
            Panel panel = panels[i];
            if (panel.IsDeployed())
            {
                return ElaborateStage(panel.GetSortingLayer());
            }
            else if (!panel.IsRetracted())
            {
                return null;
            }
        }
        return "canvas";
    }

    public string GetStageOfObject(GameObject obj)
    {
        string stage;
        if (null == obj) { return "canvas"; }
        else if (stageRoots.TryGetValue(obj, out stage)) { return stage; }
        else { return GetStageOfObject(obj.transform.parent?.gameObject); }
    }

    private string ElaborateStage(string panel_layer)
    {
        if ("word_drawer" == panel_layer)
        {
            if (wordDrawer.IsDisplayingKeyboard())
            {
                return "keyboard";
            }
            else if (wordDrawer.IsDisplayingWordBank())
            {
                if (environment.GetUser().InChildDrivenCondition())
                {
                    return "word_bank";
                }
                else
                {
                    return "staging_area";
                }
            }
            else
            {
                return null;
            }
        }
        else if ("avatar_picker" == panel_layer)
        {
            if (avatarSelectorPanel.IsRetracted())
            {
                return "avatar_picker";
            }
            else if (avatarSelectorPanel.IsDeployed())
            {
                return "avatar_selector_panel";
            }
            else
            {
                return null;
            }    
        }
        else
        {
            return panel_layer;
        }
    }

    private void InitStageRoots()
    {
        WordDrawer wordDrawer = GameObject.FindWithTag("WordDrawer").GetComponent<WordDrawer>();
        stageRoots[wordDrawer.GetWordBank()] = "word_bank";
        stageRoots[wordDrawer.GetStagingArea()] = "staging_area";
        stageRoots[wordDrawer.GetKeyboard()] = "keyboard";
        GameObject avatarPicker = GameObject.FindWithTag("AvatarPicker");
        stageRoots[avatarPicker] = "avatar_picker";
        stageRoots[avatarPicker.GetComponent<AvatarPicker>().GetSelectorPanel().gameObject] = "avatar_selector_panel";
        foreach (Panel panel in panels)
        {
            if (wordDrawer == panel || avatarPicker == panel) continue;
            stageRoots[panel.gameObject] = panel.GetSortingLayer();
        }
        stageRoots[GameObject.FindWithTag("Gallery").transform.Find("GalleryHandle").gameObject] = "canvas";
        stageRoots[GameObject.FindWithTag("WordDrawer").transform.Find("up-handle").gameObject] = "canvas";
    }
}
