using HumbleEngine.Core;
using HumbleEngine.Silk;

new SilkApplication()
    .Run(
        new ApplicationConfig(
            new Node
            {
                
            },
            [new FixedUpdatePass()],
            [new UpdatePass()]
        )
    );