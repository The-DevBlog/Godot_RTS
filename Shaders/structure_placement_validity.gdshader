shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_always, depth_test_disabled;

uniform bool  valid_placement = true;
uniform vec2  plane_size     = vec2(1.0, 1.0);

void fragment() {
	float thickness      = 0.065;
	float corner_radius  = 0.11;
	float outline_thickness = 0.008;

    // 1) map UV→world‐space coords centered at (0,0)
    vec2 pos  = (UV - 0.5) * plane_size;
    vec2 half = plane_size * 0.5;

    // 2) outer rounded‐rect SDF
    vec2 q = abs(pos) - (half - vec2(corner_radius));
    vec2 q_clamped = max(q, vec2(0.0));
    float outer_dist = length(q_clamped) - corner_radius;

    // 3) inner rounded‐rect SDF
    vec2 inner_half = half - vec2(thickness);
    vec2 iq = abs(pos) - (inner_half - vec2(corner_radius));
    vec2 iq_clamped = max(iq, vec2(0.0));
    float inner_dist = length(iq_clamped) - corner_radius;

    // 4) are we in the main ring region at all?
    bool in_ring = (outer_dist < 0.0 && inner_dist > 0.0);
    if (!in_ring) {
        discard;
    }

    // 5) outline region = within outline_thickness of *either* edge
    bool on_edge = (outer_dist > -outline_thickness)    // near outer boundary
                || (inner_dist <  outline_thickness);   // near inner boundary

    if (on_edge) {
        ALBEDO = vec3(0.0);   // black
    } else {
        // fill the “body” of the ring green or red
        ALBEDO = valid_placement
            ? vec3(0.0, 1.0, 0.0)
            : vec3(1.0, 0.0, 0.0);
    }
    ALPHA = 1.0;
}
