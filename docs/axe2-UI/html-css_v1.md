# HTML + CSS — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 1 (Vulgarisation) ✅

---

## L'idée centrale

Une page web sépare radicalement **ce qui existe** de **comment ça a l'air** :

- **HTML** décrit la **structure** : "il y a un titre, un paragraphe, un bouton".
- **CSS** décrit l'**apparence** : "le titre est rouge, le bouton est arrondi".

Les deux restent indépendants. Le HTML ne parle jamais de couleurs. Le CSS ne parle jamais de contenu.

---

## Les concepts fondamentaux

| Concept | Définition courte |
|---------|------------------|
| **HTML** | Langage de balises qui décrit la structure du contenu. |
| **DOM** | L'arbre que le navigateur construit à partir du HTML (Document Object Model). |
| **CSS** | Règles d'apparence, indépendantes du contenu. |
| **Sélecteur** | Critère CSS qui désigne les éléments auxquels une règle s'applique. |
| **Cascade** | Mécanisme de priorité quand plusieurs règles CSS s'appliquent au même élément. |

---

## HTML — L'arbre de contenu

Le HTML organise le contenu sous forme d'un **arbre de balises imbriquées**.

```
<html>
└── <body>
    ├── <h1> Titre de la page </h1>
    ├── <p> Un paragraphe de texte </p>
    └── <div>
        ├── <button> Cliquez ici </button>
        └── <img src="photo.png" />
```

Cet arbre s'appelle le **DOM** (*Document Object Model*). C'est la représentation vivante de la page en mémoire — c'est lui qu'on manipule avec JavaScript.

---

## CSS — Les règles de style

Le CSS fonctionne par **règles** : "pour tous les éléments qui correspondent à ce critère, applique ces propriétés".

```css
h1 {
    color: red;
    font-size: 32px;
}

button {
    background-color: blue;
    border-radius: 8px;
}
```

---

## Les 3 concepts CSS fondamentaux

| Concept | Ce que c'est |
|--------|-------------|
| **Sélecteur** | "À qui s'applique cette règle ?" (`h1`, `.bouton`, `#menu`…) |
| **Propriété** | "Qu'est-ce qu'on change ?" (`color`, `font-size`, `margin`…) |
| **Cascade** | Quand plusieurs règles s'appliquent au même élément, laquelle gagne ? |

La **cascade** est l'idée la plus originale du CSS : les règles se cumulent, s'héritent et se surchargent selon des priorités précises. C'est de là que vient le *C* de CSS (*Cascading* Style Sheets).

---

## Structure / Apparence — Séparation des responsabilités

```
HTML                           CSS
────────────────────           ────────────────────
Structure du contenu           Apparence de chaque élément
"Il y a un bouton"             "Le bouton est bleu et arrondi"
"Il y a un titre"              "Le titre est rouge et centré"
Indépendant du style           Indépendant du contenu
```

Changer tout le design d'une page = modifier le CSS sans toucher au HTML.
Changer le contenu = modifier le HTML sans toucher au CSS.

---

## Comparaison avec les moteurs de jeu

```
Godot    → Nodes dans un arbre  (SceneTree)
HTML/CSS → Balises dans un arbre (DOM) + règles de style séparées
```

L'idée d'arbre est la même. La nouveauté : la **séparation structure / apparence** — absente des moteurs de jeu.

---

## À retenir absolument

1. **HTML** = structure du contenu, sous forme d'arbre de balises.
2. **DOM** = l'arbre HTML tel que le navigateur le représente en mémoire.
3. **CSS** = règles d'apparence, indépendantes du contenu.
4. **Sélecteur** = critère qui dit à quelle balise une règle s'applique.
5. **Cascade** = mécanisme de priorité quand plusieurs règles se cumulent.

---

## Quiz — Questions clés

- Quelle est la différence fondamentale entre HTML et CSS ?
- Comment s'appelle l'arbre que le navigateur construit à partir du HTML ?
- À quoi sert un sélecteur CSS ?
- Si tu changes le design complet d'une page, quel fichier touches-tu ?
- Quel concept CSS explique ce qui se passe quand deux règles s'appliquent au même élément ?

---

*Tour 2 (intermédiaire) : types de sélecteurs (classe, id, pseudo-classes), spécificité et héritage, box model, Flexbox et Grid, positionnement.*
