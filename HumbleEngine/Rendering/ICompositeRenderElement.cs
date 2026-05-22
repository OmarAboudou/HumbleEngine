using System.Collections.Generic;

namespace HumbleEngine;

public interface ICompositeRenderElement : IRenderElement, IEnumerable<RenderDescription>
{
    void Add(RenderDescription child);
}
