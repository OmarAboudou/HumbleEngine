using System.Runtime.CompilerServices;

namespace HumbleEngine.Core;

public class ApplicationPassNotFoundException
(
    Application application,
    IApplicationPass applicationPass
)
:
InvalidOperationException
(
    $"'{applicationPass}' isn't registered for '{application}'."
)
{
    public Application Application {get;} = application;
    public IApplicationPass ApplicationPass {get;} = applicationPass;
}