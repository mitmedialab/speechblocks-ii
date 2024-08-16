using System.Collections;
using System.Collections.Generic;

public interface IPlugInTutorialLesson : ITutorialLesson
{
    string Topic { get; }
    IEnumerator GiveLesson(List<SynQuery> synSequence, object extraArgument);
}
