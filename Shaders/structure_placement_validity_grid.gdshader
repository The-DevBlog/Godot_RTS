// shader_type spatial;
// render_mode unshaded, cull_disabled;

// // full extents of your plane (so plane_size = grid_radius * 2)
// uniform vec2  plane_size     = vec2(20.0, 20.0);
// // how far out (world units) your grid reaches from center
// uniform float grid_radius    = 10.5;
// // size of each cell (world units)
// uniform vec2  grid_size      = vec2(1.0, 1.0);
// // half-width of your grid lines (world units)
// uniform float line_width     = 0.02;
// // how wide the fade region is near the circle edge (world units)
// uniform float edge_fade      = 5.5;
// // grid color & alpha

// uniform vec4  grid_color     = vec4(1.0,1.0,1.0,1.0);

// instance uniform bool valid_placement = true;

// // hard-coded defaults in-shader, no color-picker:
// uniform vec4 valid_color   = vec4(0.8, 0.8, 0.8, 0.8);
// uniform vec4 invalid_color = vec4(1.0, 0.0, 0.0, 1.8);

// void fragment() {
//     vec2 pos = (UV - 0.5) * plane_size;
//     float dist = length(pos);
//     float fade = 1.0 - smoothstep(grid_radius - edge_fade, grid_radius, dist);
//     if (fade <= 0.0) discard;

//     vec2 uvg = pos / grid_size;
//     vec2 f   = fract(uvg);
//     float dx = min(f.x, 1.0 - f.x) * grid_size.x;
//     float dy = min(f.y, 1.0 - f.y) * grid_size.y;
//     float d  = min(dx, dy);

//     if (d < line_width) {
//         vec4 col = valid_placement ? valid_color : invalid_color;
//         ALBEDO = col.rgb;
//         ALPHA  = col.a * fade;
//     } else {
//         discard;
//     }
// }


shader_type spatial;
render_mode unshaded, cull_disabled;

// 1) Varying to carry world‐space XZ from vertex→fragment
varying vec2 world_pos;


void vertex() {
    // MODEL_MATRIX takes your VERTEX (local coords) into world‐space
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xz;
}

uniform float grid_radius      = 10.5;
uniform vec2  grid_size        = vec2(1.0, 1.0);
uniform float cell_margin      = 0.1;
uniform float cell_corner_rad  = 0.1;
uniform float edge_fade        = 15.0;

instance uniform bool valid_placement = true;
uniform vec4  valid_color   = vec4(0.8, 0.8, 0.8, 0.8);
uniform vec4  invalid_color = vec4(1.0, 0.0, 0.0, 1.0);

void fragment() {
    // get raw world‐pos
    vec2 wp = world_pos;

    // grab the mesh’s own world‐translation (its origin)
    vec2 origin = MODEL_MATRIX[3].xz;

    // now form a *local* pos relative to your placeholder
    vec2 pos = wp - origin;

    float dist = length(pos);
    float fade = 1.0 - smoothstep(grid_radius - edge_fade, grid_radius, dist);
    if (fade <= 0.0) discard;

    // cell‐local as before
    vec2 halfCell = grid_size * 0.5;
    vec2 cellUV   = fract((pos + halfCell) / grid_size);
    vec2 cellPos  = cellUV * grid_size - halfCell;

    // margin & rounded‐rect SDF
    vec2 innerHalf = halfCell - vec2(cell_margin);
    vec2 q = abs(cellPos) - (innerHalf - vec2(cell_corner_rad));
    vec2 qc = max(q, vec2(0.0));
    float tileDist = length(qc) - cell_corner_rad;

    if (tileDist < 0.0) {
        vec4 col = valid_placement ? valid_color : invalid_color;
        ALBEDO = col.rgb;
        ALPHA  = col.a * fade;
    } else {
        discard;
    }
}
