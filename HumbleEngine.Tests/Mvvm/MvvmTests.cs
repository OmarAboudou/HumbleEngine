using NUnit.Framework;
using Silk.NET.Input;

namespace HumbleEngine.Tests;

[TestFixture]
public class MvvmTests
{
    private LoginScreen _screen = null!;

    [SetUp]
    public void SetUp()
    {
        _screen = new LoginScreen();
        _screen.Init();
    }

    [Test]
    public void Typing_UpdatesViewModel()
    {
        _screen.Input.OnKeyChar('O');
        _screen.Input.OnKeyChar('m');
        _screen.Input.OnKeyChar('a');
        _screen.Input.OnKeyChar('r');

        Assert.That(_screen.Vm.Username.Value, Is.EqualTo("Omar"));
    }

    [Test]
    public void ViewModelChange_UpdatesInput()
    {
        _screen.Vm.Username.Value = "Omar";

        Assert.That(_screen.Input.Text.Value, Is.EqualTo("Omar"));
    }

    [Test]
    public void Typing_UpdatesGreetingLabel()
    {
        _screen.Input.OnKeyChar('O');
        _screen.Input.OnKeyChar('m');
        _screen.Input.OnKeyChar('a');
        _screen.Input.OnKeyChar('r');

        Assert.That(_screen.GreetingLabel.Text.Value, Is.EqualTo("Bonjour, Omar !"));
    }

    [Test]
    public void ClearingInput_ResetsGreeting()
    {
        _screen.Vm.Username.Value = "Omar";
        _screen.Input.OnKeyDown(Key.Backspace);
        _screen.Input.OnKeyDown(Key.Backspace);
        _screen.Input.OnKeyDown(Key.Backspace);
        _screen.Input.OnKeyDown(Key.Backspace);

        Assert.That(_screen.GreetingLabel.Text.Value, Is.EqualTo("Entrez votre nom."));
    }
}
