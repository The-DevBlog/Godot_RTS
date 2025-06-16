shader_type spatial;

uniform float emission_strength = 50.0;
uniform float strobe_speed = 3.0;

void fragment() {
    // Set base color (albedo)
    ALBEDO = vec3(1.0, 0.0, 0.0); // deep red

    // Emission strobe effect
    float strobe = abs(sin(TIME * strobe_speed));
    EMISSION = ALBEDO * emission_strength * strobe;
}
