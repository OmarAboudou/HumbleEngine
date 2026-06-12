#version 450

// The vertex data now comes from a vertex buffer: each location matches one
// attribute description in the pipeline's vertex input state (vec2 at offset 0,
// vec3 at offset 8, stride 20). Clip-space coordinates, Vulkan NDC Y-down.

layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec3 inColor;

layout(location = 0) out vec3 fragColor;

void main() {
    gl_Position = vec4(inPosition, 0.0, 1.0);
    fragColor   = inColor;
}
