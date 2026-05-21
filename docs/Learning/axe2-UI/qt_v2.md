# Qt — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 2 (Intermédiaire) ✅

---

## Ce qu'on a vu au Tour 1 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **Widget** | La brique de base de l'interface Qt. |
| **Signal** | Événement émis par un objet ("quelque chose s'est passé"). |
| **Slot** | Fonction qui réagit à un Signal. |
| **connect()** | Lien explicite entre un Signal et un Slot. |
| **Cross-platform** | Une base de code, tous les OS desktop. |

---

## 1. QObject et le meta-object system

Tout dans Qt hérite de **`QObject`**. C'est la classe de base qui rend les Signals & Slots possibles.

```cpp
class MonBouton : public QObject
{
    Q_OBJECT  // ← macro obligatoire — active le meta-object system

public:
    MonBouton(QObject* parent = nullptr);

signals:
    void clique();

public slots:
    void surClique();
};
```

La macro `Q_OBJECT` est obligatoire sur toute classe qui utilise des Signals ou des Slots. Elle active le **meta-object system** — une forme d'introspection qui permet à Qt d'inspecter les types à l'exécution, de connecter des Signals et Slots dynamiquement, et de gérer la communication inter-threads.

---

## 2. Signals & Slots avancés

### Un Signal → plusieurs Slots

```cpp
connect(slider, &QSlider::valueChanged, label,  &QLabel::setNum);
connect(slider, &QSlider::valueChanged, this,   &MaFenetre::sauvegarderVolume);
connect(slider, &QSlider::valueChanged, player, &QMediaPlayer::setVolume);
```

Un seul Signal peut être connecté à autant de Slots que nécessaire. Tous se déclenchent dans l'ordre de connexion.

### Utiliser une lambda comme Slot

```cpp
connect(bouton, &QPushButton::clicked, [this]() {
    label->setText("Cliqué !");
    score += 1;
});
```

### Déconnecter

```cpp
// Déconnecter une connexion spécifique
disconnect(bouton, &QPushButton::clicked, this, &MaFenetre::sauvegarder);

// Déconnecter tout ce qui était connecté à un Signal
disconnect(bouton, &QPushButton::clicked, nullptr, nullptr);
```

---

## 3. Qt Quick / QML

Qt Widgets est l'approche classique C++. **Qt Quick** est l'approche moderne — elle utilise **QML**, un langage déclaratif proche de JSON.

```qml
import QtQuick 2.15
import QtQuick.Controls 2.15

ApplicationWindow {
    width: 400
    height: 300
    visible: true

    Column {
        anchors.centerIn: parent
        spacing: 16

        Text {
            text: "Bonjour Qt Quick"
            font.pixelSize: 24
        }

        Button {
            text: "Cliquez"
            onClicked: console.log("cliqué !")
        }
    }
}
```

### Qt Widgets vs Qt Quick

```
Qt Widgets              Qt Quick / QML
──────────────          ──────────────
C++ pur                 QML + C++ pour la logique
Applications bureau     Bureau ET mobile
Style natif OS          Style personnalisé, animations fluides
Mature, stable          Plus moderne, mieux pour les UIs riches
```

Le workflow courant : logique métier en C++, UI en QML, les deux reliés via `QObject`.

---

## 4. Le modèle/vue (MVC)

Qt a son propre système **Modèle / Vue** pour afficher des listes et des arbres de données.

| Classe | Rôle |
|--------|------|
| `QAbstractListModel` | Le modèle — contient les données |
| `QListView` | La vue — affiche les données du modèle |
| `QItemDelegate` | Le délégué — contrôle le rendu de chaque item |

```cpp
QListView* vue = new QListView();
MonModele* modele = new MonModele();
vue->setModel(modele);  // branchement — la vue ne connaît que le modèle
```

Si le modèle change, la vue se **met à jour automatiquement pour les items concernés** — pas un re-rendu complet. Même principe que le Data Binding WPF ou l'InheritedWidget Flutter.

---

## 5. Gestion des événements

Qt a deux systèmes parallèles : les Signals & Slots (haut niveau) et les **Events** (bas niveau).

```cpp
class MonWidget : public QWidget
{
protected:
    void keyPressEvent(QKeyEvent* event) override
    {
        if (event->key() == Qt::Key_Escape)
            close();
        else
            QWidget::keyPressEvent(event); // transmettre aux parents
    }

    void mousePressEvent(QMouseEvent* event) override
    {
        qDebug() << "Clic en" << event->pos();
    }
};
```

### Events vs Signals & Slots

```
Events (bas niveau)         Signals & Slots (haut niveau)
───────────────────         ─────────────────────────────
Interactions physiques OS   Réactions logiques applicatives
Clavier, souris, resize     Bouton cliqué, valeur changée
Override de méthodes        connect()
Distribués par Qt           Déclenchés par ton code
```

En pratique : Signals & Slots pour 90% des cas. Events uniquement pour des comportements fins (drag & drop custom, dessin à la main, raccourcis globaux).

---

## À retenir absolument

1. **`QObject` + `Q_OBJECT`** = base de tout. Sans la macro, pas de Signals ni de Slots.
2. **Un Signal → plusieurs Slots** : tous se déclenchent dans l'ordre de connexion.
3. **Lambdas** : on peut connecter un Signal directement à une fonction anonyme.
4. **Qt Quick / QML** = alternative déclarative moderne à Qt Widgets, idéale pour les UIs riches.
5. **Modèle/Vue** : la Vue ne connaît que le modèle — mise à jour automatique et ciblée.
6. **Events** = bas niveau OS. **Signals** = haut niveau applicatif.

---

## Quiz — Questions clés

- À quoi sert la macro `Q_OBJECT` ?
- Peut-on connecter un Signal à plusieurs Slots ?
- Quelle différence principale entre Qt Widgets et Qt Quick / QML ?
- Dans le Modèle/Vue, que se passe-t-il si le modèle change ?
- Quand utilise-t-on les Events plutôt que les Signals ?

---

*Tour 3 (technique) : QObject avancé (parent/enfant, ownership), multithreading Qt (QThread, signals inter-threads), QML avancé (bindings, états, transitions), performances.*
