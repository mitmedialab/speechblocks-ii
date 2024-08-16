using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

public class Tutorial : MonoBehaviour
{
    public GameObject helpButton;
    public GameObject wordBoxBackground;

    private GameObject pointerAsset;

    private Environment environment = null;

    private SynthesizerController synthesizer;
    private StageOrchestrator stageOrchestrator;
    private TouchManager touchManager;
    private VoiceActivityDetector voiceActivityDetector;

    private List<ITutorialLesson> lessons = new List<ITutorialLesson>();

    private HashSet<string> completedLessons;
    private HashSet<string> invitationGiven = new HashSet<string>();
    private List<IStandaloneTutorialLesson> pendingLessons = new List<IStandaloneTutorialLesson>();
    private List<ITutorialLesson> lessonsAwaitingCompletion = new List<ITutorialLesson>();

    private List<IHelpModule> helpModules = new List<IHelpModule>();

    private CoroutineRunner helpRunner = new CoroutineRunner();
    private IHelpModule activeHelpModule = null;
    private CoroutineRunner delayedHelpRunner = new CoroutineRunner();
    private CoroutineRunner schedulerRunner = new CoroutineRunner();

    private List<GameObject> objectsOfInterestBuffer = new List<GameObject>();

    private int numLessonsInProgress = 0;

    public void Setup(List<string> completedLessons)
    {
        environment = GetComponent<Environment>();
        synthesizer = GetComponent<SynthesizerController>();
        stageOrchestrator = GetComponent<StageOrchestrator>();
        touchManager = GetComponent<TouchManager>();
        voiceActivityDetector = GetComponent<VoiceActivityDetector>();
        this.completedLessons = new HashSet<string>(completedLessons);
        pointerAsset = Resources.Load<GameObject>("Prefabs/TutorialPointer");
        CreateLessonsAndHelpModules(environment);
    }

    public void Reset()
    {
        completedLessons = new HashSet<string>();
        invitationGiven = new HashSet<string>();
        pendingLessons = new List<IStandaloneTutorialLesson>();
        lessonsAwaitingCompletion = new List<ITutorialLesson>();
        schedulerRunner.SetCoroutine(null);
    }

    public void StartTutorial()
    {
        schedulerRunner.SetCoroutine(LessonScheduler());
    }

    public bool IsFirstTime()
    {
        if (environment?.GetUser()?.GetID() == "guest") return false;
        return 0 == completedLessons.Count;
    }

    public List<string> GetCompletedLessons()
    {
        return completedLessons.ToList();
    }

    public bool IsLessonCompleted(string lesson)
    {
        if (environment?.GetUser()?.GetID() == "guest") return true;
        return completedLessons.Contains(lesson);
    }

    void Update()
    {
        UpdateHelpSystem();
        schedulerRunner.Update();
        CheckLessonsCompletion();
    }

    private void UpdateHelpSystem()
    {
        delayedHelpRunner.Update();
        if (null == activeHelpModule) return;
        if (!HelpAppliesToCurrentContext(activeHelpModule, stageOrchestrator.GetStage()))
        {
            helpRunner.SetCoroutine(null);
            activeHelpModule = null;
        }
        helpRunner.Update();
        if (!helpRunner.IsRunning()) { activeHelpModule = null; }
    }

    public IEnumerator GetPlugInLesson(string topic, List<SynQuery> synSequence, object extraArgument = null)
    {
        if (environment.GetUser()?.GetID() == "guest") return null;
        for (int i = 0; i < lessons.Count; ++i)
        {
            ITutorialLesson lesson = lessons[i];
            if (!typeof(IPlugInTutorialLesson).IsInstanceOfType(lesson)) continue;
            IPlugInTutorialLesson plugInLesson = (IPlugInTutorialLesson)lesson;
            if (plugInLesson.Topic != topic) continue;
            if (!plugInLesson.Prerequisites.All(prerequisite => completedLessons.Contains(prerequisite))) continue;
            lessonsAwaitingCompletion.Add(plugInLesson);
            lessons.RemoveAt(i);
            return plugInLesson.GiveLesson(synSequence, extraArgument);
        }
        return null;
    }

    public IEnumerator DemonstrateUIElement(object uiElement, Vector2 pointingAxis, SynQuery prompt, string cause)
    {
        PointAt(uiElement, pointingAxis, cause);
        yield return synthesizer.SpeechCoroutine(prompt, cause: cause, canInterrupt: false, boundToStages: null);
    }

    public IEnumerator DemonstrateUIElementAfterPrompt(object uiElement, Vector2 pointingAxis, SynQuery prompt, string cause)
    {
        FillObjectsOfInterestBuffer(uiElement);
        yield return synthesizer.SpeechCoroutine(prompt, cause: cause, canInterrupt: false, boundToStages: null);
        objectsOfInterestBuffer.Clear();
        PointAt(uiElement, pointingAxis, cause);
    }

    public IEnumerator DemonstrateUIElementInterruptible(object uiElement, SynQuery prompt, string cause, CoroutineResult<bool> result)
    {
        PointAt(uiElement, Vector2.zero, cause);
        Action onInterrupt = () => { CeasePointing(); result.SetErrorCode("interrupted"); };
        yield return synthesizer.SpeechCoroutine(prompt, cause: cause, onInterrupt: onInterrupt);
        if (null == result.GetErrorCode()) { result.Set(true); }
    }

    public IEnumerator DemonstrateUIElementInterruptible(object uiElement, Vector2 pointingAxis, SynQuery prompt, string cause, CoroutineResult<bool> result)
    {
        PointAt(uiElement, pointingAxis, cause);
        Action onInterrupt = () => { CeasePointing(); result.SetErrorCode("interrupted"); };
        yield return synthesizer.SpeechCoroutine(prompt, cause: cause, onInterrupt: onInterrupt);
        if (null == result.GetErrorCode()) { result.Set(true); }
    }

    public IEnumerator InviteToTap(ITappable tappable, Vector2 pointingAxis, SynQuery prompt, SynQuery tapInvitation, bool mandatoryTap, string cause, bool doPoint = true)
    {
        return InviteToTap<ITappable>(tappable, otherTappables: null, pointingAxis: pointingAxis, prompt: prompt, tapInvitation: tapInvitation, mandatoryTap, cause: cause, doPoint: doPoint);
    }

    public IEnumerator InviteToTap<T>(ITappable mainTappable, IEnumerable<T> otherTappables, Vector2 pointingAxis, SynQuery prompt, SynQuery tapInvitation, bool mandatoryTap, string cause, bool doPoint = true) where T : ITappable
    {
        objectsOfInterestBuffer.Clear();
        if (null != mainTappable)
        {
            objectsOfInterestBuffer.Add(mainTappable.gameObject);
        }
        else if (null != otherTappables)
        {
            objectsOfInterestBuffer.AddRange(otherTappables.Select(tappable => tappable.gameObject));
        }
        environment.GetRoboPartner().LookAtTablet();
        touchManager.AddAllowedToTapDelayed(mainTappable);
        if (null != prompt) { yield return synthesizer.SpeechCoroutine(prompt, cause: cause, canInterrupt: false, boundToStages: null); }
        environment.GetRoboPartner().LookAtChild();
        bool tapped = false;
        touchManager.AddActionAwaitingNextTap(() => tapped = true);
        objectsOfInterestBuffer.Clear();
        if (null != mainTappable)
        {
            if (doPoint) { PointAt(mainTappable.gameObject, pointingAxis, cause); }
        }
        else if (null != otherTappables)
        {
            if (doPoint) { PointAt(otherTappables.Select(tappable => tappable.gameObject).ToList(), pointingAxis, cause); }
        }
        else
        {
            touchManager.ResetConstraints();
            yield break;
        }
        if (null != mainTappable)
        {
            touchManager.AddAllowedToTap(mainTappable);
        }
        if (null != otherTappables)
        {
            foreach (ITappable otherTappable in otherTappables)
            {
                if (otherTappable == mainTappable) continue;
                touchManager.AddAllowedToTap(otherTappable);
            }
        }
        int speechID = -1;
        if (!tapped && null != tapInvitation) { speechID = synthesizer.Speak(tapInvitation, cause: cause, canInterrupt: false, boundToStages: mandatoryTap ? null : "current"); }
        if (mandatoryTap) { while (!tapped) yield return null; }
        else { while (!tapped && synthesizer.IsSpeaking(speechID)) yield return null; }
        touchManager.ResetConstraints();
        if (synthesizer.IsSpeaking(speechID)) { synthesizer.InterruptSpeech(speechID); }
        CeasePointing();
    }

    public void PointAt(object pointAt, Vector2 axis, string cause)
    {
        CeasePointing();
        if (typeof(MonoBehaviour).IsInstanceOfType(pointAt))
        {
            pointAt = ((MonoBehaviour)pointAt).gameObject;
        }
        else if (typeof(IEnumerable<MonoBehaviour>).IsInstanceOfType(pointAt))
        {
            pointAt = ((IEnumerable<MonoBehaviour>)pointAt).Select(behr => behr.gameObject).ToArray();
        }
        if (typeof(GameObject).IsInstanceOfType(pointAt))
        {
            GameObject target = (GameObject)pointAt;
            Instantiate<GameObject>(pointerAsset).GetComponent<TutorialPointer>().PointAt(target, axis, cause);
        }
        else if (typeof(IEnumerable<GameObject>).IsInstanceOfType(pointAt))
        {
            IEnumerable<GameObject> targets = (IEnumerable<GameObject>)pointAt;
            foreach (GameObject target in targets)
            {
                Instantiate<GameObject>(pointerAsset).GetComponent<TutorialPointer>().PointAt(target, axis, cause);
            }
        }
    }

    public void PointAway(GameObject pointAwayFrom, Vector2 axis, float offset, string cause)
    {
        Instantiate<GameObject>(pointerAsset).GetComponent<TutorialPointer>().PointAway(pointAwayFrom, axis, offset, cause);
    }

    public void GetObjectsOfAttention(List<GameObject> buffer, string stage)
    {
        buffer.AddRange(objectsOfInterestBuffer);
        GameObject[] pointers = GameObject.FindGameObjectsWithTag("TutorialPointer");
        buffer.AddRange(pointers.Select(pointer => pointer.GetComponent<TutorialPointer>().GetTarget()).Where(target => null != target));
    }

    public void TriggerHelp()
    {
        if (helpRunner.IsRunning()) return;
        string currentStage = stageOrchestrator.GetStage();
        foreach (IHelpModule helpModule in helpModules)
        {
            if (HelpAppliesToCurrentContext(helpModule, currentStage))
            {
                ActivateHelpModule(helpModule);
                delayedHelpRunner.SetCoroutine(null);
            }
        }
    }

    public void TriggerDelayedHelp(float delay)
    {
        if (helpRunner.IsRunning()) return;
        delayedHelpRunner.SetCoroutine(DeliverDelayedHelp(delay));
    }

    public bool IsGivingStandaloneLesson()
    {
        return 0 != numLessonsInProgress;
    }

    public void CeasePointing()
    {
        TutorialPointer.DestroyAllPointers();
    }

    private void FillObjectsOfInterestBuffer(object objOfInterest)
    {
        objectsOfInterestBuffer.Clear();
        if (typeof(MonoBehaviour).IsInstanceOfType(objOfInterest))
        {
            objectsOfInterestBuffer.Append(((MonoBehaviour)objOfInterest).gameObject);
        }
        else if (typeof(IEnumerable<MonoBehaviour>).IsInstanceOfType(objOfInterest))
        {
            objectsOfInterestBuffer.AddRange(((IEnumerable<MonoBehaviour>)objOfInterest).Select(behr => behr.gameObject).ToArray());
        }
        else if (typeof(GameObject).IsInstanceOfType(objOfInterest))
        {
            objectsOfInterestBuffer.Append((GameObject)objOfInterest);
        }
        else if (typeof(IEnumerable<GameObject>).IsInstanceOfType(objOfInterest))
        {
            objectsOfInterestBuffer.AddRange((IEnumerable<GameObject>)objOfInterest);
        }
    }

    private void CreateLessonsAndHelpModules(Environment environment)
    {
        bool childDriven = environment.GetUser().InChildDrivenCondition();
        if (childDriven)
        {
            lessons.Add(new TutorialLessonCanvas());
            lessons.Add(new TutorialLessonCategories());
            lessons.Add(new TutorialLessonWordBankButton());
        }
        else
        {
            lessons.Add(new TutorialLessonMachineDrivenChoice());
        }
        lessons.Add(new TutorialLessonSpelling());
        lessons.Add(new TutorialLessonBlockDrag());
        lessons.Add(new TutorialLessonSpellingComplete());
        TutorialLessonAvatarPicker lessonAvatarPicker = new TutorialLessonAvatarPicker(); // it acts both as a lesson and as a help module
        lessons.Add(lessonAvatarPicker);
        lessons.Add(new TutorialLessonPictureBlockDrag());
        lessons.Add(new TutorialLessonPictureBlockPinch());
        lessons.Add(new TutorialLessonFlip());
        TutorialLessonSpeechRecoResults lessonSpeechRecoResults = null;
        if (childDriven)
        {
            lessons.Add(new TutorialLessonIdeaButton());
            lessons.Add(new TutorialLessonAssociations());
            lessons.Add(new TutorialLessonAssociationsSpell());
            lessons.Add(new TutorialLessonAssociationsIdeaButton());
            lessons.Add(new TutorialLessonSpeechReco());
            lessonSpeechRecoResults = new TutorialLessonSpeechRecoResults(); // it acts both as a lesson and as a help module
            lessons.Add(lessonSpeechRecoResults);
            lessons.Add(new TutorialLessonPictureBlockDrag2nd());
        }
        lessons.Add(new TutorialLessonCanvasFinalTouches());
        lessons.Add(new TutorialLessonGallery());
        if (childDriven)
        {
            lessons.Add(new TutorialLessonAssociationsInvoke());
        }
        lessons = lessons.Where(IsRelevantLesson).ToList();
        helpModules.Add(new HelpModuleGallery());
        helpModules.Add(new HelpModuleCanvas());
        if (null != lessonSpeechRecoResults) helpModules.Add(lessonSpeechRecoResults);
        helpModules.Add(new HelpModuleWordBank());
        helpModules.Add(new HelpModuleKeyboard());
        helpModules.Add(lessonAvatarPicker);
        StartCoroutine(InitHelpModules());
    }

    private bool IsRelevantLesson(ITutorialLesson lesson)
    {
        return !completedLessons.Contains(lesson.Name);
    }

    private void RecordLessonCompletion(string lessonName)
    {
        completedLessons.Add(lessonName);
        GetComponent<Environment>().MarkLessonCompletion(lessonName);
        UpdatePendingLessons();
    }

    private void UpdatePendingLessons()
    {
        for (int i = 0; i < lessons.Count; ++i)
        {
            ITutorialLesson lesson = lessons[i];
            if (typeof(IStandaloneTutorialLesson).IsInstanceOfType(lesson))
            {
                IStandaloneTutorialLesson standaloneLesson = (IStandaloneTutorialLesson)lesson;
                if (standaloneLesson.Prerequisites.All(prerequisite => completedLessons.Contains(prerequisite)))
                {
                    pendingLessons.Add(standaloneLesson);
                    lessons.RemoveAt(i); --i;
                }
            }
        }
    }

    private IEnumerator InitHelpModules()
    {
        yield return null; // skip 1 step to allow everything to initialize, so that the modules can properly initialize afterwards
        foreach (IHelpModule module in helpModules) {
            try
            {
                module.Init(gameObject);
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
        }
    }

    private IEnumerator LessonScheduler()
    {
        yield return null; // skip 1 step to allow everything to initialize, so that the lessons can properly initialize afterwards
        foreach (ITutorialLesson lesson in lessons) {
            try
            {
                lesson.Init(gameObject);
            }
            catch (Exception e)
            {
                ExceptionUtil.OnException(e);
            }
        }
        lessons = lessons.Where(lesson => !lesson.PrerequisitesExpectedOnStartup || lesson.Prerequisites.All(prereq => completedLessons.Contains(prereq))).ToList(); // we init lessons first and then filter the prereq-on-startup lessons, because their init procedure might contain important actions, like disabling buttons
        UpdatePendingLessons();
        while (true)
        {
            string stage = stageOrchestrator.GetStage();
            IStandaloneTutorialLesson lessonThatCanStart = pendingLessons.Where(lesson => LessonCanStart(lesson, stage)).FirstOrDefault();
            bool yielded = false;
            if (null != lessonThatCanStart)
            {
                pendingLessons.Remove(lessonThatCanStart);
                lessonsAwaitingCompletion.Add(lessonThatCanStart);
                ++numLessonsInProgress;
                Logging.LogLessonStart(lessonThatCanStart.Name);
                yield return lessonThatCanStart.GiveLesson();
                Logging.LogLessonEnd(lessonThatCanStart.Name);
                --numLessonsInProgress;
                yielded = true;
            }
            else if (0 == lessonsAwaitingCompletion.Count)
            {
                IStandaloneTutorialLesson lessonThatCanInvite = pendingLessons.Where(lesson => !invitationGiven.Contains(lesson.Name)
                                                                                               && InvitationCanStart(lesson, stage)).FirstOrDefault();
                if (null != lessonThatCanInvite)
                {
                    invitationGiven.Add(lessonThatCanInvite.Name);
                    Logging.LogLessonInviteStart(lessonThatCanInvite.Name);
                    yield return lessonThatCanInvite.InviteToLesson();
                    Logging.LogLessonInviteEnd(lessonThatCanInvite.Name);
                    yielded = true;
                }
            }
            if (!yielded) { yield return null; }
        }
    }

    private bool LessonCanStart(IStandaloneTutorialLesson lesson, string stage)
    {
        try
        {
            return lesson.CanStart(stage);
        }
        catch (Exception e)
        {
            ExceptionUtil.OnException(e);
            return false;
        }
    }

    private bool InvitationCanStart(IStandaloneTutorialLesson lesson, string stage)
    {
        try
        {
            return lesson.InvitationCanStart(stage);
        }
        catch (Exception e)
        {
            ExceptionUtil.OnException(e);
            return false;
        }
    }

    private void CheckLessonsCompletion()
    {
        for (int i = lessonsAwaitingCompletion.Count - 1; i >= 0; --i)
        {
            ITutorialLesson lesson = lessonsAwaitingCompletion[i];
            if (lesson.CheckCompletion())
            {
                lessonsAwaitingCompletion.RemoveAt(i);
                RecordLessonCompletion(lesson.Name);
            }
        }
    }

    private void ActivateHelpModule(IHelpModule helpModule)
    {
        helpRunner.SetCoroutine(helpModule.GiveHelp());
        activeHelpModule = helpModule;
    }

    private IEnumerator DeliverDelayedHelp(float delay)
    {
        string currentStage = stageOrchestrator.GetStage();
        IHelpModule currentHelpModule = helpModules.Find(helpModule => HelpAppliesToCurrentContext(helpModule, currentStage));
        if (null == currentHelpModule) yield break;
        double t0 = TimeKeeper.time;
        while ((float)(TimeKeeper.time - t0) < delay)
        {
            yield return null;
            if (touchManager.GetTouchCount() > 0) { t0 = TimeKeeper.time; }
            if (voiceActivityDetector.IsPickingVoice()) { t0 = TimeKeeper.time; }
            if (!HelpAppliesToCurrentContext(currentHelpModule, stageOrchestrator.GetStage())) { yield break; }
        }
        ActivateHelpModule(currentHelpModule);
    }

    private bool HelpAppliesToCurrentContext(IHelpModule helpModule, string stage)
    {
        try
        {
            return helpModule.HelpAppliesToCurrentContext(stage);
        }
        catch (Exception e)
        {
            ExceptionUtil.OnException(e);
            return false;
        }
    }
}
