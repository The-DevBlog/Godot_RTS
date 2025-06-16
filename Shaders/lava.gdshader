shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_schlick_ggx;

uniform vec4 ColorParameter : source_color = vec4(1.000000, 1.000000, 1.000000, 1.000000);
uniform float Falloff : hint_range(0, 20) = 5;
uniform sampler2D Noise_Texsture;
uniform float Midtones = 0.40000000596046;
uniform float Highlights : hint_range(0, 0.30000001192093) = 0.05000000074506;
uniform float Multiplier : hint_range(0, 20) = 1;
uniform float Aplha : hint_range(0, 1) = 1;
uniform float Roughness : hint_range(0, 1) = 0.10000000149012;
uniform float Emmision_Strength : hint_range(0, 20) = 1;
uniform float Bump_Strength : hint_range(0, 1) = 0.10000000149012;



void fragment() {
// ColorParameter:17
	vec4 n_out17p0 = ColorParameter;


// FloatParameter:28
	float n_out28p0 = Falloff;


// FloatOp:95
	float n_in95p1 = 0.00100;
	float n_out95p0 = max(n_out28p0, n_in95p1);


// FloatOp:10
	float n_in10p0 = 0.93000;
	float n_out10p0 = pow(n_in10p0, n_out95p0);


// MultiplyAdd:11
	float n_in11p1 = -1.00000;
	float n_in11p2 = 1.00000;
	float n_out11p0 = fma(n_out10p0, n_in11p1, n_in11p2);


// FloatFunc:21
	float n_out21p0 = log(n_out11p0);


// Fresnel:5
	float n_in5p3 = 1.00000;
	float n_out5p0 = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), n_in5p3);


// Input:31
	vec2 n_out31p0 = UV;


// FloatConstant:49
	float n_out49p0 = 0.500000;


// VectorCompose:48
	float n_in48p1 = 0.00000;
	vec2 n_out48p0 = vec2(n_out49p0, n_in48p1);


// VectorOp:38
	vec2 n_out38p0 = n_out31p0 + n_out48p0;


// FloatConstant:41
	float n_out41p0 = 2048.000000;


// FloatOp:40
	float n_in40p0 = 1.00000;
	float n_out40p0 = n_in40p0 / n_out41p0;


// VectorCompose:54
	float n_in54p1 = 0.00000;
	vec2 n_out54p0 = vec2(n_out40p0, n_in54p1);


// VectorOp:51
	vec2 n_out51p0 = n_out38p0 + n_out54p0;


	vec4 n_out34p0;
// Texture2D:34
	n_out34p0 = texture(Noise_Texsture, n_out51p0);


// VectorOp:46
	vec2 n_in46p1 = vec2(0.00000, 0.00000);
	vec2 n_out46p0 = n_out31p0 + n_in46p1;


// VectorOp:52
	vec2 n_out52p0 = n_out46p0 + n_out54p0;


	vec4 n_out35p0;
// Texture2D:35
	n_out35p0 = texture(Noise_Texsture, n_out52p0);


// VectorCompose:50
	float n_in50p0 = 0.00000;
	vec2 n_out50p0 = vec2(n_in50p0, n_out49p0);


// VectorOp:39
	vec2 n_out39p0 = n_out31p0 + n_out50p0;


// VectorOp:53
	vec2 n_out53p0 = n_out39p0 + n_out54p0;


	vec4 n_out36p0;
// Texture2D:36
	n_out36p0 = texture(Noise_Texsture, n_out53p0);


// VectorCompose:9
	vec3 n_out9p0 = vec3(n_out34p0.x, n_out35p0.x, n_out36p0.x);


	vec3 n_out3p0;
// ColorFunc:3
	{
		vec3 c = n_out9p0;
		vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
		vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
		vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
		float d = q.x - min(q.w, q.y);
		float e = 1.0e-10;
		n_out3p0 = vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
	}
	float n_out3p2 = n_out3p0.g;


// Mix:4
	float n_in4p2 = 0.50000;
	float n_out4p0 = mix(n_out5p0, n_out3p2, n_in4p2);


// FloatParameter:26
	float n_out26p0 = Midtones;


// FloatParameter:27
	float n_out27p0 = Highlights;


	float n_out6p0;
// Remap:6
	float n_in6p3 = 0.00000;
	float n_in6p4 = 1.00000;
	{
		float __input_range = n_out27p0 - n_out26p0;
		float __output_range = n_in6p4 - n_in6p3;
		n_out6p0 = n_in6p3 + __output_range * ((n_out4p0 - n_out26p0) / __input_range);
	}


// FloatOp:13
	float n_in13p0 = 1.00000;
	float n_out13p0 = min(n_in13p0, n_out6p0);


// FloatFunc:12
	float n_out12p0 = log(n_out13p0);


// FloatOp:22
	float n_out22p0 = n_out21p0 / n_out12p0;


// FloatOp:15
	float n_out15p0 = n_out22p0 * n_out6p0;


// Compare:23
	float n_in23p1 = 0.00000;
	bool n_out23p0 = n_out15p0 < n_in23p1;


// FloatOp:25
	float n_in25p1 = 0.04000;
	float n_out25p0 = (n_out23p0 ? 1.0 : 0.0) / n_in25p1;


// FloatOp:24
	float n_out24p0 = max(n_out15p0, n_out25p0);


// FloatParameter:30
	float n_out30p0 = Multiplier;


// FloatOp:29
	float n_out29p0 = n_out24p0 * n_out30p0;


// VectorOp:18
	vec3 n_out18p0 = vec3(n_out17p0.xyz) * vec3(n_out29p0);


	float n_out92p0;
// Remap:92
	float n_in92p1 = 0.00000;
	float n_in92p2 = 5.16500;
	float n_in92p3 = 0.00000;
	float n_in92p4 = 1.00000;
	{
		float __input_range = n_in92p2 - n_in92p1;
		float __output_range = n_in92p4 - n_in92p3;
		n_out92p0 = n_in92p3 + __output_range * ((n_out29p0 - n_in92p1) / __input_range);
	}


// FloatParameter:94
	float n_out94p0 = Aplha;


// FloatOp:93
	float n_out93p0 = max(n_out92p0, n_out94p0);


// FloatParameter:91
	float n_out91p0 = Roughness;


// FloatParameter:97
	float n_out97p0 = Emmision_Strength;


// VectorOp:96
	vec3 n_out96p0 = n_out18p0 * vec3(n_out97p0);


	vec4 n_out55p0;
// Texture2D:55
	n_out55p0 = texture(Noise_Texsture, n_out38p0);


	vec4 n_out56p0;
// Texture2D:56
	n_out56p0 = texture(Noise_Texsture, n_out46p0);


	vec4 n_out57p0;
// Texture2D:57
	n_out57p0 = texture(Noise_Texsture, n_out39p0);


// VectorCompose:58
	vec3 n_out58p0 = vec3(n_out55p0.x, n_out56p0.x, n_out57p0.x);


// FloatOp:60
	float n_in60p1 = 0.00000;
	float n_out60p0 = n_out58p0.x + n_in60p1;


// FloatOp:44
	float n_in44p1 = 0.00000;
	float n_out44p0 = n_out9p0.x + n_in44p1;


// FloatOp:62
	float n_out62p0 = min(n_out60p0, n_out44p0);


// FloatOp:61
	float n_out61p0 = n_out44p0 - n_out60p0;


// FloatOp:63
	float n_out63p0 = min(n_out62p0, n_out61p0);


// FloatOp:64
	float n_in64p1 = 0.50000;
	float n_out64p0 = n_out63p0 + n_in64p1;


// FloatConstant:66
	float n_out66p0 = 0.500000;


// FloatParameter:87
	float n_out87p0 = Bump_Strength;


// FloatOp:89
	float n_in89p1 = 0.00100;
	float n_out89p0 = max(n_out87p0, n_in89p1);


// FloatOp:88
	float n_in88p0 = 1.00000;
	float n_out88p0 = n_in88p0 / n_out89p0;


// FloatOp:70
	float n_in70p1 = 100.00000;
	float n_out70p0 = n_out88p0 / n_in70p1;


// FloatOp:69
	float n_out69p0 = n_out66p0 - n_out70p0;


// FloatOp:68
	float n_out68p0 = n_out66p0 + n_out70p0;


	float n_out65p0;
// Remap:65
	float n_in65p3 = 0.00000;
	float n_in65p4 = 1.00000;
	{
		float __input_range = n_out68p0 - n_out69p0;
		float __output_range = n_in65p4 - n_in65p3;
		n_out65p0 = n_in65p3 + __output_range * ((n_out64p0 - n_out69p0) / __input_range);
	}


// VectorCompose:78
	float n_in78p0 = 0.00000;
	vec2 n_out78p0 = vec2(n_in78p0, n_out40p0);


// VectorOp:76
	vec2 n_out76p0 = n_out38p0 - n_out78p0;


	vec4 n_out75p0;
// Texture2D:75
	n_out75p0 = texture(Noise_Texsture, n_out76p0);


// VectorOp:73
	vec2 n_out73p0 = n_out46p0 - n_out78p0;


	vec4 n_out71p0;
// Texture2D:71
	n_out71p0 = texture(Noise_Texsture, n_out73p0);


// VectorOp:74
	vec2 n_out74p0 = n_out39p0 - n_out78p0;


	vec4 n_out72p0;
// Texture2D:72
	n_out72p0 = texture(Noise_Texsture, n_out74p0);


// VectorCompose:77
	vec3 n_out77p0 = vec3(n_out75p0.x, n_out71p0.x, n_out72p0.x);


// FloatOp:79
	float n_in79p1 = 0.00000;
	float n_out79p0 = n_out77p0.x + n_in79p1;


// FloatOp:81
	float n_out81p0 = min(n_out60p0, n_out79p0);


// FloatOp:80
	float n_out80p0 = n_out79p0 - n_out60p0;


// FloatOp:82
	float n_out82p0 = min(n_out81p0, n_out80p0);


// FloatOp:83
	float n_in83p1 = 0.50000;
	float n_out83p0 = n_out82p0 + n_in83p1;


	float n_out85p0;
// Remap:85
	float n_in85p3 = 0.00000;
	float n_in85p4 = 1.00000;
	{
		float __input_range = n_out68p0 - n_out69p0;
		float __output_range = n_in85p4 - n_in85p3;
		n_out85p0 = n_in85p3 + __output_range * ((n_out83p0 - n_out69p0) / __input_range);
	}


// VectorCompose:86
	float n_in86p2 = 1.00000;
	vec3 n_out86p0 = vec3(n_out65p0, n_out85p0, n_in86p2);


// Output:0
	ALBEDO = n_out18p0;
	ALPHA = n_out93p0;
	ROUGHNESS = n_out91p0;
	EMISSION = n_out96p0;
	NORMAL_MAP = n_out86p0;
}
