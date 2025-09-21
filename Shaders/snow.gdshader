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
uniform float patch_softness : hint_range(0.00, 0.3) = 0.1;

// Outline parameters
uniform float outline_width : hint_range(0.0, 0.1) = 0.02;
uniform vec3 outline_color : source_color = vec3(0.0, 0.0, 0.0);

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

    // Sample neighboring pixels to detect edges
    vec2 texel_size = 1.0 / vec2(textureSize(noise_texture, 0));
    float edge_detection = 0.0;

    // Check 4 neighboring pixels
    float outline_scale = 0.5;
    float left = texture(noise_texture, uv + vec2(-texel_size.x * outline_width * outline_scale, 0.0)).r;
    float right = texture(noise_texture, uv + vec2(texel_size.x * outline_width * outline_scale, 0.0)).r;
    float up = texture(noise_texture, uv + vec2(0.0, -texel_size.y * outline_width * outline_scale)).r;
    float down = texture(noise_texture, uv + vec2(0.0, texel_size.y * outline_width * outline_scale)).r;

    // Convert to snow masks
    float left_snow = step(snow_threshold, left * snow_coverage);
    float right_snow = step(snow_threshold, right * snow_coverage);
    float up_snow = step(snow_threshold, up * snow_coverage);
    float down_snow = step(snow_threshold, down * snow_coverage);
    float current_snow = step(snow_threshold, noise_value * snow_coverage);

    // Create outline where snow meets non-snow
    float outline_mask = 0.0;
    if (current_snow > 0.5) {
        // We're in snow, check if any neighbor is not snow
        if (left_snow < 0.5 || right_snow < 0.5 || up_snow < 0.5 || down_snow < 0.5) {
            outline_mask = 1.0;
        }
    }

    // Mix colors
    vec3 final_color = mix(ground_color, snow_color, snow_mask);
    final_color = mix(final_color, outline_color, outline_mask);

    ALBEDO = final_color;

    // Apply normal map
    NORMAL_MAP = texture(normal_map, UV).rgb;
}