shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_always, depth_test_disabled;

// full extents of your plane (so plane_size = grid_radius * 2)
uniform vec2  plane_size     = vec2(10.0, 10.0);
// how far out (world units) your grid reaches from center
uniform float grid_radius    = 5.0;
// size of each cell (world units)
uniform vec2  grid_size      = vec2(1.0, 1.0);
// half-width of your grid lines (world units)
uniform float line_width     = 0.05;
// how wide the fade region is near the circle edge (world units)
uniform float edge_fade      = 1.0;
// grid color & alpha
uniform vec4  grid_color     = vec4(1.0,1.0,1.0,1.0);

void fragment() {
    // 1) Map UV into world-space pos (centered)
    vec2 pos = (UV - 0.5) * plane_size;
    float dist = length(pos);  // distance from center

    // 2) Compute a fade factor: 1.0 well inside, 0.0 at/over the radius
    float fade = 1.0 - smoothstep(grid_radius - edge_fade, grid_radius, dist);
    // if fully faded out, skip
    if (fade <= 0.0)
        discard;

    // 3) Figure out if we're on a grid line
    vec2 uvg = pos / grid_size;
    vec2 f   = fract(uvg);
    // world-space distance to nearest cell edge
    float dx = min(f.x, 1.0 - f.x) * grid_size.x;
    float dy = min(f.y, 1.0 - f.y) * grid_size.y;
    float d  = min(dx, dy);

    if (d < line_width) {
        // 4) Draw the line, modulating alpha by the fade
        ALBEDO = grid_color.rgb;
        ALPHA  = grid_color.a * fade;
    } else {
        discard;
    }
}
