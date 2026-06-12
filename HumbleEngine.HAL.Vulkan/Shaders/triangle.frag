#version 450

// The colour arriving here is never one of the three vertex colours: the
// rasterizer interpolates the location-0 output barycentrically per pixel.

layout(location = 0) in  vec3 fragColor;
layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4(fragColor, 1.0);
}
