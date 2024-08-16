public interface IScaffoldLevel
{
    SynQuery Prompt(PGMapping target, int[] syllableBreakdown, int targetPGSlot, bool giveQuestion, int scaffolderTargetID, int scaffoldingInteractionID);
}