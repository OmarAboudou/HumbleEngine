# HTML + CSS — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 2 (Intermédiaire) ✅

---

## Ce qu'on a vu au Tour 1 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **HTML** | Langage de balises qui décrit la structure du contenu. |
| **DOM** | L'arbre que le navigateur construit à partir du HTML. |
| **CSS** | Règles d'apparence, indépendantes du contenu. |
| **Sélecteur** | Critère CSS qui désigne les éléments ciblés. |
| **Cascade** | Mécanisme de priorité quand plusieurs règles s'appliquent. |

---

## 1. Types de sélecteurs

```css
/* Balise — tous les h1 */
h1 { color: red; }

/* Classe — tous les éléments avec class="titre" */
.titre { color: blue; }

/* ID — l'élément unique avec id="menu" */
#menu { background: black; }

/* Combinaison — un p à l'intérieur d'un div */
div p { font-size: 14px; }

/* Pseudo-classe — un bouton au survol */
button:hover { background: darkblue; }

/* Pseudo-classe — le premier enfant */
li:first-child { font-weight: bold; }
```

| Sélecteur | Syntaxe | Cible |
|-----------|---------|-------|
| Balise | `h1` | Tous les `<h1>` |
| Classe | `.nom` | Tous les éléments avec `class="nom"` |
| ID | `#nom` | L'élément unique avec `id="nom"` |
| Descendant | `div p` | Tous les `<p>` dans un `<div>` |
| Pseudo-classe | `a:hover` | État dynamique (survol, focus…) |

---

## 2. Spécificité et héritage

### Ordre de priorité (du plus faible au plus fort)

```
Balise       →  spécificité : 0-0-1
Classe       →  spécificité : 0-1-0
ID           →  spécificité : 1-0-0
Style inline →  plus fort que tout
!important   →  écrase tout (à éviter)
```

```css
h1 { color: red; }         /* perd */
.titre { color: blue; }    /* gagne sur h1 */
#main { color: green; }    /* gagne sur .titre */
```

```html
<h1 class="titre" id="main">Quelle couleur ?</h1>
<!-- Réponse : vert — l'ID gagne -->
```

### L'héritage

Certaines propriétés CSS se transmettent automatiquement aux enfants.

```css
body { font-family: Arial; color: #333; }
/* Tous les éléments héritent de font-family et color */
```

- Propriétés de **texte** → héritées (`color`, `font-size`, `font-family`…)
- Propriétés de **boîte** → non héritées (`margin`, `padding`, `border`…)

---

## 3. Le Box Model

Chaque élément HTML est une boîte rectangulaire composée de 4 couches.

```
┌─────────────────────────────┐
│           margin            │  ← espace extérieur (entre les éléments)
│  ┌───────────────────────┐  │
│  │        border         │  │  ← bordure visible
│  │  ┌─────────────────┐  │  │
│  │  │     padding     │  │  │  ← espace intérieur (bordure → contenu)
│  │  │  ┌───────────┐  │  │  │
│  │  │  │  contenu  │  │  │  │
│  │  │  └───────────┘  │  │  │
│  │  └─────────────────┘  │  │
│  └───────────────────────┘  │
└─────────────────────────────┘
```

### La propriété `box-sizing`

Par défaut, `width` ne compte que le contenu. Avec `border-box`, il inclut padding et border.

```css
* {
    box-sizing: border-box; /* bonne pratique universelle */
}
```

---

## 4. Flexbox et Grid

### Flexbox — mise en page sur un axe

```css
.container {
    display: flex;
    justify-content: space-between; /* axe principal (horizontal) */
    align-items: center;            /* axe secondaire (vertical) */
    gap: 16px;
}
```

```
[ A ]  [ B ]  [ C ]   ← justify-content: space-between
```

Idéal pour : barres de navigation, alignements simples, centrage.

### Grid — mise en page sur deux axes

```css
.container {
    display: grid;
    grid-template-columns: 1fr 2fr 1fr;
    gap: 16px;
}
```

```
┌──────┬────────────┬──────┐
│  1fr │    2fr     │  1fr │
└──────┴────────────┴──────┘
```

Idéal pour : layouts de page entière, interfaces complexes.

### Résumé

```
Flexbox → une dimension (ligne OU colonne)
Grid    → deux dimensions (lignes ET colonnes)
```

---

## 5. Le positionnement

| Valeur | Comportement |
|--------|-------------|
| `static` | Par défaut. Suit le flux normal. |
| `relative` | Décalé par rapport à sa position normale, garde sa place dans le flux. |
| `absolute` | Retiré du flux, positionné par rapport à son ancêtre positionné le plus proche. |
| `fixed` | Retiré du flux, positionné par rapport à la fenêtre. Ne bouge pas au scroll. |
| `sticky` | Suit le scroll jusqu'à un seuil, puis se fixe. |

```css
/* Icône superposée sur une image */
.container { position: relative; }
.badge     { position: absolute; top: 8px; right: 8px; }

/* Barre de navigation fixe en haut */
nav { position: fixed; top: 0; width: 100%; }
```

---

## À retenir absolument

1. **Spécificité** : ID > classe > balise. L'ID gagne toujours sur la classe.
2. **Héritage** : propriétés texte héritées, propriétés boîte non héritées.
3. **Box model** : contenu → padding → border → margin.
4. **`box-sizing: border-box`** : `width` inclut padding et border — toujours l'activer.
5. **Flexbox** = une dimension. **Grid** = deux dimensions.
6. **`absolute`** = par rapport au parent positionné. **`fixed`** = par rapport à la fenêtre.

---

## Quiz — Questions clés

- Un élément a une classe et un ID avec des règles conflictuelles — lequel gagne ?
- Quelle est la différence entre `margin` et `padding` ?
- Tu veux centrer 3 boutons avec espace égal — Flexbox ou Grid ? Quelle propriété ?
- Quelle différence entre `position: absolute` et `position: fixed` ?
- Pourquoi utilise-t-on `box-sizing: border-box` ?

---

*Tour 3 (technique) : spécificité avancée, custom properties (variables CSS), animations et transitions, responsive design (media queries), architecture CSS (BEM, utility-first).*
