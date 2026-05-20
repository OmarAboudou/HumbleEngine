# WPF / XAML — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 1 (Vulgarisation) ✅

---

## L'idée centrale

WPF (*Windows Presentation Foundation*) est le système UI de Microsoft pour les applications de bureau Windows. Il reprend la même philosophie que HTML/CSS — **séparer la structure de l'apparence** — mais en version native Windows, écrit en C#.

XAML joue le rôle du HTML. C# joue le rôle de JavaScript. Et WPF a son propre équivalent du CSS pour le style.

---

## Les concepts fondamentaux

| Concept | Définition courte |
|---------|------------------|
| **XAML** | Langage de balisage XML qui décrit la structure de l'UI (≈ HTML). |
| **Contrôle** | Élément interactif : `Button`, `TextBox`, `Slider`… |
| **Panel** | Conteneur de mise en page qui organise ses enfants : `StackPanel`, `Grid`… |
| **Style** | Règles d'apparence applicables à un ou plusieurs contrôles (≈ CSS). |
| **Data Binding** | Lien automatique entre un élément UI et une donnée en C#. |

---

## XAML — L'arbre de l'interface

```xml
<Window>
    <StackPanel>
        <TextBlock Text="Bonjour" />
        <Button Content="Cliquez ici" />
        <TextBox />
    </StackPanel>
</Window>
```

Comme en HTML, c'est un **arbre d'éléments imbriqués**. La `Window` est la racine. Même philosophie, syntaxe différente.

---

## Contrôle vs Panel

| Type | Rôle | Exemples |
|------|------|---------|
| **Contrôle** | Élément *interactif* — fait quelque chose | `Button`, `TextBox`, `Slider`, `CheckBox` |
| **Panel** | Conteneur de *mise en page* — organise ses enfants | `StackPanel`, `Grid`, `DockPanel` |

Un Panel ne s'affiche pas vraiment lui-même — il positionne les éléments qu'il contient.

---

## Les Panels — Mise en page

```xml
<StackPanel Orientation="Vertical">
    <Button Content="Premier" />
    <Button Content="Deuxième" />
</StackPanel>

<Grid>
    <Grid.RowDefinitions>
        <RowDefinition />
        <RowDefinition />
    </Grid.RowDefinitions>
    <Button Grid.Row="0" Content="Ligne 1" />
    <Button Grid.Row="1" Content="Ligne 2" />
</Grid>
```

---

## Le Data Binding — La vraie nouveauté

Le **Data Binding** lie directement un élément UI à une donnée C#. Quand la donnée change, l'interface se met à jour automatiquement.

```xml
<TextBlock Text="{Binding NomDuJoueur}" />
<Slider Value="{Binding Volume}" />
```

```csharp
public string NomDuJoueur { get; set; } = "Hero";
public double Volume { get; set; } = 0.8;
```

Le `TextBlock` affiche automatiquement `NomDuJoueur`. Si le C# change la valeur, l'affichage suit — sans toucher à l'UI manuellement.

---

## Comparaison HTML/CSS ↔ WPF/XAML

```
HTML/CSS                       WPF / XAML
────────────────────           ────────────────────
HTML                           XAML
CSS                            Styles WPF
<div>, <section>               StackPanel, Grid, DockPanel
<button>, <input>              Button, TextBox, Slider
JavaScript                     C#
Pas de binding natif           Data Binding intégré
Navigateur web                 Application Windows native
```

---

## À retenir absolument

1. **XAML** = l'équivalent du HTML pour WPF — décrit la structure de l'UI.
2. **Contrôle** = élément interactif (`Button`, `TextBox`…).
3. **Panel** = conteneur de mise en page — organise ses enfants, ne s'affiche pas lui-même.
4. **Style** = règles d'apparence, comme le CSS.
5. **Data Binding** = lien automatique entre l'UI et les données C# — la grande nouveauté par rapport à HTML/CSS.

---

## Quiz — Questions clés

- Quel langage joue le rôle du HTML dans WPF ?
- Quelle est la différence entre un Contrôle et un Panel dans WPF ?
- À quoi sert le Data Binding ?
- Cite une similarité et une différence entre WPF/XAML et HTML/CSS.
- Dans quel contexte utilise-t-on WPF plutôt que HTML/CSS ?

---

*Tour 2 (intermédiaire) : MVVM (Model-View-ViewModel), INotifyPropertyChanged, types de Panels avancés, Triggers et Animations, Commands.*
