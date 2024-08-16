using UnityEngine;

public class GalleryRecycleBin : MonoBehaviour, IDropArea {
    private StageOrchestrator stageOrchestrator = null;
    private Gallery gallery;
    private SynthesizerController synthesizer;

    private int lastSpeechID = -1;

    void Start()
    {
        GameObject stageObject = GameObject.FindWithTag("StageObject");
        stageOrchestrator = stageObject.GetComponent<StageOrchestrator>();
        gallery = GameObject.FindWithTag("Gallery").GetComponent<Gallery>();
        synthesizer = stageObject.GetComponent<SynthesizerController>();
    }

    public bool AcceptDrop(GameObject droppedObject) {
        if ("gallery" != stageOrchestrator.GetStage()) return false;
        GalleryButton galleryButton = droppedObject.GetComponent<GalleryButton>();
        if (null == galleryButton) return false;
        gallery.DeleteScene(galleryButton.GetSceneID());
        return true;
    }

    public void OnTap(TouchInfo touchInfo)
    {
        if (synthesizer.IsSpeaking(lastSpeechID)) return;
        string prompt = "This bin is for pictures that you don't want anymore. If you want to delete a picture, drag it here with your finger.";
        lastSpeechID = synthesizer.Speak(prompt, cause: "flipper:tap");
    }
}
