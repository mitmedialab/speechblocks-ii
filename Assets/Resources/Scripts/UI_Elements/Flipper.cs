using System.Collections.Generic;
using UnityEngine;

public class Flipper : MonoBehaviour, IDragInArea {
    private AnimationMaster animaster = null;
    private StageOrchestrator stageOrchestrator = null;
    private SynthesizerController synthesizer = null;
    private bool everFlipped = false;
    private int lastSpeechID = -1;

    void Start()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        stageOrchestrator = stageObject.GetComponent<StageOrchestrator>();
        animaster = stageObject.GetComponent<AnimationMaster>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
        GetComponent<ActiveTouchArea>().Setup(gameObject);
    }

    public void OnTap(TouchInfo touchInfo)
    {
        if (synthesizer.IsSpeaking(lastSpeechID)) return;
        string prompt = "This arrow flips images around. If you want to flip an image, drag it here with your finger.";
        lastSpeechID = synthesizer.Speak(prompt, cause: "flipper:tap");
    }

    public bool EverFlipped()
    {
        return everFlipped;
    }

    public bool AcceptDragIn(GameObject draggedInObject) {
        if ("canvas" != stageOrchestrator.GetStage()) return false;
        if (null == draggedInObject.GetComponent<PictureBlock>()) return false;
        List<GameObject> inscriptions = new List<GameObject>();
        Inscription.GatherInscriptions(draggedInObject, inscriptions);
        everFlipped = true;
        Flip(draggedInObject);
        foreach (GameObject inscription in inscriptions)
        {
            Flip(inscription);
        }
        return true;
    }

    private void Flip(GameObject gameObject)
    {
        Vector3 scale = gameObject.transform.localScale;
        float targetX = scale.x > 0 ? -scale.y : scale.y;
        animaster.StartScale(gameObject, new Vector3(targetX, scale.y, 1), 0.25f);
    }
}
