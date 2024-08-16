using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationMaster : MonoBehaviour
{
    private Vocab vocab;
    private Environment environment;
    private SynthesizerController synthesizer;
    private TouchManager touchManager;
    private bool firstTime = false;

    public static string[] ENCOURAGEMENTS = { "Nice!", "Great!", "Very nice!", "Excellent!", "Love it!", "Wonderful!", "Magnificent!" };
    public static string[] ENCOURAGEMENTS_MUTED = { "Good choice!", "Good idea!", "Nice choice!" };

    private void Start()
    {
        vocab = GetComponent<Vocab>();
        environment = GetComponent<Environment>();
        synthesizer = GetComponent<SynthesizerController>();
        touchManager = GetComponent<TouchManager>();
    }

    public void StartIntro()
    {
        firstTime = GetComponent<Tutorial>().IsFirstTime();
        StartCoroutine(IntroCoroutine());
    }

    public void StartGoodbye()
    {
        StartCoroutine(GoodbyeCoroutine());
    }

    private IEnumerator IntroCoroutine()
    {
        while (!environment.GetRoboPartner().HasEntered()) yield return null;
        environment.GetRoboPartner().ShowExpression(RoboExpression.GREETING);
        if (firstTime)
        {
            SynQuery userNameCode = GetUserNameCode();
            SynQuery jiboCode = vocab.GetPronunciation("jibo");
            SynQuery helloPrompt = SynQuery.Format("Hello {0}! My name is {1}. I'm a robot who likes to play!", userNameCode, jiboCode);
            yield return synthesizer.SpeechCoroutine(helloPrompt, cause: "convmaster", canInterrupt: false, keepPauses: false);
            environment.GetRoboPartner().ShowExpression(RoboExpression.EXCITED);
            if ("jibo" == environment.GetStationType())
            {
                yield return new WaitForSeconds(5.5f);
            }
            else
            {
                VirtualJiboPartner virtualJibo = (VirtualJiboPartner)environment.GetRoboPartner();
                while (virtualJibo.IsShowingExpression()) yield return null;
            }
            yield return GetVideoApproval();
            string coolThingToPlayPrompt = "I want to show you a cool thing to play! It is about making fun pictures by spelling words. Ready?";
            yield return synthesizer.SpeechCoroutine(coolThingToPlayPrompt, cause: "convmaster", canInterrupt: false);
        }
        else
        {
            yield return synthesizer.SpeechCoroutine(GetWelcomeText(), cause: "convmaster", canInterrupt: false);
            yield return GetVideoApproval();
            synthesizer.Speak(GetTapIfYouForgotText(), cause: "convmaster", boundToStages: null);
        }
        GameObject.FindWithTag("TitlePage").GetComponent<TitlePage>().Retract(retractInstantly: false);
        touchManager.Unconstrain();
        if (environment.GetUser().GetID() != "guest") { GetComponent<Tutorial>().StartTutorial(); }
    }

    private IEnumerator GoodbyeCoroutine()
    {
        touchManager.Constrain();
        environment.GetRoboPartner().LookAtChild();
        //roboPartner.ShowExpression(RoboExpression.GREETING);
        environment.GetRoboPartner().AtEase();
        environment.WrapDataCollection();
        yield return synthesizer.SpeechCoroutine(GetGoodbyeText(), cause: "convmaster", canInterrupt: false, keepPauses: false);
        environment.Logout();
        touchManager.Unconstrain();
        //Application.Quit();
    }

    private SynQuery GetWelcomeText()
    {
        SynQuery userNameCode = GetUserNameCode();
        string[] greetings = {  "Welcome back, {0}!",
                                "Nice to see you, {0}!",
                                "Great to see you, {0}!",
                                "Hello {0}! Nice to see you!",
                                "Hi {0}! Great to see you!",
                                "Hi {0}!", "Hello {0}!"};
        return SynQuery.Format(RandomUtil.PickOne("conv-welc1", greetings), userNameCode);
    }

    //private string GetEncouragementText()
    //{
    //    string[] lets_make = {  "Let's make some fun pictures today!",
    //                            "Let's spell some cool words today!"};
    //    return RandomUtil.PickOne("conv-enc1", lets_make);
    //}

    private SynQuery GetTapIfYouForgotText()
    {
        string[] patterns =  {  "Remember, you can always tap on {0} whenever you want a reminder how to play!",
                                "If at any point you want me to remind you how to play, you can always tap on {0}!",
                                "Remember to tap on {0} if at any point you forgot how to play!",
                                "Remember to tap on {0} if at any point you want a reminder how to play!"};
        return SynQuery.Format(RandomUtil.PickOne("conv-tap-forgot", patterns), "tablet" == environment.GetStationType() ? "me" : SynQuery.Format("the button with {0}.", vocab.GetPronunciation("jibo")));
    }

    private IEnumerator GetVideoApproval()
    {
        if (environment.IsVideoApprovalNeeded())
        {
            bool touchManagerWasOff = !touchManager.IsUnconstrained();
            if (touchManagerWasOff) { touchManager.Unconstrain(); }
            GameObject.FindWithTag("TitlePage").GetComponent<TitlePage>().ActivateVideoButtons();
            string videoPrompt = "Are you and your parents OK if I record an audio of how we play for researchers? Press the green button if it is OK, and the red one if it is not.";
            int speechID = synthesizer.Speak(videoPrompt, cause: "convmaster");
            while (!environment.IsRecordingDecisionMade()) yield return null;
            if (synthesizer.IsSpeaking(speechID)) { synthesizer.InterruptSpeech(speechID); }
            if (touchManagerWasOff) { touchManager.Constrain(); }
        }
        else
        {
            environment.ProceedWithRecording(webcamEnabled: false);
        }
    }

    private SynQuery GetGoodbyeText()
    {
        string[] byeOptions = { "Bye-bye",
                                "See you"};
        string[] itWasSoFunOptions = { "It was so fun",
                                               "I had so much fun",
                                               "I really liked",
                                               "I so much liked",
                                               "I really enjoyed",
                                               "I so much enjoyed"};
        string[] actionOptions = {  "playing",
                                    "making pictures",
                                    "spelling words"};
        string[] endingOptions = {"Hope to see you again soon!",
                                 "Hope to play with you again soon!",
                                 "Hope you will be back soon!"};
        string ending = (firstTime || RandomUtil.Range("conv-bye1", 0, 1) < 0.33) ? RandomUtil.PickOne("conv-bye2", endingOptions) : "";
        SynQuery kidNameCode = GetUserNameCode();
        SynQuery prompt = SynQuery.Format($"{RandomUtil.PickOne("conv-bye3", byeOptions)}, {{0}}! {RandomUtil.PickOne("conv-bye4", itWasSoFunOptions)} {RandomUtil.PickOne("conv-bye5", actionOptions)} with you! {ending}", kidNameCode);
        return prompt;
    }

    private SynQuery GetUserNameCode()
    {
        return environment.GetUser().GetID() != "guest" ? vocab.GetPronunciation(environment.GetUser().GetShortName()) : "friend";
    }
}
