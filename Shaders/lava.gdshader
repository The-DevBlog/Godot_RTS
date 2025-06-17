shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_opaque;


/////////////////////////////////////////////////////
// Uniforms — tweak these in the Inspector
/////////////////////////////////////////////////////

// Grayscale noise (repeat enabled)
uniform sampler2D noise_tex : source_color;

// Flow controls
uniform float time_scale     : hint_range(0.0, 5.0) = 1.0;
uniform float noise_scale    : hint_range(0.1, 10.0) = 3.0;
uniform float noise_strength : hint_range(0.0, 1.0) = 0.2;
uniform float uv_scroll_x    : hint_range(-1.0, 1.0) = 0.1;
uniform float uv_scroll_y    : hint_range(-1.0, 1.0) = 0.2;

// Stylization controls
uniform float toon_steps     : hint_range(2.0, 8.0) = 4.0;
uniform float rim_power      : hint_range(0.5, 8.0) = 2.0;
uniform float rim_intensity  : hint_range(0.0, 3.0) = 1.0;
uniform float glow_strength  : hint_range(0.0, 2.0) = 1.2;

/////////////////////////////////////////////////////
// Helper: HSL → RGB conversion
/////////////////////////////////////////////////////
vec3 hsl2rgb(vec3 hsl) {
    vec3 k = vec3(0.0, 4.0, 2.0);
    vec3 p = abs(mod(hsl.x * 6.0 + k, 6.0) - 3.0) - 1.0;
    vec3 c = clamp(p, 0.0, 1.0);
    return hsl.z + hsl.y * (c - 0.5) * (1.0 - abs(2.0 * hsl.z - 1.0));
}

/////////////////////////////////////////////////////
// Main fragment
/////////////////////////////////////////////////////
void fragment() {
    // time as float
    float t = TIME;

    // animate UVs
    vec2 uv = UV + vec2(uv_scroll_x, uv_scroll_y) * t * time_scale;

    // primary noise
    float n = texture(noise_tex, uv * noise_scale).r;

    // crack flicker
    vec2 sw = uv + (n - 0.5) * noise_strength;
    float crack = texture(noise_tex, sw * noise_scale * 1.5 - t * 0.5).r;

    // toon-quantize
    float q = floor(n * toon_steps) / (toon_steps - 1.0);

    // map q → HSL (hue: red→yellow, lum: dark→bright, sat=1)
    float hue = mix(0.02, 0.15, q);
    float lum = mix(0.2,  1.0,  q);
    vec3 base_col = hsl2rgb(vec3(hue, 1.0, lum));

    // rim glow
    float rim = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), rim_power) * rim_intensity;
    vec3 rim_col = vec3(1.0, 0.5, 0.0) * rim;

    // final composite
    vec3 out_col = base_col
                 + crack * glow_strength
                 + rim_col;

    ALBEDO   = out_col;
    EMISSION = out_col;
    ALPHA    = 1.0;
}
