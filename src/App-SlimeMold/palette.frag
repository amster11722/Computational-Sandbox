#version 330

in vec2 fragTexCoord;
in vec4 fragColor;
uniform sampler2D texture0;
uniform float u_time;
out vec4 finalColor;

vec3 colorPalette(float t) {
    vec3 a = vec3(0.5, 0.5, 0.5);
    vec3 b = vec3(0.5, 0.5, 0.5);
    vec3 c = vec3(1.0, 1.0, 1.0);
    vec3 d = vec3(0.0 + u_time * 0.02, 0.33 + u_time * 0.05, 0.67 + u_time * 0.1);
    return a + b * cos(6.28318 * (c * t + d));
}

void main() {
    // Sample the raw grayscale value from the simulation texture
    vec4 texColor = texture(texture0, fragTexCoord);
    
    // Map the trail intensity (Red channel) through our color ramp function
    finalColor.rgb = colorPalette(texColor.r) * texColor.r;
    finalColor.a = 1.0;
}