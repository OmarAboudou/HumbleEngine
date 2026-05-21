# WPF / XAML — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 3 (Technique) ✅

---

## Ce qu'on a vu au Tour 2 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **MVVM** | View ↔ ViewModel (binding) ↔ Model. La View ne connaît pas le Model. |
| **INotifyPropertyChanged** | Notifie le binding quand une donnée change (ViewModel → View). |
| **`Mode=TwoWay`** | Répercute les saisies vers le ViewModel (View → ViewModel). |
| **DockPanel / WrapPanel** | Slots aux bords / retour à la ligne automatique. |
| **Triggers** | Changements d'apparence selon un état, en XAML sans code C#. |
| **Commands** | Actions découplées de l'UI, avec condition d'activation intégrée. |

---

## 1. MVVM avancé

### ViewModelBase — factoriser le boilerplate

```csharp
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetProperty<T>(ref T field, T value,
        [CallerMemberName] string name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

// Usage dans un ViewModel
public class JoueurViewModel : ViewModelBase
{
    private int _score;
    public int Score
    {
        get => _score;
        set => SetProperty(ref _score, value); // notification automatique
    }
}
```

### ObservableCollection — listes réactives

`List<T>` ne notifie pas le binding quand on ajoute/retire un élément. `ObservableCollection<T>` le fait automatiquement.

```csharp
public ObservableCollection<string> Joueurs { get; } = new();

Joueurs.Add("Alice");   // → ListView se met à jour automatiquement
Joueurs.Remove("Bob");
```

```xml
<ListView ItemsSource="{Binding Joueurs}" />
```

Règle : `List<T>` pour les données statiques, `ObservableCollection<T>` pour les données qui changent.

### Navigation MVVM — changer de ViewModel, pas de Page

```csharp
// Dans le ViewModel principal
private ViewModelBase _vueActuelle;
public ViewModelBase VueActuelle
{
    get => _vueActuelle;
    set => SetProperty(ref _vueActuelle, value);
}

// Naviguer
VueActuelle = new ParametresViewModel();
```

```xml
<!-- WPF affiche automatiquement le DataTemplate du ViewModel courant -->
<ContentControl Content="{Binding VueActuelle}" />
```

---

## 2. Behaviors

Un **Behavior** encapsule un comportement réutilisable et s'attache à un contrôle en XAML — sans modifier sa classe ni écrire de code-behind.

```csharp
public class SelectAllOnFocusBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
        => AssociatedObject.GotFocus += OnGotFocus;

    protected override void OnDetaching()
        => AssociatedObject.GotFocus -= OnGotFocus;

    private void OnGotFocus(object sender, RoutedEventArgs e)
        => AssociatedObject.SelectAll();
}
```

```xml
<TextBox>
    <i:Interaction.Behaviors>
        <local:SelectAllOnFocusBehavior />
    </i:Interaction.Behaviors>
</TextBox>
```

- **`AssociatedObject`** = le contrôle auquel le Behavior est attaché.
- **`OnAttached` / `OnDetaching`** = cycle de vie du Behavior (abonnement / désabonnement).
- Réutilisable sur n'importe quel `TextBox` sans toucher à la classe ni au code-behind.

---

## 3. ControlTemplate — redéfinir l'apparence d'un contrôle

Remplace **entièrement** l'apparence visuelle d'un contrôle.

```xml
<Style TargetType="Button">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}"
                        CornerRadius="8"
                        Padding="{TemplateBinding Padding}">
                    <ContentPresenter HorizontalAlignment="Center"
                                      VerticalAlignment="Center" />
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="Background" Value="#2563EB" />
                    </Trigger>
                    <Trigger Property="IsPressed" Value="True">
                        <Setter Property="Background" Value="#1D4ED8" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

- **`TemplateBinding`** — lie une propriété du template à une propriété du contrôle (ex : `Background`).
- **`ContentPresenter`** — emplacement réservé au contenu du contrôle. Obligatoire pour que le texte du bouton s'affiche.

---

## 4. DataTemplate — définir comment afficher un type de données

WPF utilise automatiquement le DataTemplate correspondant dès qu'il rencontre un objet du type ciblé.

```xml
<DataTemplate DataType="{x:Type local:Joueur}">
    <StackPanel Orientation="Horizontal">
        <Image Source="{Binding Avatar}" Width="32" />
        <TextBlock Text="{Binding Nom}" FontWeight="Bold" />
        <TextBlock Text="{Binding Score}" Foreground="Gray" />
    </StackPanel>
</DataTemplate>
```

### Pattern Navigation MVVM complet

```xml
<!-- Chaque ViewModel → sa View, automatiquement -->
<DataTemplate DataType="{x:Type vm:AccueilViewModel}">
    <views:AccueilView />
</DataTemplate>
<DataTemplate DataType="{x:Type vm:ParametresViewModel}">
    <views:ParametresView />
</DataTemplate>

<ContentControl Content="{Binding VueActuelle}" />
```

Changer `VueActuelle` dans le ViewModel → WPF instancie la View correspondante. Navigation sans code-behind.

---

## 5. Styles implicites

Un style **avec** `x:Key` s'applique uniquement si on le référence explicitement :

```xml
<Style x:Key="MonBouton" TargetType="Button">...</Style>
<Button Style="{StaticResource MonBouton}" />
```

Un style **sans** `x:Key` est **implicite** — s'applique automatiquement à tous les contrôles du type dans sa portée :

```xml
<!-- Appliqué à TOUS les Button dans cette portée -->
<Style TargetType="Button">
    <Setter Property="Background" Value="#3B82F6" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="Padding" Value="12 6" />
</Style>
```

La portée suit la hiérarchie XAML : ressources d'une `Window` → toute la fenêtre. Ressources dans `App.xaml` → toute l'application.

---

## 6. ResourceDictionary — organiser les ressources

### Fichiers séparés par responsabilité

```
Resources/
├── Colors.xaml
├── Typography.xaml
├── ButtonStyles.xaml
└── DataTemplates.xaml
```

```xml
<!-- Colors.xaml -->
<ResourceDictionary>
    <Color x:Key="PrimaryColor">#3B82F6</Color>
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}" />
</ResourceDictionary>
```

### Fusion dans App.xaml avec MergedDictionaries

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Resources/Colors.xaml" />
            <ResourceDictionary Source="Resources/Typography.xaml" />
            <ResourceDictionary Source="Resources/ButtonStyles.xaml" />
            <ResourceDictionary Source="Resources/DataTemplates.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### StaticResource vs DynamicResource

| | `StaticResource` | `DynamicResource` |
|--|---|---|
| **Résolution** | À la compilation XAML | Au runtime |
| **Performance** | Meilleure | Légèrement plus lente |
| **Cas d'usage** | Ressource fixe | Ressource qui change (thème dynamique) |

```xml
<Button Background="{StaticResource PrimaryBrush}" />   <!-- figé -->
<Button Background="{DynamicResource PrimaryBrush}" />  <!-- réactif au changement de thème -->
```

---

## À retenir absolument

1. **ViewModelBase** = factorise `INotifyPropertyChanged`. `SetProperty` notifie uniquement si la valeur change.
2. **ObservableCollection** = `List<T>` réactive — notifie le binding à chaque ajout/suppression.
3. **Behavior** = comportement encapsulé, attachable en XAML sans modifier la classe ni le code-behind.
4. **ControlTemplate** = redéfinir l'apparence complète. `TemplateBinding` + `ContentPresenter` sont indispensables.
5. **DataTemplate** = définir le rendu d'un type de données. Base du pattern de navigation MVVM.
6. **Style implicite** = sans `x:Key` → s'applique à tous les contrôles du type dans sa portée.
7. **MergedDictionaries** = fusionner plusieurs ResourceDictionaries dans App.xaml.
8. **StaticResource** = résolu à la compilation. **DynamicResource** = résolu au runtime (thèmes).

---

## Quiz — Questions clés

- À quoi sert `SetProperty` dans un `ViewModelBase` ? Que fait-il de différent par rapport à un simple setter avec `OnPropertyChanged` ?
- Quelle différence entre `List<T>` et `ObservableCollection<T>` dans un contexte de binding ?
- Qu'est-ce qu'un Behavior et pourquoi est-il préférable au code-behind pour MVVM ?
- Dans un ControlTemplate, à quoi sert `ContentPresenter` ?
- Comment WPF sait-il quel DataTemplate utiliser sans qu'on le spécifie explicitement ?
- Quelle différence entre un style avec et sans `x:Key` ?
- Quand utiliser `DynamicResource` plutôt que `StaticResource` ?

---

*Synthèse finale : comparaison de tous les systèmes UI, choix argumenté pour HumbleEngine.*
