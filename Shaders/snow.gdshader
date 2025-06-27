shader_type spatial;

// Base ground color
uniform vec3 ground_color : source_color = vec3(0.329, 0.486, 0.325); // #547c53
uniform vec3 snow_color : source_color = vec3(0.95, 0.97, 1.0);

// Normal map
uniform sampler2D normal_map : hint_normal;

// Snow parameters
uniform float snow_coverage : hint_range(0.0, 1.0) = 0.5;
uniform float snow_threshold : hint_range(0.0, 1.0) = 0.4;
uniform float noise_scale : hint_range(0.1, 10.0) = 2.0;

// Simple noise function
float noise(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

// Fractal noise
float fractal_noise(vec2 p) {
    float value = 0.0;
    float amplitude = 1.0;
    float frequency = 1.0;
    
    for (int i = 0; i < 4; i++) {
        value += amplitude * (noise(p * frequency) * 2.0 - 1.0);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    
    return value * 0.5 + 0.5;
}

void fragment() {
    vec2 uv = UV;
    
    // Generate noise for snow patches
    float snow_noise = fractal_noise(uv * noise_scale);
    
    // Create snow mask
    float snow_mask = step(snow_threshold, snow_noise * snow_coverage);
    
    // Mix colors
    vec3 final_color = mix(ground_color, snow_color, snow_mask);
    
    ALBEDO = final_color;
    
    // Apply normal map
    NORMAL_MAP = texture(normal_map, uv).rgb;
}