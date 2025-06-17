shader_type spatial;

uniform sampler2D albedo_texture : source_color;
uniform sampler2D team_mask : source_color;
uniform vec4 team_color : source_color;
uniform float outline_thickness = 1.0;

void fragment() {
    vec2 texel_size = vec2(1.0) / vec2(textureSize(team_mask, 0));
    vec2 offset = texel_size * outline_thickness;

    float center = texture(team_mask, UV).r;
    float left   = texture(team_mask, UV - vec2(offset.x, 0.0)).r;
    float right  = texture(team_mask, UV + vec2(offset.x, 0.0)).r;
    float up     = texture(team_mask, UV + vec2(0.0, offset.y)).r;
    float down   = texture(team_mask, UV - vec2(0.0, offset.y)).r;

    bool is_edge = center > 0.1 && (left < 0.1 || right < 0.1 || up < 0.1 || down < 0.1);

    vec4 base = texture(albedo_texture, UV);
    vec3 final_color = mix(base.rgb, team_color.rgb, center);

    if (is_edge) {
        final_color = vec3(0.0); // black outline
    }

    ALBEDO = final_color;
}
