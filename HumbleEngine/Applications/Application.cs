using System.Reflection;

namespace HumbleEngine;

public abstract class Application
{
    public Application()
    {
        PlatformWindowFactory = CreatePlatformWindow;
    }

    protected abstract PlatformWindow CreatePlatformWindow();
    protected virtual IRenderer? CreateRenderer() => null;

    internal static Func<PlatformWindow> PlatformWindowFactory;

    public void Run(ApplicationConfig config)
    {
        using Window window = new();
        if (config.Scene is not null)
            window.Add(config.Scene);

        PlatformWindow platformWindow = window.PlatformWindow;
        IRenderer? renderer = CreateRenderer();

        List<IUpdatePass>      updatePasses      = BuildUpdatePasses(config);
        List<IFixedUpdatePass> fixedUpdatePasses = BuildFixedUpdatePasses(config);

        platformWindow.Loaded.Connect(() =>
        {
            if (renderer is not null)
            {
                if (!renderer.Supports(config.PreferredBackend))
                    throw new InvalidOperationException($"Renderer does not support {config.PreferredBackend}.");
                renderer.Initialize(config.PreferredBackend);
            }

            platformWindow.FixUpdated.Connect(delta =>
            {
                foreach (IFixedUpdatePass pass in fixedUpdatePasses)
                    pass.Execute(window, delta);
            });

            platformWindow.Rendering.Connect(delta =>
            {
                foreach (IUpdatePass pass in updatePasses)
                    pass.Execute(window, delta);
                // TODO: paint pass → renderer.Render(buffer)
            });
        });

        if (renderer is not null)
            platformWindow.Resized.Connect(renderer.Resize);

        platformWindow.Closing.Connect(() =>
        {
            renderer?.Dispose();
            Console.WriteLine("CLOSING !");
        });

        platformWindow.Run();
    }

    private static List<IUpdatePass> BuildUpdatePasses(ApplicationConfig config)
    {
        var passes = new List<IUpdatePass>();

        if (config.EnableUpdate)
            passes.Add(new UpdatePass());

        // TODO: EnableReconciler, EnableLayout, EnablePaint — passes non encore implémentées

        // Passes custom découvertes par réflexion (remplacé par source generator à terme)
        foreach (IUpdatePass custom in DiscoverCustomPasses<IUpdatePass, UpdatePassAttribute>())
            passes.Add(custom);

        return passes;
    }

    private static List<IFixedUpdatePass> BuildFixedUpdatePasses(ApplicationConfig config)
    {
        var passes = new List<IFixedUpdatePass>();

        if (config.EnableFixedUpdate)
            passes.Add(new FixedUpdatePass());

        foreach (IFixedUpdatePass custom in DiscoverCustomPasses<IFixedUpdatePass, FixedUpdatePassAttribute>())
            passes.Add(custom);

        return passes;
    }

    private static IEnumerable<TPass> DiscoverCustomPasses<TPass, TAttr>()
        where TAttr : Attribute
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract
                     && typeof(TPass).IsAssignableFrom(t)
                     && t.GetCustomAttribute<TAttr>() is not null)
            .OrderBy(t => (t.GetCustomAttribute<TAttr>() as dynamic)?.Order ?? 0)
            .Select(t => (TPass)Activator.CreateInstance(t)!);
    }
}
