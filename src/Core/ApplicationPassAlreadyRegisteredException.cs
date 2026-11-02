using System.Runtime.CompilerServices;

namespace HumbleEngine.Core;

public class ApplicationPassAlreadyRegisteredException
(
    Application application,
    IApplicationPass applicationPass
)
:
InvalidOperationException
(
    $"'{applicationPass}' is already registered for '{application}'."
)
{
    public Application Application {get;} = application;
    public IApplicationPass ApplicationPass {get;} = applicationPass;
}