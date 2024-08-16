using System;
using System.Collections;

public interface ISynthesizerModule
{
    IEnumerator Speak(SynQuery query);
    int LastSpeechStatus(); // see SPEECH_STATUS_OK ... SPEECH_STATUS_FAIL in SynthesizerController
    bool IsActivelySpeaking();
    void Interrupt();
}
