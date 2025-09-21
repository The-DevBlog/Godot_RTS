shader_type spatial;
render_mode unshaded, depth_draw_opaque, depth_prepass_alpha;

// Inspired by https://godotshaders.com/shader/3d-pixel-art-outline-highlight-post-processing-shader/

uniform sampler2D DEPTH_TEXTURE : hint_depth_texture, filter_linear_mipmap;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
uniform sampler2D NORMAL_TEXTURE : hint_normal_roughness_texture, filter_nearest;

uniform vec3 shadow_color : source_color = vec3(0.0);
uniform float shadow_thickness = 15.0;
uniform float base_thickness : hint_range(0.1, 5.0) = 2.5; // Base outline thickness multiplier
uniform float depth_scale : hint_range(0.1, 10.0) = 5.0; // Controls how much depth affects outline thickness
uniform float min_thickness : hint_range(0.1, 2.0) = 0.22; // Minimum outline thickness at far distances
uniform float max_thickness : hint_range(0.5, 10.0) = 1.5; // Maximum outline thickness at close distances

vec2 getDepth(vec2 screen_uv, sampler2D depth_texture, mat4 inv_projection_matrix){
	float raw_depth = texture(depth_texture, screen_uv)[0];
	vec3 normalized_device_coordinates = vec3(screen_uv * 2.0 - 1.0, raw_depth);
    vec4 view_space = inv_projection_matrix * vec4(normalized_device_coordinates, 1.0);
	view_space.xyz /= view_space.w;
	return vec2(-view_space.z, raw_depth);
}

void fragment() {
	vec2 depth_data = getDepth(SCREEN_UV, DEPTH_TEXTURE, INV_PROJECTION_MATRIX);
	float depth = depth_data.x;

	// Calculate depth-based scaling factor
	// Closer objects (smaller depth) get thicker outlines
	float distance_factor = depth_scale / max(depth, 0.1);
	// Clamp between min and max thickness
	distance_factor = clamp(distance_factor, min_thickness, max_thickness);

	// Base pixel offset with base thickness multiplier
	vec2 e = vec2(1.0/VIEWPORT_SIZE.xy) * base_thickness * distance_factor;

	float depth_diff = 0.0;
	float neg_depth_diff = .5;

	vec3 color = texture(SCREEN_TEXTURE, SCREEN_UV).rgb;
	vec3 c = vec3(0.0);

	vec2 min_depth_data = depth_data;
	float min_depth = 9999999.9;

	vec3 normal = texture(NORMAL_TEXTURE, SCREEN_UV).rgb * 2.0 - 1.0;

	for (float x = -shadow_thickness; x <= shadow_thickness;x += 1.0){
		for (float y = -shadow_thickness; y <= shadow_thickness; y += 1.0){
			if ((x == 0.0 && y == 0.0) || (shadow_thickness*shadow_thickness < (x*x + y*y))){
				continue;
			}

			vec2 du_data = getDepth(SCREEN_UV+1.0*vec2(x, y)*e, DEPTH_TEXTURE, INV_PROJECTION_MATRIX);
			vec2 dd_data = getDepth(SCREEN_UV+0.5*vec2(x, y)*e, DEPTH_TEXTURE, INV_PROJECTION_MATRIX);

			float du = du_data.x;
			float dd = dd_data.x;

			float dd_diff = clamp(abs((depth - dd) - (dd - du)), 0.0, 1.0);

			float val = clamp(abs(depth - du), 0., 1.)/(x*x + y*y)*dd_diff*dd_diff*5000.0;

			val = clamp(val, 0.0, 1.0);

			depth_diff += val;

			if (du < min_depth){
				min_depth = du;
				min_depth_data = du_data;
				c = texture(SCREEN_TEXTURE, SCREEN_UV+vec2(x, y)*e).rgb;

				c *= clamp(0.5+ 0.5*dot(normalize(vec2(x, y)), (vec2(0.0, 1.0))), 0.0, 1.0);
			}

			vec3 nu = texture(NORMAL_TEXTURE, SCREEN_UV+vec2(x, y)*e).rgb * 2.0 - 1.0;

			depth_diff += (1.0-abs(dot(nu, normal)))/max(min(dd, depth), 2.0);
		}
	}

	depth_diff = smoothstep(0.2, 0.3, depth_diff);

	vec3 final = c*shadow_color;
	ALBEDO = final;

	float alpha_mask = depth_diff;
	DEPTH = min_depth_data.y*alpha_mask + depth_data.y*(1.0-alpha_mask);
	ALPHA = clamp((alpha_mask) * 5., 0., 1.);
}