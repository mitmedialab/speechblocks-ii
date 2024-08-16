
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialLessonCategories : IStandaloneTutorialLesson
{
    public string Name { get; } = "word-bank-categories";

    public string[] Prerequisites { get; } = { "canvas" };

    public bool PrerequisitesExpectedOnStartup { get; } = false;

    public void Init(GameObject stageObject)
    {
        tutorial = stageObject.GetComponent<Tutorial>();
        environment = stageObject.GetComponent<Environment>();
        GameObject drawerObject = GameObject.FindWithTag("WordDrawer");
        touchManager = stageObject.GetComponent<TouchManager>();
        categories = GameObject.FindGameObjectsWithTag("CategoryButton").Select(obj => obj.GetComponent<FixedCategoryButton>()).Where(category => null != category).ToArray();
    }

    public bool InvitationCanStart(string stage) { return false; }

    public IEnumerator InviteToLesson() { return null; }

    public bool CanStart(string stage)
    {
        return "word_bank" == stage;
    }

    public IEnumerator GiveLesson()
    {
        List<FixedCategoryButton> categories = GameObject.FindGameObjectsWithTag("CategoryButton").Select(obj => obj.GetComponent<FixedCategoryButton>()).ToList();
        touchManager.Constrain();
        environment.GetRoboPartner().LookAtTablet();
        string categoriesPrompt = "All these buttons stand for different kinds of things that we can make!";
        yield return tutorial.DemonstrateUIElement(uiElement: categories,
            pointingAxis: Vector2.up,
            prompt: categoriesPrompt,
            cause: "tutorial:word-bank-categories:intro");
        FixedCategoryButton peopleCategory = categories.Where(category => category.GetName() == "people").First();
        string peopleCategoryPrompt = "Can you see the one for people?";
        yield return tutorial.InviteToTap(mainTappable: peopleCategory,
            otherTappables: categories,
            pointingAxis: Vector2.zero,
            prompt: peopleCategoryPrompt,
            tapInvitation: "Let's tap on it and see what words are there!",
            mandatoryTap: true,
            cause: "tutorial:word-bank-categories:lets-tap");
        environment.GetRoboPartner().LookAtChild();
        touchManager.Unconstrain();
        completed = true;
    }

    public bool CheckCompletion()
    {
        return completed;
    }

    private Environment environment;
    private Tutorial tutorial;
    private TouchManager touchManager;
    private FixedCategoryButton[] categories;
    private bool completed = false;
}