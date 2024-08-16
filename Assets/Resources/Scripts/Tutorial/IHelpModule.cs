using System.Collections;
using UnityEngine;

public interface IHelpModule
{
    void Init(GameObject stageObject);
    bool HelpAppliesToCurrentContext(string currentStage);
    IEnumerator GiveHelp();
}
