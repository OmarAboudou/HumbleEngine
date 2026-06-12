#version 450

// UI übershader, vertex side: no vertex input at all — the unit quad is born
// from gl_VertexIndex (two triangles covering 0..1), stretched onto the
// pixel-space rect pushed per draw, then projected to clip space by the
// orthographic matrix pushed once per frame. UVs come out for free and feed
// the future modes (texture, text, rounded corners).

layout(push_constant) uniform Push {
    mat4 projection; // offset  0 — per frame: pixels -> clip (origin top-left)
    vec4 rect;       // offset 64 — per draw: x, y, width, height in pixels
    vec4 color;      // offset 80 — per draw: RGBA
    int  mode;       // offset 96 — per draw: fragment-side interpretation
} push;

layout(location = 0) out vec2 uv;

const vec2 corners[6] = vec2[](
    vec2(0.0, 0.0), vec2(1.0, 0.0), vec2(0.0, 1.0),
    vec2(1.0, 0.0), vec2(1.0, 1.0), vec2(0.0, 1.0)
);

void main() {
    vec2 unit  = corners[gl_VertexIndex];
    vec2 pixel = push.rect.xy + unit * push.rect.zw;
    gl_Position = push.projection * vec4(pixel, 0.0, 1.0);
    uv = unit;
}
