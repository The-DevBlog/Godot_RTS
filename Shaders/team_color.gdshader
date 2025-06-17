shader_type spatial;

uniform sampler2D albedo_texture : source_color;
uniform sampler2D team_mask : source_color;
uniform vec4 team_color : source_color;

void fragment() {
    vec4 base = texture(albedo_texture, UV);
    float mask = texture(team_mask, UV).r;

    ALBEDO = mix(base.rgb, team_color.rgb, mask);
}
