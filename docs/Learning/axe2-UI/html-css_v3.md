# HTML + CSS — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 3 (Technique) ✅

---

## Ce qu'on a vu au Tour 2 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **Spécificité** | ID > classe > balise. L'ID gagne toujours. |
| **Héritage** | Propriétés texte héritées, propriétés boîte non héritées. |
| **Box model** | contenu → padding → border → margin. |
| **`box-sizing: border-box`** | `width` inclut padding et border. |
| **Flexbox** | Une dimension. **Grid** = deux dimensions. |
| **`absolute`** | Par rapport au parent positionné. **`fixed`** = par rapport à la fenêtre. |

---

## 1. Spécificité avancée

### Le triplet (A, B, C)

| Lettre | Ce qu'elle compte |
|--------|------------------|
| **A** | Nombre d'IDs |
| **B** | Nombre de classes, pseudo-classes, attributs |
| **C** | Nombre de balises et pseudo-éléments |

```css
p              →  (0, 0, 1)
.titre         →  (0, 1, 0)
p.titre        →  (0, 1, 1)
#menu          →  (1, 0, 0)
#menu .titre p →  (1, 1, 1)
```

On compare de gauche à droite. 10 classes ne battent jamais 1 ID.

### Sélecteurs modernes

| Sélecteur | Spécificité | Usage |
|-----------|-------------|-------|
| `:is(h1, h2).titre` | Celle du sélecteur le plus fort du groupe | Grouper sans répéter |
| `:where(h1, h2).titre` | **(0, 1, 0)** — le `:where()` n'ajoute rien | Resets, bibliothèques |
| `p:not(.special)` | Spécificité de ce qu'il contient | Exclure des éléments |

```css
/* :where() — idéal pour les resets : spécificité zéro, facile à surcharger */
:where(h1, h2, h3) { margin: 0; }
```

---

## 2. Custom Properties (variables CSS)

Variables CSS natives, sans préprocesseur. Elles respectent la **cascade** et le **scope**.

```css
/* Déclaration globale */
:root {
    --couleur-primaire: #3b82f6;
    --espacement-base: 8px;
    --rayon-bordure: 4px;
}

/* Utilisation */
button {
    background: var(--couleur-primaire);
    padding: var(--espacement-base) calc(var(--espacement-base) * 2);
}
```

### Scope — redéfinition locale

```css
:root { --couleur: blue; }

.theme-sombre {
    --couleur: white; /* redéfini pour cet élément et tous ses enfants */
}

p { color: var(--couleur); }
```

### Interop JavaScript

```javascript
// Lire
getComputedStyle(document.documentElement).getPropertyValue('--couleur-primaire');

// Écrire — propage à tout le DOM
document.documentElement.style.setProperty('--couleur-primaire', '#ef4444');
```

---

## 3. Transitions et Animations

### `transition` — changement d'état A → B

```css
button {
    background: blue;
    transition: background 0.3s ease, transform 0.2s ease-out;
    /* propriété | durée | timing */
}

button:hover {
    background: darkblue;
    transform: scale(1.05);
}
```

### `@keyframes` — animation en plusieurs étapes

```css
@keyframes glisser {
    from { transform: translateX(-100%); opacity: 0; }
    to   { transform: translateX(0);     opacity: 1; }
}

.panneau {
    animation: glisser 0.4s ease-out forwards;
    /*         nom     durée  timing  fill-mode */
}
```

### fill-mode

| Valeur | Effet |
|--------|-------|
| `forwards` | Reste à l'état final après la fin |
| `backwards` | Applique l'état initial avant le délai |
| `both` | Les deux |

### Timing functions

```css
ease            /* lent → rapide → lent (défaut) */
ease-in         /* lent au début */
ease-out        /* lent à la fin */
linear          /* vitesse constante */
cubic-bezier(0.68, -0.55, 0.27, 1.55)  /* spring, rebond… */
```

### `will-change` — promotion GPU

```css
.element-anime {
    will-change: transform, opacity;
    /* Monte l'élément sur le GPU à l'avance */
}
```

⚠️ À utiliser avec parcimonie — consomme de la mémoire vidéo.

---

## 4. Responsive Design

### Mobile-first avec `min-width`

```css
/* Base : mobile */
.container { padding: 16px; }

/* Tablette et + */
@media (min-width: 768px) {
    .container { padding: 32px; }
}

/* Desktop large */
@media (min-width: 1200px) {
    .container { max-width: 1200px; margin: auto; }
}
```

On écrit d'abord le CSS mobile, on surcharge pour les grands écrans avec `min-width`.

### Unités viewport

| Unité | Définition |
|-------|-----------|
| `vw` | % de la largeur de la fenêtre |
| `vh` | % de la hauteur de la fenêtre |
| `dvh` | Hauteur dynamique — corrige le bug des barres mobiles |
| `clamp(min, préféré, max)` | Valeur fluide entre min et max |

```css
.hero { height: 100dvh; }  /* ✅ plein écran sur mobile */

h1 { font-size: clamp(1.5rem, 4vw, 3rem); }
/* Taille fluide — plus besoin de media query pour les textes */
```

### Container Queries

Réagissent à la taille du **conteneur parent**, pas de la fenêtre — idéal pour les composants réutilisables.

```css
.carte-container {
    container-type: inline-size; /* déclare un conteneur */
}

@container (min-width: 400px) {
    .carte {
        display: flex; /* layout différent si la carte est large */
    }
}
```

---

## 5. Architecture CSS

### BEM — Block Element Modifier

Naming convention qui encode la structure dans les noms de classes.

```css
.carte { }               /* Block */
.carte__titre { }        /* Element (double underscore) */
.carte__bouton { }
.carte--mise-en-avant { }  /* Modifier (double tiret) */
.carte__bouton--desactive { }
```

```html
<div class="carte carte--mise-en-avant">
    <h2 class="carte__titre">Titre</h2>
    <button class="carte__bouton carte__bouton--desactive">OK</button>
</div>
```

✅ Spécificité toujours (0,1,0). Aucun conflit. Structure lisible dans le HTML.

### Utility-first (philosophie Tailwind)

Chaque classe fait une seule chose — on compose l'UI avec des briques atomiques.

```html
<button class="bg-blue-500 text-white px-6 py-3 rounded-lg font-bold hover:bg-blue-600">
    OK
</button>
```

✅ Pas de dead CSS. Pas de nommage. Cohérence forcée.
⚠️ HTML verbeux. Courbe d'apprentissage des noms de classes.

### CSS Modules

Portée locale automatique — les noms de classes sont rendus uniques à la compilation.

```css
/* Button.module.css */
.bouton { background: blue; }
```

```javascript
import styles from './Button.module.css';
<button className={styles.bouton}>OK</button>
/* rendu : <button class="Button_bouton__x7k9q"> */
```

✅ Zéro conflit de nommage. Portée garantie. Natif dans React, Vue, Next.js…

---

## Tableau de synthèse

| Approche | Avantage | Inconvénient |
|----------|----------|--------------|
| **BEM** | Pas de conflit, lisible | Nommage verbeux |
| **Utility-first** | Rapide, cohérent, pas de dead CSS | HTML verbeux |
| **CSS Modules** | Portée locale garantie | Nécessite un bundler JS |

---

## À retenir absolument

1. **Spécificité** = triplet (A, B, C). `:where()` = spécificité zéro — parfait pour les resets.
2. **Custom properties** = variables CSS natives, scopées au DOM, lisibles/modifiables en JS.
3. **`transition`** = A → B. **`@keyframes`** = animation en plusieurs étapes.
4. **Mobile-first** = écrire le CSS mobile en base, surcharger avec `min-width`.
5. **`clamp()`** = taille fluide sans media query. **`dvh`** = plein écran mobile correct.
6. **Container Queries** = réagir à la taille du conteneur, pas de la fenêtre.
7. **BEM** = nommage structuré. **Utility-first** = composition atomique. **CSS Modules** = portée locale.

---

## Quiz — Questions clés

- Quelle est la spécificité de `#menu .titre p` ?
- Quelle différence entre `:is()` et `:where()` en termes de spécificité ?
- Comment faire qu'une variable CSS s'applique différemment dans un thème sombre ?
- Quelle différence fondamentale entre `transition` et `@keyframes` ?
- Pourquoi `dvh` est-il préférable à `vh` sur mobile ?
- Qu'est-ce qu'une Container Query, et en quoi diffère-t-elle d'une Media Query ?
- Cite un avantage et un inconvénient de l'approche utility-first.

---

*Synthèse finale : comparaison de tous les systèmes UI, choix argumenté pour HumbleEngine.*
