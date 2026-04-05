shader_type canvas_item;

uniform float h : hint_range(-1.0, 1.0) = 0.0;
uniform vec4 charcoal_color : source_color = vec4(0.02, 0.02, 0.02, 1.0);
uniform vec4 blood_shadow_color : source_color = vec4(0.16, 0.01, 0.01, 1.0);
uniform vec4 blood_mid_color : source_color = vec4(0.33, 0.03, 0.03, 1.0);
uniform vec4 blood_highlight_color : source_color = vec4(0.52, 0.07, 0.06, 1.0);
uniform float blend_strength : hint_range(0.0, 1.0) = 0.92;
uniform float tile_scale : hint_range(0.5, 24.0) = 6.0;
uniform float stain_scale : hint_range(0.5, 12.0) = 2.8;
uniform float streak_strength : hint_range(0.0, 2.0) = 0.7;
uniform float mist_strength : hint_range(0.0, 2.0) = 0.4;
uniform float crust_strength : hint_range(0.0, 2.0) = 0.85;
uniform float edge_hardness : hint_range(0.0, 1.0) = 0.58;
uniform float contrast : hint_range(0.5, 3.0) = 1.55;
uniform float normal_strength : hint_range(0.0, 2.0) = 0.55;
uniform float roughness_strength : hint_range(0.0, 1.0) = 0.82;
uniform vec2 fake_light_direction = vec2(-0.45, -0.85);
uniform bool use_detail_texture = false;
uniform sampler2D detail_texture : repeat_enable, filter_linear_mipmap;
uniform bool use_roughness_texture = false;
uniform sampler2D roughness_texture : repeat_enable, filter_linear_mipmap;
uniform bool use_normal_texture = false;
uniform sampler2D normal_texture : repeat_enable, filter_linear_mipmap;

float hash12(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float periodic_noise(vec2 p, vec2 period) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);

	vec2 a = mod(i, period);
	vec2 b = mod(i + vec2(1.0, 0.0), period);
	vec2 c = mod(i + vec2(0.0, 1.0), period);
	vec2 d = mod(i + vec2(1.0, 1.0), period);

	float va = hash12(a);
	float vb = hash12(b);
	float vc = hash12(c);
	float vd = hash12(d);

	return mix(mix(va, vb, u.x), mix(vc, vd, u.x), u.y);
}

float periodic_fbm(vec2 p, vec2 period) {
	float value = 0.0;
	float amplitude = 0.5;
	vec2 current_p = p;
	vec2 current_period = period;

	for (int index = 0; index < 4; index++) {
		value += periodic_noise(current_p, current_period) * amplitude;
		current_p = current_p * 2.0 + vec2(0.37, 0.91);
		current_period *= 2.0;
		amplitude *= 0.5;
	}

	return value;
}

vec3 rgb_to_hsv(vec3 color) {
	vec4 k = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = mix(vec4(color.bg, k.wz), vec4(color.gb, k.xy), step(color.b, color.g));
	vec4 q = mix(vec4(p.xyw, color.r), vec4(color.r, p.yzx), step(p.x, color.r));
	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv_to_rgb(vec3 color) {
	vec3 p = abs(fract(color.xxx + vec3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
	return color.z * mix(vec3(1.0), clamp(p - 1.0, 0.0, 1.0), color.y);
}

vec3 apply_hue_shift(vec3 color, float hue_shift) {
	vec3 hsv = rgb_to_hsv(color);
	hsv.x = fract(hsv.x + hue_shift);
	return hsv_to_rgb(hsv);
}

float blood_height(vec2 uv) {
	vec2 tiled_uv = uv * tile_scale;
	float broad_stains = periodic_fbm(tiled_uv * 0.8, vec2(8.0));
	float dense_stains = periodic_fbm(tiled_uv * stain_scale, vec2(8.0));
	float streaks = periodic_fbm(vec2(tiled_uv.x * 0.55, tiled_uv.y * 2.7 + broad_stains * 2.5), vec2(8.0));
	float mist = periodic_fbm(tiled_uv * 0.45 + vec2(6.0, 3.0), vec2(8.0));
	float crust = periodic_fbm(tiled_uv * 3.8 + vec2(dense_stains * 2.0), vec2(8.0));

	float stain_mask = smoothstep(0.26, 0.82, dense_stains * 0.7 + broad_stains * 0.6 + streaks * streak_strength * 0.35 + mist * mist_strength * 0.22);
	float crust_mask = smoothstep(0.45, 0.92, crust) * crust_strength;
	float height = stain_mask + crust_mask * 0.35 + mist * mist_strength * 0.12;
	return clamp(height, 0.0, 1.0);
}

void fragment() {
	vec4 source = texture(TEXTURE, UV);
	if (source.a <= 0.001) {
		COLOR = source;
		return;
	}

	vec2 tiled_uv = fract(UV * tile_scale);
	float height = blood_height(tiled_uv);
	float height_dx = blood_height(fract((UV + vec2(TEXTURE_PIXEL_SIZE.x, 0.0)) * tile_scale));
	float height_dy = blood_height(fract((UV + vec2(0.0, TEXTURE_PIXEL_SIZE.y)) * tile_scale));
	vec3 procedural_normal = normalize(vec3((height - height_dx) * normal_strength, (height - height_dy) * normal_strength, 1.0));

	if (use_normal_texture) {
		vec3 sampled_normal = texture(normal_texture, tiled_uv).xyz * 2.0 - 1.0;
		procedural_normal = normalize(mix(procedural_normal, vec3(sampled_normal.xy * normal_strength, max(sampled_normal.z, 0.001)), 0.65));
	}

	float roughness = mix(0.92, 0.42, height * roughness_strength);
	if (use_roughness_texture) {
		roughness = clamp(mix(roughness, texture(roughness_texture, tiled_uv).r, 0.7), 0.04, 1.0);
	}

	float detail = 0.0;
	if (use_detail_texture) {
		detail = texture(detail_texture, tiled_uv).r;
	}

	float mask = pow(clamp(height + detail * 0.18, 0.0, 1.0), mix(2.2, 0.85, edge_hardness));
	mask = clamp((mask - 0.5) * contrast + 0.5, 0.0, 1.0);

	vec3 blood_color = mix(charcoal_color.rgb, blood_shadow_color.rgb, smoothstep(0.12, 0.48, height));
	blood_color = mix(blood_color, blood_mid_color.rgb, smoothstep(0.38, 0.78, height));
	blood_color = mix(blood_color, blood_highlight_color.rgb, smoothstep(0.72, 1.0, height));

	vec3 light_direction = normalize(vec3(fake_light_direction, 0.9));
	float ndotl = clamp(dot(procedural_normal, light_direction), 0.0, 1.0);
	float diffuse = mix(0.72, 1.18, ndotl);
	float specular = pow(ndotl, mix(10.0, 42.0, 1.0 - roughness)) * (1.0 - roughness) * 0.12;
	vec3 lit_blood = blood_color * diffuse + vec3(specular);

	vec3 final_rgb = mix(source.rgb, lit_blood, mask * blend_strength);
	final_rgb = mix(final_rgb, final_rgb * vec3(0.65, 0.4, 0.4), mask * 0.14);
	final_rgb = apply_hue_shift(final_rgb, h);

	COLOR = vec4(final_rgb, source.a);
}