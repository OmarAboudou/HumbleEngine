# WPF / XAML — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 2 (Intermédiaire) ✅

---

## Ce qu'on a vu au Tour 1 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **XAML** | Langage de balisage XML qui décrit la structure de l'UI (≈ HTML). |
| **Contrôle** | Élément interactif : `Button`, `TextBox`, `Slider`… |
| **Panel** | Conteneur de mise en page qui organise ses enfants. |
| **Style** | Règles d'apparence applicables à un ou plusieurs contrôles (≈ CSS). |
| **Data Binding** | Lien automatique entre un élément UI et une donnée en C#. |

---

## 1. MVVM — Model / View / ViewModel

Le pattern architectural central de WPF. Il sépare le code en trois couches.

```
Model        ← les données brutes (ex : un objet Joueur avec Nom, Score…)
ViewModel    ← prépare et expose les données pour l'UI, contient la logique
View         ← le XAML, affiche ce que le ViewModel expose, sans logique
```

```
View (XAML)  ←──── binding ────→  ViewModel (C#)  ←──── accès ────→  Model (C#)
```

La View ne connaît pas le Model. Elle ne parle qu'au ViewModel. Le ViewModel ne connaît pas la View. Les deux communiquent uniquement via le binding.

```csharp
// ViewModel — expose les données à la View
public class JoueurViewModel
{
    public string Nom { get; set; } = "Hero";
    public int Score { get; set; } = 0;
}
```

```xml
<!-- View — affiche ce que le ViewModel expose -->
<TextBlock Text="{Binding Nom}" />
<TextBlock Text="{Binding Score}" />
```

---

## 2. INotifyPropertyChanged

Le binding lit la valeur au démarrage, mais ne la relit pas automatiquement. Pour que la View se mette à jour quand une donnée change dans le ViewModel, il faut **notifier** le binding.

```csharp
public class JoueurViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private int _score;
    public int Score
    {
        get => _score;
        set
        {
            _score = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Score)));
            // ↑ "Hey binding, Score a changé, mets l'UI à jour"
        }
    }
}
```

```xml
<!-- Se met à jour automatiquement quand Score change -->
<TextBlock Text="{Binding Score}" />
```

### Deux mécanismes distincts

| Mécanisme | Direction | Rôle |
|-----------|-----------|------|
| `INotifyPropertyChanged` | ViewModel → View | Notifie la View quand une donnée change |
| `Mode=TwoWay` (binding) | View → ViewModel | Répercute les saisies UI vers le ViewModel |

---

## 3. Panels avancés

| Panel | Comportement |
|-------|-------------|
| `StackPanel` | Empile les enfants verticalement ou horizontalement |
| `Grid` | Grille lignes × colonnes, contrôle précis du placement |
| `DockPanel` | Accroche les enfants aux bords (Top, Bottom, Left, Right) |
| `WrapPanel` | Aligne en ligne, revient à la ligne si ça déborde |
| `Canvas` | Positionnement absolu par coordonnées X/Y |

```xml
<!-- DockPanel : menu en haut, contenu au centre -->
<DockPanel>
    <Menu DockPanel.Dock="Top">...</Menu>
    <StatusBar DockPanel.Dock="Bottom">...</StatusBar>
    <TextEditor />   ← prend tout l'espace restant
</DockPanel>

<!-- WrapPanel : tuiles qui s'adaptent à la largeur -->
<WrapPanel>
    <Button Width="100">A</Button>
    <Button Width="100">B</Button>
    <Button Width="100">C</Button>
</WrapPanel>
```

---

## 4. Triggers et Animations

Les **Triggers** changent l'apparence d'un contrôle en réponse à un état — sans code C#.

```xml
<Style TargetType="Button">
    <Setter Property="Background" Value="Gray" />
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="Blue" />
        </Trigger>
        <Trigger Property="IsPressed" Value="True">
            <Setter Property="Background" Value="Green" />
        </Trigger>
    </Style.Triggers>
</Style>
```

### Animations via Storyboard

```xml
<Button Content="Cliquez">
    <Button.Triggers>
        <EventTrigger RoutedEvent="Button.Click">
            <BeginStoryboard>
                <Storyboard>
                    <DoubleAnimation
                        Storyboard.TargetProperty="Opacity"
                        From="1" To="0" Duration="0:0:0.3" />
                </Storyboard>
            </BeginStoryboard>
        </EventTrigger>
    </Button.Triggers>
</Button>
```

---

## 5. Commands

Dans MVVM, les boutons ne doivent pas appeler du code directement. Les **Commands** découplent l'action de l'UI.

```csharp
// Une Command = une action + une condition d'activation
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public RelayCommand(Action execute, Func<bool> canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public void Execute(object parameter) => _execute();
    public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
    public event EventHandler CanExecuteChanged;
}
```

```csharp
// Dans le ViewModel
public ICommand SauvegarderCommand { get; }

public JoueurViewModel()
{
    SauvegarderCommand = new RelayCommand(Sauvegarder, () => Score > 0);
}
```

```xml
<!-- Dans la View — aucun code-behind nécessaire -->
<Button Content="Sauvegarder" Command="{Binding SauvegarderCommand}" />
```

Le bouton est automatiquement désactivé si `CanExecute` retourne `false`.

---

## À retenir absolument

1. **MVVM** : View ↔ ViewModel (binding) ↔ Model. La View ne connaît pas le Model.
2. **INotifyPropertyChanged** : notifie le binding quand une donnée change (ViewModel → View).
3. **`Mode=TwoWay`** : répercute les saisies vers le ViewModel (View → ViewModel).
4. **DockPanel** = slots aux bords. **WrapPanel** = retour à la ligne automatique.
5. **Triggers** = changements d'apparence selon un état, déclarés en XAML sans code C#.
6. **Commands** = actions découplées de l'UI, avec condition d'activation intégrée.

---

## Quiz — Questions clés

- Dans MVVM, quel est le rôle du ViewModel ? Que connaît-il et que ne connaît-il pas ?
- Pourquoi a-t-on besoin de `INotifyPropertyChanged` si on a déjà le Data Binding ?
- Quelle différence entre `DockPanel` et `WrapPanel` ?
- Comment un Trigger réagit-il au survol d'un bouton sans code C# ?
- Pourquoi utilise-t-on des Commands plutôt qu'un simple event handler dans le code-behind ?

---

*Tour 3 (technique) : MVVM avancé, Behaviors, ControlTemplates, DataTemplates, styles implicites, ressources et dictionnaires de ressources.*
