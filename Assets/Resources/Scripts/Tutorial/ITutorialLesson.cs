using UnityEngine;

public interface ITutorialLesson
{
    string Name { get; }
    string[] Prerequisites { get; }
    bool PrerequisitesExpectedOnStartup { get; }
    void Init(GameObject stageObject);
    bool CheckCompletion();
}
