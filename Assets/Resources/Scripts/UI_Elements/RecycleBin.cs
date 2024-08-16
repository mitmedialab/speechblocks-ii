using UnityEngine;

public class RecycleBin : MonoBehaviour, IDropArea {
    private TouchManager touchManager = null;
    private StageOrchestrator stageOrchestrator = null;
    private SynthesizerController synthesizer;

    private int lastSpeechID = -1;

    void Start()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        touchManager = stageObject.GetComponent<TouchManager>();
        stageOrchestrator = stageObject.GetComponent<StageOrchestrator>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
    }

    public bool AcceptDrop(GameObject droppedObject) {
        if ("canvas" != stageOrchestrator.GetStage()) return false;
        if (null == droppedObject.GetComponent<PictureBlock>()) return false;
        Destroy(droppedObject);
        return true;
    }

    public void OnTap(TouchInfo touchInfo)
    {
        if (synthesizer.IsSpeaking(lastSpeechID)) return;
        string prompt = "This bin is for images that you don't want anymore. If you want to delete an image, drag it here with your finger.";
        lastSpeechID = synthesizer.Speak(prompt, cause: "flipper:tap");
    }
}
