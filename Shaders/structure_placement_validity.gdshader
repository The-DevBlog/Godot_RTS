shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_always, depth_test_disabled;

// per‐instance toggle
uniform bool  valid_placement = true;
// world‐size of the plane
uniform vec2  plane_size   = vec2(1.0, 1.0);
// width of the ring in world‐units
uniform float thickness    = 0.2;
// radius of the rounded corners, in world‐units
uniform float corner_radius = 0.3;

void fragment() {
    // 1) Compute local pos in world units, centered at (0,0)
    vec2 pos = (UV - 0.5) * plane_size;
    vec2 half = plane_size * 0.5;

    // 2) SDF to outer rounded rect (half-size = half, radius = corner_radius)
    vec2 q = abs(pos) - (half - vec2(corner_radius));
    vec2 q_clamped = max(q, vec2(0.0));
    float outer_dist = length(q_clamped) - corner_radius;

    // 3) SDF to inner rounded rect (half-size = half - vec2(thickness), same radius)
    vec2 inner_half = half - vec2(thickness);
    vec2 iq = abs(pos) - (inner_half - vec2(corner_radius));
    vec2 iq_clamped = max(iq, vec2(0.0));
    float inner_dist = length(iq_clamped) - corner_radius;

    // 4) Border region = inside outer AND outside inner
    if (!(outer_dist < 0.0 && inner_dist > 0.0)) {
        discard;
    }

    // 5) Color it
    ALBEDO = valid_placement
        ? vec3(0.0, 1.0, 0.0)
        : vec3(1.0, 0.0, 0.0);
    ALPHA = 1.0;
}
