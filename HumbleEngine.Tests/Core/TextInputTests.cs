using NUnit.Framework;
using Silk.NET.Input;

namespace HumbleEngine.Tests;

[TestFixture]
public class TextInputTests
{
    private TextInput _input = null!;

    [SetUp]
    public void SetUp()
    {
        _input = new TextInput();
        _input.Init();
    }

    [Test]
    public void IsFocusable_IsTrue()
        => Assert.That(_input.IsFocusable, Is.True);

    [Test]
    public void OnKeyChar_AppendsCharacter()
    {
        _input.OnKeyChar('H');
        _input.OnKeyChar('i');

        Assert.That(_input.Text.Value, Is.EqualTo("Hi"));
    }

    [Test]
    public void OnKeyDown_Backspace_RemovesLastCharacter()
    {
        _input.Text.Value = "Hello";
        _input.OnKeyDown(Key.Backspace);

        Assert.That(_input.Text.Value, Is.EqualTo("Hell"));
    }

    [Test]
    public void OnKeyDown_Backspace_OnEmptyText_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _input.OnKeyDown(Key.Backspace));
        Assert.That(_input.Text.Value, Is.EqualTo(""));
    }

    [Test]
    public void OnFocusGained_SetsFocusedState()
    {
        _input.OnFocusGained();

        Assert.That(_input.Dirty, Is.Not.EqualTo(DirtyLevel.None));
    }

    [Test]
    public void OnFocusLost_ClearsFocusedState()
    {
        _input.OnFocusGained();
        _input.ClearDirty();

        _input.OnFocusLost();

        Assert.That(_input.Dirty, Is.Not.EqualTo(DirtyLevel.None));
    }
}
