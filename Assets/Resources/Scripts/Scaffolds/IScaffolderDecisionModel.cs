public interface IScaffolderDecisionModel
{
    // for position type, see constants in scaffolder
    bool RobotShallTakeTurn(PGPair pgPair, int positionType);
    int DecideScaffoldingLevel(PGPair pgPair, int positionType);
}