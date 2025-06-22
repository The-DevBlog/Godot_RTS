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
