using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicrophoneKeeper : MonoBehaviour
{
    private string _microphoneDevice;
    private AudioClip _microphoneWorkingAudioClip = null;
    private int _currentSamplePosition = 0;
    private int _previousSamplePosition = 0;

    public const int SAMPLE_RATE = 44100; // This is a rate that works well with video recorder (for data logging purposes)

    // Start is called before the first frame update
    void Start()
    {
        _microphoneDevice = Microphone.devices[0];
        _microphoneWorkingAudioClip = Microphone.Start(_microphoneDevice, true, 1, SAMPLE_RATE);
    }

    // Update is called once per frame
    void Update()
    {
        _previousSamplePosition = _currentSamplePosition;
        _currentSamplePosition = Microphone.GetPosition(_microphoneDevice);
    }

    void OnDestroy()
    {
        Microphone.End(_microphoneDevice);
    }

    public int ChannelCount()
    {
        return _microphoneWorkingAudioClip.channels;
    }    

    public float[] GetNewData()
    {
        int dataLen = _currentSamplePosition - _previousSamplePosition;
        if (dataLen < 0) { dataLen += _microphoneWorkingAudioClip.samples * _microphoneWorkingAudioClip.channels; }
        float[] dataBuffer = new float[dataLen];
        if (0 == dataLen) return dataBuffer;
        _microphoneWorkingAudioClip.GetData(dataBuffer, _previousSamplePosition);
        return dataBuffer;
    }
}
