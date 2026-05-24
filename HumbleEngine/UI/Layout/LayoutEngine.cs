namespace HumbleEngine;

public sealed class LayoutEngine
{
    private float         _vw;
    private float         _vh;
    private ITextMeasurer _measurer = null!;
    private UINode?       _owner;

    public LayoutNode Layout(RenderElement root, float viewportWidth, float viewportHeight,
                             ITextMeasurer measurer, UINode owner)
    {
        _vw       = viewportWidth;
        _vh       = viewportHeight;
        _measurer = measurer;
        _owner    = owner;
        return Compute(root, new Constraints(viewportWidth, viewportHeight), 0f, 0f,
                       $"{root.GetType().Name}:0");
    }

    // ── Dispatch ───────────────────────────────────────────────────────────────

    private LayoutNode Compute(RenderElement el, Constraints c, float ox, float oy, string path)
        => el switch
        {
            VLayout  v  => ComputeFlow(v,  c, isVertical: true,  ox, oy, path),
            HLayout  h  => ComputeFlow(h,  c, isVertical: false, ox, oy, path),
            Stack    s  => ComputeStack(s, c, ox, oy, path),
            CompositeRenderElement ce => ComputeComposite(ce, c, ox, oy, path),
            _           => ComputeLeaf(el, c, ox, oy, path),
        };

    private ElementId MakeId(RenderElement el, string path)
        => el.Key != null ? ElementId.FromKey(el.Key)
                          : _owner != null ? ElementId.FromPath(_owner, path) : default;

    // ── Length helpers ─────────────────────────────────────────────────────────

    // ── Length helpers ─────────────────────────────────────────────────────────

    // Returns -1 for Auto (caller must supply a fallback).
    private float Resolve(Length l, float available) => l.Kind switch
    {
        LengthKind.Px      => l.Value,
        LengthKind.Percent => l.Value / 100f * available,
        LengthKind.Vw      => l.Value / 100f * _vw,
        LengthKind.Vh      => l.Value / 100f * _vh,
        LengthKind.Fill    => available,
        _                  => -1f, // Auto
    };

    private float ApplyMinMax(float value, Length min, Length max, float available)
    {
        float lo = min.Kind == LengthKind.Auto ? 0f             : Resolve(min, available);
        float hi = max.Kind == LengthKind.Auto ? float.MaxValue  : Resolve(max, available);
        return Math.Clamp(value, lo, hi);
    }

    // Returns (totalWidth, totalHeight) including the element's own margins.
    private (float w, float h) Measure(RenderElement el, Constraints c)
    {
        var node = Compute(el, c, 0f, 0f, "");
        return (node.Box.Width  + el.Margin.Left + el.Margin.Right,
                node.Box.Height + el.Margin.Top  + el.Margin.Bottom);
    }

    // ── Leaf ───────────────────────────────────────────────────────────────────

    private LayoutNode ComputeLeaf(RenderElement el, Constraints c, float ox, float oy, string path)
    {
        float availW = Math.Max(0f, c.MaxWidth  - el.Margin.Left - el.Margin.Right);
        float availH = Math.Max(0f, c.MaxHeight - el.Margin.Top  - el.Margin.Bottom);

        float rw = Resolve(el.Width,  availW);
        float rh = Resolve(el.Height, availH);

        float baseW = rw >= 0f ? rw : availW;
        float baseH = rh >= 0f ? rh : availH;

        if (el is Text t)
        {
            if (rw < 0f) baseW = _measurer.MeasureText(t.Content, t.Font);
            if (rh < 0f) baseH = t.Font.Size;
        }
        else if (el is TextBlock tb)
        {
            if (rh < 0f)
            {
                var lines = _measurer.BreakLines(tb.Content, tb.Font, baseW);
                baseH = lines.Length * tb.Font.Size;
            }
        }

        float w = ApplyMinMax(baseW, el.MinWidth,  el.MaxWidth,  availW);
        float h = ApplyMinMax(baseH, el.MinHeight, el.MaxHeight, availH);

        return new LayoutNode(el, new LayoutBox(ox + el.Margin.Left, oy + el.Margin.Top, w, h), [])
            { Id = MakeId(el, path) };
    }

    // ── Flow layout (VLayout / HLayout) ───────────────────────────────────────

    private LayoutNode ComputeFlow(FlowLayout el, Constraints c, bool isVertical, float ox, float oy, string path)
    {
        float availW = Math.Max(0f, c.MaxWidth  - el.Margin.Left - el.Margin.Right);
        float availH = Math.Max(0f, c.MaxHeight - el.Margin.Top  - el.Margin.Bottom);

        float rw   = Resolve(el.Width,  availW);
        float rh   = Resolve(el.Height, availH);
        bool  autoW = rw < 0f;
        bool  autoH = rh < 0f;

        float selfW = autoW ? availW : ApplyMinMax(rw, el.MinWidth,  el.MaxWidth,  availW);
        float selfH = autoH ? availH : ApplyMinMax(rh, el.MinHeight, el.MaxHeight, availH);

        float padH = el.Padding.Left + el.Padding.Right;
        float padV = el.Padding.Top  + el.Padding.Bottom;
        float contentW = Math.Max(0f, selfW - padH);
        float contentH = Math.Max(0f, selfH - padV);

        var children = el.Children;
        int  n       = children.Count;

        if (n == 0)
        {
            if (autoW && isVertical)  selfW = ApplyMinMax(padH, el.MinWidth,  el.MaxWidth,  availW);
            if (autoH && !isVertical) selfH = ApplyMinMax(padV, el.MinHeight, el.MaxHeight, availH);
            return new LayoutNode(el, new LayoutBox(ox + el.Margin.Left, oy + el.Margin.Top, selfW, selfH), [])
                { Id = MakeId(el, path) };
        }

        // ── Pass 1: measure non-Fill children ──────────────────────────────────

        float totalGaps = (n - 1) * el.Gap;
        float mainFixed = totalGaps;
        int   fillCount = 0;
        var   measured  = new (float w, float h)[n];

        for (int i = 0; i < n; i++)
        {
            var ch       = children[i];
            bool fillMain = isVertical ? ch.Height.Kind == LengthKind.Fill
                                       : ch.Width.Kind  == LengthKind.Fill;
            if (fillMain) { fillCount++; continue; }

            measured[i] = isVertical
                ? Measure(ch, new Constraints(contentW, float.PositiveInfinity))
                : Measure(ch, new Constraints(float.PositiveInfinity, contentH));

            mainFixed += isVertical ? measured[i].h : measured[i].w;
        }

        // ── Pass 2: distribute remaining space to Fill children ────────────────

        float mainAvail   = isVertical ? contentH : contentW;
        float fillMainPx  = fillCount > 0 ? Math.Max(0f, mainAvail - mainFixed) / fillCount : 0f;

        for (int i = 0; i < n; i++)
        {
            var ch       = children[i];
            bool fillMain = isVertical ? ch.Height.Kind == LengthKind.Fill
                                       : ch.Width.Kind  == LengthKind.Fill;
            if (!fillMain) continue;

            measured[i] = isVertical
                ? Measure(ch, new Constraints(contentW, fillMainPx))
                : Measure(ch, new Constraints(fillMainPx, contentH));
        }

        // ── Shrink self to content when Auto ──────────────────────────────────

        if (autoW)
        {
            float w = isVertical
                ? measured.Max(s => s.w) + padH
                : measured.Sum(s => s.w) + totalGaps + padH;
            selfW    = ApplyMinMax(w, el.MinWidth, el.MaxWidth, availW);
            contentW = Math.Max(0f, selfW - padH);
        }
        if (autoH)
        {
            float h = isVertical
                ? measured.Sum(s => s.h) + totalGaps + padV
                : measured.Max(s => s.h) + padV;
            selfH    = ApplyMinMax(h, el.MinHeight, el.MaxHeight, availH);
            contentH = Math.Max(0f, selfH - padV);
        }

        // ── Main axis alignment ────────────────────────────────────────────────

        float mainTotal = (isVertical ? measured.Sum(s => s.h) : measured.Sum(s => s.w)) + totalGaps;
        GetMainAlignment(el.MainAlignment, n, mainTotal, isVertical ? contentH : contentW,
                         out float mainStart, out float gapExtra);

        // ── Pass 3: position children ──────────────────────────────────────────

        var   childNodes  = new LayoutNode[n];
        float cursor      = mainStart;
        float contentOriX = ox + el.Margin.Left + el.Padding.Left;
        float contentOriY = oy + el.Margin.Top  + el.Padding.Top;

        for (int i = 0; i < n; i++)
        {
            var ch = children[i];

            float crossSpan  = isVertical ? measured[i].w : measured[i].h;
            float crossAvail = isVertical ? contentW       : contentH;
            float crossOff   = GetCrossOffset(el.CrossAlignment, crossSpan, crossAvail);

            float childOx, childOy;
            if (isVertical)
            {
                childOx = contentOriX + crossOff;
                childOy = contentOriY + cursor;
            }
            else
            {
                childOx = contentOriX + cursor;
                childOy = contentOriY + crossOff;
            }

            string childPath = path.Length > 0 ? $"{path}/{ch.GetType().Name}:{i}" : "";
            childNodes[i] = Compute(ch, new Constraints(measured[i].w, measured[i].h), childOx, childOy, childPath);

            cursor += (isVertical ? measured[i].h : measured[i].w);
            if (i < n - 1) cursor += el.Gap + gapExtra;
        }

        return new LayoutNode(el,
            new LayoutBox(ox + el.Margin.Left, oy + el.Margin.Top, selfW, selfH),
            childNodes) { Id = MakeId(el, path) };
    }

    private static void GetMainAlignment(MainAlignment alignment, int count,
                                          float total, float available,
                                          out float start, out float extra)
    {
        float free = available - total;
        start = 0f; extra = 0f;
        switch (alignment)
        {
            case MainAlignment.Start:        break;
            case MainAlignment.Center:       start = free / 2f; break;
            case MainAlignment.End:          start = free;      break;
            case MainAlignment.SpaceBetween: extra = count > 1 ? free / (count - 1) : 0f; break;
            case MainAlignment.SpaceAround:
                extra = count > 0 ? free / count       : 0f;
                start = extra / 2f;
                break;
            case MainAlignment.SpaceEvenly:
                extra = count > 0 ? free / (count + 1) : 0f;
                start = extra;
                break;
        }
    }

    private static float GetCrossOffset(CrossAlignment alignment, float childSpan, float available)
        => alignment switch
        {
            CrossAlignment.Center => (available - childSpan) / 2f,
            CrossAlignment.End    => available - childSpan,
            _                     => 0f, // Start / Stretch
        };

    // ── Stack ──────────────────────────────────────────────────────────────────

    private LayoutNode ComputeStack(Stack el, Constraints c, float ox, float oy, string path)
    {
        float availW = Math.Max(0f, c.MaxWidth  - el.Margin.Left - el.Margin.Right);
        float availH = Math.Max(0f, c.MaxHeight - el.Margin.Top  - el.Margin.Bottom);

        float rw   = Resolve(el.Width,  availW);
        float rh   = Resolve(el.Height, availH);
        float selfW = rw < 0f ? availW : ApplyMinMax(rw, el.MinWidth,  el.MaxWidth,  availW);
        float selfH = rh < 0f ? availH : ApplyMinMax(rh, el.MinHeight, el.MaxHeight, availH);

        float contentX = ox + el.Margin.Left + el.Padding.Left;
        float contentY = oy + el.Margin.Top  + el.Padding.Top;
        float contentW = Math.Max(0f, selfW - el.Padding.Left - el.Padding.Right);
        float contentH = Math.Max(0f, selfH - el.Padding.Top  - el.Padding.Bottom);

        var children  = el.Children;
        var childNodes = new LayoutNode[children.Count];

        for (int i = 0; i < children.Count; i++)
        {
            var ch     = children[i];
            var anchor = ch.Anchor ?? Anchor.TopLeft;

            (float childW, float childH, float childX, float childY)
                = ResolveAnchor(anchor, ch, contentW, contentH, contentX, contentY);

            string childPath = path.Length > 0 ? $"{path}/{ch.GetType().Name}:{i}" : "";
            childNodes[i] = Compute(ch,
                new Constraints(childW + ch.Margin.Left + ch.Margin.Right,
                                 childH + ch.Margin.Top  + ch.Margin.Bottom),
                childX, childY, childPath);
        }

        return new LayoutNode(el,
            new LayoutBox(ox + el.Margin.Left, oy + el.Margin.Top, selfW, selfH),
            childNodes) { Id = MakeId(el, path) };
    }

    private (float w, float h, float x, float y) ResolveAnchor(
        Anchor anchor, RenderElement child,
        float parentW, float parentH, float originX, float originY)
    {
        float l = anchor.Left   * parentW;
        float t = anchor.Top    * parentH;
        float r = anchor.Right  * parentW;
        float b = anchor.Bottom * parentH;

        float w, x;
        if (anchor.Right > anchor.Left)
        {
            w = r - l;
            x = originX + l;
        }
        else
        {
            float rw = Resolve(child.Width, parentW);
            w = rw < 0f ? 0f : rw;
            x = originX + l; // point anchor: top-left corner of child at anchor position
        }

        float h, y;
        if (anchor.Bottom > anchor.Top)
        {
            h = b - t;
            y = originY + t;
        }
        else
        {
            float rh = Resolve(child.Height, parentH);
            h = rh < 0f ? 0f : rh;
            y = originY + t;
        }

        return (w, h, x, y);
    }

    // ── Generic composite (Box, etc.) ──────────────────────────────────────────

    private LayoutNode ComputeComposite(CompositeRenderElement el, Constraints c, float ox, float oy, string path)
    {
        float availW = Math.Max(0f, c.MaxWidth  - el.Margin.Left - el.Margin.Right);
        float availH = Math.Max(0f, c.MaxHeight - el.Margin.Top  - el.Margin.Bottom);

        float rw   = Resolve(el.Width,  availW);
        float rh   = Resolve(el.Height, availH);
        float selfW = rw < 0f ? availW : ApplyMinMax(rw, el.MinWidth,  el.MaxWidth,  availW);
        float selfH = rh < 0f ? availH : ApplyMinMax(rh, el.MinHeight, el.MaxHeight, availH);

        float contentX = ox + el.Margin.Left + el.Padding.Left;
        float contentY = oy + el.Margin.Top  + el.Padding.Top;
        float contentW = Math.Max(0f, selfW - el.Padding.Left - el.Padding.Right);
        float contentH = Math.Max(0f, selfH - el.Padding.Top  - el.Padding.Bottom);

        var children  = el.Children;
        var childNodes = new LayoutNode[children.Count];

        for (int i = 0; i < children.Count; i++)
        {
            string childPath = path.Length > 0 ? $"{path}/{children[i].GetType().Name}:{i}" : "";
            childNodes[i] = Compute(children[i], new Constraints(contentW, contentH), contentX, contentY, childPath);
        }

        return new LayoutNode(el,
            new LayoutBox(ox + el.Margin.Left, oy + el.Margin.Top, selfW, selfH),
            childNodes) { Id = MakeId(el, path) };
    }
}
