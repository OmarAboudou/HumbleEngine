namespace HumbleEngine;

public static class HitTest
{
    // Phase 1 — trouve le Node le plus profond dont les bounds contiennent le point.
    // Le filtre est ignoré : on veut le candidat géographiquement le plus précis.
    public static Node? Find(Node root, float x, float y)
    {
        for (int i = root.Children.Count - 1; i >= 0; i--)
        {
            var hit = Find(root.Children[i], x, y);
            if (hit is not null) return hit;
        }
        return root.ComputedBounds.Contains(x, y) ? root : null;
    }

    // Phase 2 — dispatche un clic depuis le node cible en remontant.
    // Stop : appelle et s'arrête. Pass : appelle et remonte. Ignore : remonte sans appeler.
    public static void DispatchClick(Node? node)
    {
        while (node is not null)
        {
            switch (node.MouseFilter)
            {
                case HitTestFilter.Stop:
                    node.OnClick();
                    return;
                case HitTestFilter.Pass:
                    node.OnClick();
                    node = node.Parent;
                    break;
                default: // Ignore
                    node = node.Parent;
                    break;
            }
        }
    }

    // Retourne la chaîne d'ancêtres depuis le node cible jusqu'à la racine.
    // Exclut les Disabled — tous les autres reçoivent MouseEnter/MouseLeave.
    public static List<Node> GetHoveredPath(Node? node)
    {
        var path = new List<Node>();
        while (node is not null)
        {
            if (node.MouseFilter != HitTestFilter.Disabled)
                path.Add(node);
            node = node.Parent;
        }
        return path;
    }

    // Retourne le premier ancêtre interactif (Pass ou Stop) dans le chemin.
    // Utilisé pour identifier la cible d'un clic au MouseDown.
    public static Node? FirstInteractive(List<Node> path)
        => path.FirstOrDefault(n => n.MouseFilter is HitTestFilter.Pass or HitTestFilter.Stop);
}
