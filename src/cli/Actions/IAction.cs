namespace sqlM.Actions;
public interface IAction
{
    public void Go(StartupParams startupParams, State.Container state);
}
