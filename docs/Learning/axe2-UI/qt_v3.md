# Qt — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 3 (Technique) ✅

---

## Ce qu'on a vu au Tour 2 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **`QObject` + `Q_OBJECT`** | Base de tout. Sans la macro, pas de Signals ni de Slots. |
| **Un Signal → plusieurs Slots** | Tous se déclenchent dans l'ordre de connexion. |
| **Lambda comme Slot** | `connect(bouton, &QPushButton::clicked, [this]() { ... })` |
| **Qt Quick / QML** | Alternative déclarative moderne à Qt Widgets. |
| **Modèle/Vue** | La Vue ne connaît que le modèle — mise à jour ciblée automatique. |
| **Events** | Bas niveau OS. Signals = haut niveau applicatif. |

---

## 1. QObject avancé — Ownership et object trees

Quand tu passes un parent à un `QObject`, le parent en prend **ownership** et sera responsable de le détruire.

```cpp
// Sans parent — tu dois gérer la destruction toi-même
auto bouton = new QPushButton("OK");
delete bouton; // obligatoire

// Avec parent — détruit automatiquement avec la fenêtre
auto bouton = new QPushButton("OK", this); // this = parent
// pas de delete nécessaire
```

Ça crée un **arbre d'objets** : quand la racine est détruite, tous ses descendants le sont dans l'ordre inverse de création.

### moveToThread()

Un `QObject` appartient au thread qui l'a créé. Pour l'exécuter dans un autre thread :

```cpp
auto worker = new Worker();
auto thread = new QThread();

worker->moveToThread(thread); // déplace l'ownership vers le nouveau thread

connect(thread, &QThread::started, worker, &Worker::process);
thread->start();
```

Après `moveToThread`, tous les Slots de `worker` s'exécutent dans `thread`.

---

## 2. Multithreading Qt

### Le pattern Worker Object

L'erreur classique : hériter de `QThread` et mettre la logique dans `run()`. Le pattern recommandé : créer un `QObject` worker et le déplacer dans un `QThread`.

```cpp
// Le Worker — logique métier pure, aucune connaissance des threads
class Worker : public QObject
{
    Q_OBJECT

public slots:
    void process() {
        // calcul long...
        emit finished(resultat);
    }

signals:
    void finished(int resultat);
};
```

```cpp
// Mise en place
auto worker = new Worker();
auto thread = new QThread();

worker->moveToThread(thread);

connect(thread, &QThread::started,  worker, &Worker::process);
connect(worker, &Worker::finished,  this,   &MaFenetre::surResultat);
connect(worker, &Worker::finished,  thread, &QThread::quit);
connect(thread, &QThread::finished, worker, &QObject::deleteLater);
connect(thread, &QThread::finished, thread, &QObject::deleteLater);

thread->start();
```

### Queued connections en détail

Quand émetteur et récepteur sont dans des threads différents, Qt crée automatiquement une **queued connection** :

```
Thread A émet un Signal
        ↓
Qt place l'appel dans la queue d'événements du Thread B
        ↓
Thread B exécute le Slot dans sa propre boucle d'événements
```

Le Slot s'exécute toujours dans le thread du récepteur. Pas de race condition, pas de mutex pour ce cas.

### `deleteLater()`

Ne jamais `delete` un `QObject` depuis un thread autre que le sien.

```cpp
// ✅ safe — destruction planifiée dans le bon thread
worker->deleteLater();

// ⚠️ dangereux si worker est dans un autre thread
delete worker;
```

---

## 3. QML avancé

### Property Bindings réactifs

En QML, `=` crée un **binding réactif** — pas une simple affectation. Si la valeur source change, la cible se met à jour automatiquement.

```qml
Rectangle {
    width: parent.width * 0.5  // binding — suit parent.width en temps réel
    height: width              // binding — toujours carré
    color: mouseArea.containsMouse ? "blue" : "gray"  // binding conditionnel
}
```

### States et Transitions

Un **State** applique un ensemble de modifications simultanées. Les **Transitions** animent le passage entre états.

```qml
Rectangle {
    id: boite
    width: 100; height: 100; color: "gray"

    states: [
        State {
            name: "survole"
            PropertyChanges { target: boite; color: "blue"; width: 150 }
        }
    ]

    transitions: [
        Transition {
            NumberAnimation { properties: "width"; duration: 200 }
            ColorAnimation  { duration: 150 }
        }
    ]

    MouseArea {
        anchors.fill: parent
        hoverEnabled: true
        onEntered: boite.state = "survole"
        onExited:  boite.state = ""
    }
}
```

### Exposer un objet C++ entier au QML

```cpp
// C++
engine.rootContext()->setContextProperty("monModele", modele);
```

```qml
// QML — accès direct à l'objet C++
Text   { text: monModele.score }
Button { onClicked: monModele.reinitialiser() }
```

---

## 4. Performances

### Implicit Sharing (Copy-on-Write)

La plupart des types Qt (`QString`, `QList`, `QImage`…) partagent leurs données jusqu'à modification.

```cpp
QString a = "Bonjour";
QString b = a;          // pas de copie — b partage les données de a
b += " monde";          // copie seulement ici
```

Passer des `QString` et `QList` par valeur est souvent aussi efficace que par référence.

### QStringView — lecture sans copie

```cpp
void afficher(QStringView texte) { /* lecture seule, zéro copie */ }

QString ma_chaine = "Bonjour";
afficher(ma_chaine);   // ✅ pas de copie
afficher(u"Bonjour");  // ✅ fonctionne aussi avec un littéral
```

### Profiling avec Qt Creator

| Outil | Ce qu'il mesure |
|-------|----------------|
| **CPU Profiler** | Fonctions les plus coûteuses, call stack |
| **QML Profiler** | Bindings trop fréquents, rendus QML lents |

Un binding QML qui se réévalue trop souvent est l'erreur de performance la plus courante en Qt Quick.

---

## À retenir absolument

1. **Ownership** = passer un parent à un `QObject` lui délègue sa destruction — pas de `delete` manuel.
2. **`moveToThread()`** = déplace un `QObject` dans un autre thread. Ses Slots s'y exécuteront.
3. **Pattern Worker** = `QObject` worker + `QThread` séparé. Ne pas hériter de `QThread`.
4. **Queued connection** = automatique entre threads. Le Slot s'exécute dans le thread du récepteur.
5. **`deleteLater()`** = destruction safe inter-threads — planifie dans le bon thread.
6. **Property bindings QML** = réactifs, pas des affectations simples.
7. **States / Transitions QML** = équivalent des Triggers / Storyboards WPF.
8. **Implicit Sharing** = Copy-on-Write sur les types Qt — copier est bon marché.

---

## Quiz — Questions clés

- Qu'est-ce que l'ownership dans Qt, et quel avantage concret ça apporte ?
- Pourquoi ne pas mettre la logique directement dans `QThread::run()` ?
- Qu'est-ce qu'une queued connection, et quand Qt en crée-t-il une automatiquement ?
- Pourquoi utiliser `deleteLater()` plutôt que `delete` pour un worker dans un autre thread ?
- Quelle différence entre un `=` en QML et un `=` en C++ ou Dart ?
- Comment expose-t-on un objet C++ au QML ?
- Qu'est-ce que le Copy-on-Write et pourquoi ça change la façon de passer des `QString` ?

---

*Synthèse finale : comparaison de tous les systèmes UI, choix argumenté pour HumbleEngine.*
