using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class Gallery : Panel
{
    private GameObject galleryPagePrefab = null;
    private GameObject galleryButtonPrefab = null;
    private AnimationMaster animationMaster;
    private List<GameObject> pages = new List<GameObject>();
    private List<GalleryButton> buttons = new List<GalleryButton>();
    private Environment environment;
    private ConversationMaster convemaster;
    private ButtonsArranger buttonsArranger;
    private Tutorial tutorial;

    public void Start()
    {
        SetupPanel();
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        environment = stageObject.GetComponent<Environment>();
        animationMaster = stageObject.GetComponent<AnimationMaster>();
        tutorial = stageObject.GetComponent<Tutorial>();
        //ideaMaster = stageObject.GetComponent<IdeaMaster>();
        galleryPagePrefab = Resources.Load<GameObject>("Prefabs/GalleryPage");
        galleryButtonPrefab = Resources.Load<GameObject>("Prefabs/GalleryButton");
        convemaster = stageObject.GetComponent<ConversationMaster>();
        float pageSizeY = 2 * Camera.main.orthographicSize;
        float pageSizeX = Camera.main.aspect * pageSizeY;
        buttonsArranger = new ButtonsArranger(x0: 1f, y0: 0,
            areaWidth: pageSizeX - 2f, areaHeight: pageSizeY - 1f,
            buttonWidth: ButtonsArranger.GetButtonWidth(galleryButtonPrefab), buttonHeight: ButtonsArranger.GetButtonHeight(galleryButtonPrefab));
    }

    public void ConfigureUI()
    {
        foreach (Transform galleryPrefabChild in galleryPagePrefab.transform)
        {
            if (galleryPagePrefab.name == "idea_button")
            {
                galleryPrefabChild.gameObject.SetActive(environment.GetUser().InChildDrivenCondition());
                break;
            }
        }
    }

    public override string GetSortingLayer()
    {
        return "gallery";
    }

    public void Setup(List<string> scenes)
    {
        int totalButtonsCount = scenes.Count + 1;
        int pagesCount = totalButtonsCount / buttonsArranger.MaxButtons() + ((totalButtonsCount % buttonsArranger.MaxButtons() == 0) ? 0 : 1);
        for (int i = 0; i < pagesCount; ++i) { AddPage(); }
        for (int i = 0; i < totalButtonsCount; ++i)
        {
            string sceneID = null;
            if (i < scenes.Count) { sceneID = scenes[i]; }
            buttons.Add(CreateGalleryButton(i, sceneID));
        }
    }

    public void Reset()
    {
        Retract(retractInstantly: true);
        foreach (GalleryButton button in buttons)
        {
            Destroy(button.gameObject);
        }
        foreach (GameObject page in pages)
        {
            Destroy(page);
        }
        pages.Clear();
        buttons.Clear();
    }

    public override bool IsDeployed()
    {
        return transform.position.y <= 0;
    }

    public int ButtonsOnCurrentPage()
    {
        int currentPageID = CurrentPageID();
        if (currentPageID == pages.Count - 1) { return buttons.Count - buttonsArranger.MaxButtons() * currentPageID; }
        else { return buttonsArranger.MaxButtons(); }
    }

    public int PageCount()
    {
        return pages.Count;
    }

    public int CurrentPageID()
    {
        int pageNum = pages.Count - 1 - Mathf.RoundToInt(-transform.position.y / 10);
        if (pageNum < 0) return 0;
        if (pageNum >= pages.Count) return pages.Count - 1;
        return pageNum;
    }

    public List<GameObject> GetSceneButtons()
    {
        return buttons.Select(button => button.gameObject).ToList();
    }

    public List<GameObject> GetButtonsForCurrentScenesAndNewScene(out GameObject newSceneButton)
    {
        newSceneButton = null;
        List<GameObject> currentSceneButtons = new List<GameObject>();
        foreach (GalleryButton button in buttons)
        {
            if (null == button.GetSceneID())
            {
                newSceneButton = button.gameObject;
            }
            else
            {
                currentSceneButtons.Add(button.gameObject);
            }
        }
        return currentSceneButtons;
    }

    public List<GameObject> GetArrows()
    {
        List<GameObject> arrows = new List<GameObject>();
        for (int i = 0; i < pages.Count; ++i)
        {
            if (i > 0)
            {
                arrows.Add(pages[i].transform.Find("up-handle").gameObject);
            }
            if (i < pages.Count - 1)
            {
                arrows.Add(pages[i].transform.Find("dn-handle").gameObject);
            }
        }
        return arrows;
    }

    public List<GameObject> GetPageElements(string elementName)
    {
        List<GameObject> selection = new List<GameObject>();
        foreach (GameObject galleryPage in pages)
        {
            foreach (Transform galleryGrandchild in galleryPage.transform)
            {
                if (galleryGrandchild.name == elementName)
                {
                    selection.Add(galleryGrandchild.gameObject);
                }
            }
        }
        return selection;
    }

    public void DeleteScene(string sceneID)
    {
        int sceneIndex = buttons.FindIndex(button => button.GetSceneID() == sceneID);
        Destroy(buttons[sceneIndex].gameObject);
        buttons.RemoveAt(sceneIndex);
        int minButtonToMove = sceneIndex;
        if (0 == buttons.Count % buttonsArranger.MaxButtons())
        {
            RemovePage();
            minButtonToMove = 0;
        }
        for (int i = minButtonToMove; i < buttons.Count; ++i)
        {
            buttons[i].SetTargetPosition(ButtonTargetPosition(i));
        }
        environment.DeleteScene(sceneID);
    }

    public void Close()
    {
        convemaster.StartGoodbye();
    }

    public void SaveCurrentScene()
    {
        Composition composition = GameObject.FindWithTag("CompositionRoot").GetComponent<Composition>();
        string sceneID = composition.Push(makeSnapshot: true);
        if (null != sceneID)
        {
            int existingButtonIndex = buttons.FindIndex(button => button.GetSceneID() == sceneID);
            if (existingButtonIndex < 0)
            {
                AddScene(sceneID);
            }
            else
            {
                GalleryButton button = buttons[existingButtonIndex];
                button.UpdateImage();
                button.transform.localPosition = new Vector3(0, -10, -1);
                int pageI = (int)(existingButtonIndex / buttonsArranger.MaxButtons());
                animationMaster.StartGlide(gameObject, new Vector3(0, -pages[pageI].transform.localPosition.y, 0), 0.25f);
            }
        }
    }

    private void AddPage()
    {
        int pageNumber = pages.Count;
        GameObject pageObject = Instantiate(galleryPagePrefab);
        Transform pageTransform = pageObject.transform;
        pageTransform.SetParent(transform, false);
        if (0 == pageNumber)
        {
            Transform handleTransform = pageTransform.Find("up-handle");
            handleTransform.GetComponent<GenericTappable>().enabled = false;
            handleTransform.GetComponent<CircleCollider2D>().enabled = false;
            Opacity.SetOpacity(handleTransform.gameObject, 0.25f);
        }
        string[] handleLabels = { "up-handle", "dn-handle" };
        for (int i = 0; i < 2; ++i)
        {
            string handleLabel = handleLabels[i];
            GenericTappable tappable = pageTransform.Find(handleLabel).GetComponent<GenericTappable>();
            bool up = (0 == i);
            tappable.AddAction(() => GoToNextPage(up, pageNumber));
        }
        pageTransform.Find("cross_button").GetComponent<GenericTappable>().AddAction(Close);
        pageTransform.Find("help_button").GetComponent<GenericTappable>().AddAction(() => tutorial.TriggerHelp());
        if (!environment.GetUser().InChildDrivenCondition()) { pageTransform.Find("idea_button").gameObject.SetActive(false); }
        pages.Add(pageObject);
        for (int i = 0; i < pages.Count; ++i)
        {
            pages[i].transform.localPosition = new Vector3(0, PageTargetY(i), 0);
        }
    }

    private void RemovePage()
    {
        if (0 == pages.Count) return;
        Destroy(pages[pages.Count - 1]);
        pages.RemoveAt(pages.Count - 1);
        foreach (GameObject galleryObject in pages.Concat(buttons.Select(button => button.gameObject)))
        {
            Vector3 oldPos = galleryObject.transform.localPosition;
            galleryObject.transform.localPosition = new Vector3(oldPos.x, oldPos.y - 10, oldPos.z);
        }
        Vector3 old_gallery_position = transform.position;
        transform.position = new Vector3(old_gallery_position.x, old_gallery_position.y + 10, old_gallery_position.z);
    }

    private void GoToNextPage(bool up, int pageNumber)
    {
        if (up)
        {
            if (0 == pageNumber) { return; }
            else { GoToPage(pageNumber - 1); }
        }
        else
        {
            if (pages.Count - 1 == pageNumber) { Retract(retractInstantly: false); }
            else { GoToPage(pageNumber + 1); }
        }
    }

    private void GoToPage(int pageNumber)
    {
        animationMaster.StartGlide(gameObject, new Vector2(0, -PageTargetY(pageNumber)), 0.25f);
    }

    private GalleryButton CreateGalleryButton(int buttonIndex, string sceneID)
    {
        GalleryButton galleryButton = Instantiate(galleryButtonPrefab).GetComponent<GalleryButton>();
        Transform galleryButtonTransform = galleryButton.transform;
        galleryButtonTransform.SetParent(transform);
        Vector3 targetPosition = ButtonTargetPosition(buttonIndex);
        galleryButtonTransform.localPosition = targetPosition;
        galleryButton.SetTargetPosition(targetPosition);
        galleryButton.Setup(sceneID);
        return galleryButton;
    }

    private float PageTargetY(int pageIndex)
    {
        return (pages.Count - 1 - pageIndex) * 10;
    }

    private Vector3 ButtonTargetPosition(int buttonIndex)
    {
        float pageY = PageTargetY(buttonIndex / buttonsArranger.MaxButtons());
        int indexOnPage = buttonIndex % buttonsArranger.MaxButtons();
        return new Vector3(buttonsArranger.GetButtonX(indexOnPage), pageY + buttonsArranger.GetButtonY(indexOnPage), -1);
    }

    protected override void OnDeploy()
    {
        SaveCurrentScene();
    }

    private void AddScene(string sceneID)
    {
        GalleryButton newButton = CreateGalleryButton(buttons.Count - 1, sceneID);
        newButton.transform.localPosition = new Vector3(0, -10, -1);
        buttons.Insert(buttons.Count - 1, newButton);
        int firstRealignIndex;
        if (1 == buttons.Count % buttonsArranger.MaxButtons())
        {
            AddPage();
            firstRealignIndex = 0;
        }
        else
        {
            firstRealignIndex = buttons.Count - 1;
        }
        for (int i = firstRealignIndex; i < buttons.Count; ++i)
        {
            buttons[i].SetTargetPosition(ButtonTargetPosition(i));
        }
    }
}
