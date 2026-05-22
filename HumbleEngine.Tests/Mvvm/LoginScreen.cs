namespace HumbleEngine.Tests;

public class LoginScreen : Node
{
    public  readonly LoginViewModel Vm             = new();
    private readonly TextInput      _input         = new();
    private readonly Label          _greetingLabel = new();

    public TextInput Input         => _input;
    public Label     GreetingLabel => _greetingLabel;

    public override void Init()
    {
        base.Init();

        var titleLabel = new Label();
        titleLabel.Text.Value = "Votre nom :";

        AddChild(new Column
        {
            titleLabel,
            _input,
            _greetingLabel
        });

        // Two-way : _input.Text ↔ Vm.Username
        _input.Text.BindFrom(Vm.Username);
        Vm.Username.BindFrom(_input.Text);

        // One-way : Vm.Greeting → _greetingLabel.Text
        _greetingLabel.Text.BindFrom(Vm.Greeting);
    }
}
