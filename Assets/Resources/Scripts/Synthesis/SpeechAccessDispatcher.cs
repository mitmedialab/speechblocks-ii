using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public class SpeechAccessDispatcher : MonoBehaviour {
    List<InterruptRecord> interruptRecords = new List<InterruptRecord>();
    private bool locked = false;

    public bool AccessSpeech(Action interruptCallback, bool canAutoInterrupt, bool initialAccess) {
        if (!CanAutoInterrupt())
        {
            if (canAutoInterrupt) {
                try
                {
                    interruptCallback();
                }
                catch (Exception e)
                {
                    ExceptionUtil.OnException(e);
                }
            }
            return false;
        }
        if (initialAccess)
        {
            Interrupt();
        }
        interruptRecords.Add(new InterruptRecord(interruptCallback, canAutoInterrupt));
        return true;
    }

    public void AccessToSpeechFinished(Action interruptCallback) {
        for (int i = interruptRecords.Count - 1; i > -1; --i)
        {
            if (interruptRecords[i].interruptCallback == interruptCallback)
            {
                interruptRecords.RemoveAt(i);
            }
        }
    }

    public bool IsSpeechAccessed() {
        return locked || 0 != interruptRecords.Count;
    }

    private void Interrupt()
    {
        List<InterruptRecord> terminationList = new List<InterruptRecord>();
        terminationList.AddRange(interruptRecords);
        foreach (InterruptRecord interruptRecord in terminationList)
        {
            try
            {
                interruptRecord.interruptCallback();
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
        }
        interruptRecords.Clear();
    }

    private bool CanAutoInterrupt()
    {
        return interruptRecords.All(record => record.canAutoInterrupt);
    }

    private struct InterruptRecord
    {
        public Action interruptCallback;
        public bool canAutoInterrupt;

        public InterruptRecord(Action interruptCallback, bool canAutoInterrupt)
        {
            this.interruptCallback = interruptCallback;
            this.canAutoInterrupt = canAutoInterrupt;
        }
    }
}
