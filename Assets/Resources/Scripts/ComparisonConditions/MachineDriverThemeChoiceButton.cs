using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineDriverThemeChoiceButton : MonoBehaviour, ITappable
{
    public void Setup(IdeaTemplate idea)
    {
        if (null != hideCoroutine) { StopCoroutine(hideCoroutine); hideCoroutine = null; }
        Logging.LogChoiceButtonAssignment(gameObject, idea.GetID());
        confirmButton = transform.Find("confirm-button").gameObject;
        confirmButton.SetActive(false);
        gameObject.SetActive(true);
        transform.localScale = new Vector3(0.1f, 0.1f, 1);
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        environment = stageObject.GetComponent<Environment>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        machineDriver = stageObject.GetComponent<MachineDriver>();
        animaster = stageObject.GetComponent<AnimationMaster>();
        this.idea = idea;
        float picSize = 1.5f * GetComponent<CircleCollider2D>().radius;
        GetComponent<Picture>().Setup(idea.GetIcon(), picSize, picSize, "stage_ui");
        transform.localScale = new Vector3(0.1f, 0.1f, 1);
        scale = animaster.StartScale(gameObject, 1f, duration: 0.25f);
    }

    public IdeaTemplate GetIdea()
    {
        return idea;
    }

    public void OnTap(TouchInfo touchInfo)
    {
        if (null == idea) return;
        environment.GetRoboPartner().LookAtTablet();
        synthesizer.Speak(idea.GetPrompt(), cause: "m-theme-choice-button-tap", boundToStages: "canvas+staging_area");
        HidePeerConfirmButtons();
        confirmButton.SetActive(true);
        Opacity.SetOpacity(confirmButton, 0);
        animaster.StartFade(confirmButton, 1f, 0.25f);
    }

    public bool IsDeployed()
    {
        return null != idea && (null == scale || !scale.IsGoing());
    }

    public void Hide()
    {
        idea = null;
        StartCoroutine(HideCoroutine());
    }

    public void ConfirmSelection()
    {
        machineDriver.SelectIdea(idea);
    }

    public GameObject GetConfirmButton()
    {
        return confirmButton;
    }

    private IEnumerator HideCoroutine()
    {
        idea = null;
        scale = animaster.StartScale(gameObject, 0.1f, duration: 0.25f);
        while (scale.IsGoing()) yield return null;
        gameObject.SetActive(false);
        confirmButton.SetActive(false);
        hideCoroutine = null;
    }

    private void HidePeerConfirmButtons()
    {
        foreach (MachineDriverThemeChoiceButton themeChoiceButton in MachineDriver.GetThemeChoiceButtons())
        {
            themeChoiceButton.confirmButton.SetActive(false);
        }
    }

    private Environment environment;
    private SynthesizerController synthesizer;
    private MachineDriver machineDriver;
    private AnimationMaster animaster;
    private IdeaTemplate idea;
    private Coroutine hideCoroutine = null;
    private GameObject confirmButton = null;
    private Scale scale = null;
}
