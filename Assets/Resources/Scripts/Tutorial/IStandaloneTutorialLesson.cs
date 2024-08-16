using System.Collections;
using UnityEngine;

public interface IStandaloneTutorialLesson : ITutorialLesson
{
    bool InvitationCanStart(string stage);
    IEnumerator InviteToLesson();
    bool CanStart(string stage);
    IEnumerator GiveLesson();
}
