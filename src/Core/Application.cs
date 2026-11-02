namespace HumbleEngine.Core;

public class Application
{
    public required string Name {get;init;}
    public required Node RootNode {get;init;}


    public IReadOnlyList<IApplicationPass> ApplicationPasses {get;}
    private readonly List<IApplicationPass> applicationPasses = [];


    public Application()
    {
        ApplicationPasses = applicationPasses.AsReadOnly();
    }

    public void RegisterPass(IApplicationPass applicationPass)
    {
        if(applicationPasses.Contains(applicationPass))
            throw new ApplicationPassAlreadyRegisteredException(this, applicationPass);

        applicationPasses.Add(applicationPass);
        applicationPasses.Sort();
    }

    public void UnregisterPass(IApplicationPass applicationPass)
    {
        if(!applicationPasses.Contains(applicationPass))
            throw new ApplicationPassNotFoundException(this, applicationPass);

        applicationPasses.Remove(applicationPass);
        applicationPasses.Sort();
    }

    public void LoopOnce()
    {
        foreach(IApplicationPass applicationPass in applicationPasses)
        {
            applicationPass.Run(this);
        }
    }

}