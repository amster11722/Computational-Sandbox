#version 330

// Fragment inputs
in vec2 fragTexCoord;

// Texture from raylib
uniform sampler2D texture0;

// Output pixel color
out vec4 finalColor;

void main() {

    ivec2 texSize = textureSize(texture0, 0);
    vec2 texelSize = 1.0 / vec2(texSize);

    vec4 sum = vec3(0.0);

    // 3x3 blur around current coordinate

    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            vec2 offset = vec2(x, y) * texelSize;
            sum += texture(texture0, fragTexCoord + offset);
        }
    }

    vec4 avgColor = sum / 9.0;

    // Decay by 5%
    finalColor.rgb = avgColor.rgb * 0.95;
    finalColor.a = 1.0;
}