using System.Collections.Generic;

namespace HumbleEngine;

public interface ICompositeRenderNode : IRenderNode, IEnumerable<RenderDescription> { }
