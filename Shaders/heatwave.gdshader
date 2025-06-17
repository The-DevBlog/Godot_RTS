shader_type spatial;
render_mode unshaded, blend_mix, depth_prepass_alpha, cull_disabled;

/// A small tileable grayscale noise texture.
uniform sampler2D noise_texture : source_color;
/// The rendered screen behind this mesh.
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

/// Waviness strength (0.0–1.0)
uniform float strength : hint_range(0.0, 1.0) = 0.1;
/// Noise zoom level
uniform float frequency : hint_range(1.0, 100.0) = 30.0;
/// Scroll speed of the noise
uniform vec2 speed = vec2(2.0, 1.0);

void vertex() {
    float n   = texture(noise_texture, UV * frequency + speed * TIME).r;
    float off = (n - 0.5) * strength;
    VERTEX   += NORMAL * off;
}

void fragment() {
    // build a noisy UV for refraction
    vec2 noise_uv   = SCREEN_UV * frequency + speed * TIME;
    float n         = texture(noise_texture, noise_uv).r;
    vec2 ref_off    = vec2((n - 0.5) * strength);
    vec2 refract_uv = SCREEN_UV + ref_off;

    // sample the screen buffer
    vec4 scene      = texture(SCREEN_TEXTURE, refract_uv);
    ALBEDO = scene.rgb;
    ALPHA  = scene.a;
}
