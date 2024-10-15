using sqlM.State;

namespace sqlM.Actions;
public class None : IAction
{
    public void Go(StartupParams startupParams, Container state) {}
}
