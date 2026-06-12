#version 450

// UI übershader, fragment side: one closed set of modes, switched per draw —
// a single pipeline for the whole UI, no combinatorial PSO explosion. Mode 0
// (flat colour) is the only one so far; texture, text glyphs and rounded
// corners will join the switch, in this same shader.

layout(push_constant) uniform Push {
    mat4 projection;
    vec4 rect;
    vec4 color;
    int  mode;
} push;

layout(location = 0) in vec2 uv;

layout(location = 0) out vec4 outColor;

void main() {
    switch (push.mode) {
        case 0: // flat colour
        default:
            outColor = push.color;
    }
}
