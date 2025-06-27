shader_type spatial;

// Base ground color
uniform vec3 ground_color : source_color = vec3(0.329, 0.486, 0.325); // #547c53
uniform vec3 snow_color : source_color = vec3(0.95, 0.97, 1.0);

// Textures
uniform sampler2D noise_texture : source_color, hint_default_white;
uniform sampler2D normal_map : hint_normal;

// Snow parameters
uniform float snow_coverage : hint_range(0.0, 1.0) = 0.3;
uniform float snow_threshold : hint_range(0.0, 1.0) = 0.6;
uniform float patch_softness : hint_range(0.01, 0.3) = 0.1;

void fragment() {
    vec2 uv = UV;
    
    // Sample the noise texture
    float noise_value = texture(noise_texture, uv).r;
    
    // Create snow mask with soft edges
    float snow_mask = smoothstep(
        snow_threshold - patch_softness,
        snow_threshold + patch_softness,
        noise_value * snow_coverage
    );
    
    // Mix colors
    vec3 final_color = mix(ground_color, snow_color, snow_mask);
    
    ALBEDO = final_color;
    
    // Apply normal map
    NORMAL_MAP = texture(normal_map, UV).rgb;
}