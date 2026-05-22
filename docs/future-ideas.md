# HumbleEngine — Idées pour le futur

> Idées architecturales ou optimisations identifiées mais non implémentées.
> À revisiter quand un profiler confirme le besoin, ou quand la base de code est plus mature.

---

## RenderTree — stockage hétérogène en byte buffer

**Idée :** Remplacer les tableaux typés séparés (`_spanData[]`, `_boxData[]`…) par un seul `byte[]` contiguë. Chaque élément est écrit via `MemoryMarshal.Write<T>` et lu via `MemoryMarshal.AsRef<T>`. Un tableau d'index `(offset, RenderEntryKind)` permet de retrouver chaque élément sans boxing.

**Bénéfice attendu :** Éliminer les copies de struct pendant `Flatten()`, meilleure localité cache sur l'ensemble des données de rendu.

**Pourquoi pas maintenant :** `RenderDescription` est déjà transient (stack), les tableaux typés actuels sont déjà cache-friendly, et le vrai bottleneck est SkiaSharp (DrawText, DrawRect, OpenGL). À profiler avant d'implémenter.
