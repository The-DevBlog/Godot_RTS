shader_type spatial;
render_mode unshaded;

uniform vec4 ember_color : source_color = vec4(1.0, 0.2, 0.0, 1.0); // Ember glow
uniform float edge_fade : hint_range(0.0, 2.0) = 1.0;

void fragment() {
    // Use the normal's Y component (facing outward) for gradient glow
    float intensity = dot(NORMAL, VIEW); // View-facing = brighter
    intensity = clamp(intensity, 0.0, 1.0);

    // Sharpen or soften the fade to black
    float glow = pow(intensity, edge_fade);

    vec3 color = mix(vec3(0.0), ember_color.rgb, glow);

    ALBEDO = color;
    ALPHA = 1.0;
}
