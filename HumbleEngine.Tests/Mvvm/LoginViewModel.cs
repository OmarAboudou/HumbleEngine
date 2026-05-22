namespace HumbleEngine.Tests;

public class LoginViewModel
{
    public ReactiveProperty<string> Username = new("");
    public ReactiveProperty<string> Greeting = new("Entrez votre nom.");

    public LoginViewModel()
    {
        Username.Connect(name =>
            Greeting.Value = string.IsNullOrEmpty(name)
                ? "Entrez votre nom."
                : $"Bonjour, {name} !");
    }
}
