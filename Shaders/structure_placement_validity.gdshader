shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_always, depth_test_disabled;

// true = green border; false = red
uniform bool  valid_placement = true;
// sizes of your plane in world units (X and Z)
uniform vec2  plane_size      = vec2(1.0, 1.0);
// desired border thickness in world units
uniform float thickness = 0.2;

void fragment() {
    // UV runs [0→1] across the plane
    vec2 uv = UV;

    // compute distance in world units from each edge:
    float dx = min(uv.x * plane_size.x, (1.0 - uv.x) * plane_size.x);
    float dy = min(uv.y * plane_size.y, (1.0 - uv.y) * plane_size.y);
    float dist_to_edge = min(dx, dy);

    // only draw where we're within 'thickness_world' of any edge:
    if (dist_to_edge > thickness) {
        discard;
    }

    // pick color by valid_placement
    if (valid_placement) {
        ALBEDO = vec3(0.0, 1.0, 0.0);
        ALPHA  = 1.0;
    } else {
        ALBEDO = vec3(1.0, 0.0, 0.0);
        ALPHA  = 1.0;
    }
}
