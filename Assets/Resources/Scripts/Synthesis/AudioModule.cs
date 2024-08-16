using System.Collections;
using UnityEngine;

public class AudioModule: ISynthesizerModule
{
    private AudioSource audioSource;
    private int lastSpeechStatus = SynthesizerController.SPEECH_STATUS_OK;

    public AudioModule(AudioSource audioSource)
    {
        this.audioSource = audioSource;
    }

    public IEnumerator Speak(SynQuery query)
    {
        lastSpeechStatus = SynthesizerController.SPEECH_STATUS_FAIL;
        AudioClip audio = (AudioClip)query.GetParam("audio");
        if (null == audio) yield break;
        yield return SoundUtils.PlayAudioCoroutine(audioSource, audio);
        lastSpeechStatus = SynthesizerController.SPEECH_STATUS_OK;
    }

    public int LastSpeechStatus()
    {
        return lastSpeechStatus;
    }

    public void Interrupt()
    {
        audioSource.Stop();
        audioSource.clip = null;
    }

    public bool IsActivelySpeaking()
    {
        return audioSource.isPlaying;
    }
}
