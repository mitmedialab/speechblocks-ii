// for Save and TrimSilence, and associated functions:
//  Copyright (c) 2012 Calvin Rien
//        http://the.darktable.com
//
//  This software is provided 'as-is', without any express or implied warranty. In
//  no event will the authors be held liable for any damages arising from the use
//  of this software.
//
//  Permission is granted to anyone to use this software for any purpose,
//  including commercial applications, and to alter it and redistribute it freely,
//  subject to the following restrictions:
//
//  1. The origin of this software must not be misrepresented; you must not claim
//  that you wrote the original software. If you use this software in a product,
//  an acknowledgment in the product documentation would be appreciated but is not
//  required.
//
//  2. Altered source versions must be plainly marked as such, and must not be
//  misrepresented as being the original software.
//
//  3. This notice may not be removed or altered from any source distribution.
//
//  =============================================================================
//
//  derived from Gregorio Zanon's script
//  http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734
//
//  =============================================================================
// 
//  Modifications by Hanna Lee (February 2018):
//   - remove use of 3D parameter to AudioClip.Create() because it is deprecated
//   - use clip.samples * clip.channels to support multichannel audio

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundUtils : MonoBehaviour
{
    public const int HEADER_SIZE = 44;
    public const float RESCALE_FACTOR = 32767; // to convert float to Int16

    public static bool Save(string filename, AudioClip clip)
    {
        if (!filename.ToLower().EndsWith(".wav", StringComparison.CurrentCulture))
        {
            filename += ".wav";
        }

        var filepath = filename;

        Debug.Log(filepath);

        // Make sure directory exists if user is saving to sub dir.
        Directory.CreateDirectory(Path.GetDirectoryName(filepath));
        float[] dataFloat = new float[clip.samples * clip.channels];
        clip.GetData(dataFloat, 0); // Fill clipdata starting from beginning of clip.
        using (var fileStream = new FileStream(filepath, FileMode.Create))
        {
            WriteHeader(fileStream, clip.frequency, clip.channels, clip.samples);
            ConvertAndWrite(fileStream, dataFloat);
            fileStream.Flush();
            fileStream.Close();
        }

        return true; // TODO: return false if there's a failure saving the file
    }

    public static MemoryStream ConvertToMemstream(AudioClip clip)
    {
        MemoryStream memstream = new MemoryStream();
        float[] dataFloat = new float[clip.samples * clip.channels];
        clip.GetData(dataFloat, 0); // Fill clipdata starting from beginning of clip.
        WriteHeader(memstream, clip.frequency, clip.channels, clip.samples);
        ConvertAndWrite(memstream, dataFloat);
        return memstream;
    }

    public static byte[] ConvertToBytes(AudioClip clip)
    {
        MemoryStream memstream = ConvertToMemstream(clip);
        byte[] bytes = memstream.ToArray();
        memstream.Close();
        return bytes;
    }

    public static string ConvertToString(AudioClip clip)
    {
        byte[] bytes = ConvertToBytes(clip);
        return System.Convert.ToBase64String(bytes);
    }

    public static AudioClip TrimSilence(AudioClip clip, float min)
    {
        var samples = new float[clip.samples * clip.channels];

        clip.GetData(samples, 0);

        return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency, false);
    }

    public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool stream)
    {
        int i;

        for (i = 0; i < samples.Count; i++)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                break;
            }
        }

        samples.RemoveRange(0, i);

        for (i = samples.Count - 1; i > 0; i--)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                break;
            }
        }

        samples.RemoveRange(i, samples.Count - i);

        var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, stream);

        clip.SetData(samples.ToArray(), 0);

        return clip;
    }

    public static float[] Downsample(float[] samples, int sourceFrequency, int targetFrequency, out int leftover)
    {
        int targetLength = (int)((long)samples.Length * (long)targetFrequency / (long)sourceFrequency);
        float[] resampled = new float[targetLength];
        leftover = samples.Length - (int)((long)targetLength * (long)sourceFrequency / (long)targetFrequency);
        int lowerBound = 0;
        for (int i = 0; i < targetLength; ++i)
        {
            int upperBound = (int)((long)(i + 1) * (long)sourceFrequency / (long)targetFrequency);
            double sum = 0;
            for (int j = lowerBound; j < upperBound; ++j)
            {
                sum += samples[j];
            }
            resampled[i] = (float)(sum / (upperBound - lowerBound));
            lowerBound = upperBound;
        }
        return resampled;
    }

    public class WavFileHandle
    {
        public WavFileHandle(string filename, int frequency, int nChannels)
        {
            this.nChannels = nChannels;
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            fileStream = new FileStream(filename, FileMode.Create);
            WriteHeader(fileStream, frequency, nChannels, 0);
        }

        public void AddSamples(float[] samples)
        {
            this.nSamples += samples.Length;
            UpdateHeader(fileStream, nSamples, nChannels);
            ConvertAndWrite(fileStream, samples);
        }

        public void Finish()
        {
            fileStream.Close();
        }

        private FileStream fileStream;
        private int nChannels;
        private int nSamples = 0;
    }

    public static Int16[] ConvertToPCMInt16(float[] samples)
    {
        Int16[] intData = new Int16[samples.Length];

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * RESCALE_FACTOR);
        }

        return intData;
    }

    public static Byte[] ConvertToPCMBytes(float[] samples)
    {
        Int16[] intData = ConvertToPCMInt16(samples);
        Byte[] bytesData = new Byte[samples.Length * 2];
        Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);
        return bytesData;
    }

    private static void ConvertAndWrite(Stream stream, float[] samples)
    {
        Byte[] bytesData = ConvertToPCMBytes(samples);
        stream.Seek(0, SeekOrigin.End);
        stream.Write(bytesData, 0, bytesData.Length);
        stream.Flush();
    }

    public static void PlaySound(AudioSource audioSource, AudioClip audioClip)
    {
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    public static IEnumerator PlayAudioCoroutine(AudioSource audioSource, AudioClip audioClip)
    {
        if (null == audioClip || null == audioSource) yield break;
        audioSource.clip = audioClip;
        audioSource.Play();
        while (audioSource.isPlaying) yield return null;
    }

    private static void WriteHeader(Stream stream, int frequency, int channelCount, int sampleCount)
    {
        int dataSize = sampleCount * channelCount * 2;
        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        stream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(dataSize + HEADER_SIZE - 8);
        stream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        stream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        stream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        stream.Write(subChunk1, 0, 4);

        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        stream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channelCount);
        stream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(frequency);
        stream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(frequency * channelCount * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        stream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channelCount * 2);
        stream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        stream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        stream.Write(datastring, 0, 4);

        // TODO: deleted "* channels"
        Byte[] subChunk2 = BitConverter.GetBytes(dataSize);
        stream.Write(subChunk2, 0, 4);

        stream.Flush();
    }

    private static void UpdateHeader(Stream stream, int channelCount, int sampleCount)
    {
        int dataSize = sampleCount * channelCount * 2;
        stream.Seek(4, SeekOrigin.Begin);
        Byte[] sizeBytes = BitConverter.GetBytes(HEADER_SIZE + dataSize - 8);
        stream.Write(sizeBytes, 0, 4);
        stream.Seek(HEADER_SIZE - 4, SeekOrigin.Begin);
        sizeBytes = BitConverter.GetBytes(dataSize);
        stream.Write(sizeBytes, 0, 4);
    }
}
