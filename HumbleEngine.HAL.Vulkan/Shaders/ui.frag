#version 450

// UI übershader, fragment side: one closed set of modes, switched per draw —
// a single pipeline for the whole UI, no combinatorial PSO explosion. One
// combined image sampler is always bound (flat draws bind a 1×1 white texture
// they never sample), so the layout is uniform across modes.

layout(push_constant) uniform Push {
    mat4 projection;
    vec4 rect;
    vec4 uvRect;
    vec4 color;
    int  mode;
} push;

layout(set = 0, binding = 0) uniform sampler2D tex;

layout(location = 0) in vec2 uv;

layout(location = 0) out vec4 outColor;

void main() {
    switch (push.mode) {
        case 1: // textured: sampled RGBA, tinted
            outColor = texture(tex, uv) * push.color;
            break;
        case 2: // glyph: coverage in the red channel, colour from the tint
            outColor = vec4(push.color.rgb, push.color.a * texture(tex, uv).r);
            break;
        case 0: // flat colour
        default:
            outColor = push.color;
    }
}
