namespace HumbleEngine;

public enum HitTestFilter
{
    Ignore,   // hover uniquement, pas de click (défaut)
    Pass,     // hover + click, propagé vers les ancêtres
    Stop,     // hover + click, stoppé
    Disabled, // aucun événement — ni hover ni click (nodes purement décoratifs)
}
