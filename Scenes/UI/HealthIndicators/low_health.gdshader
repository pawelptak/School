shader_type canvas_item;

uniform vec4 vignette_color : source_color = vec4(1.0, 0.0, 0.0, 1.0);
uniform float multiplier : hint_range(0.0, 10.0) = .9;
uniform float softness : hint_range(0.0, 5.0) = .5;
uniform float alpha : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    float value = distance(UV, vec2(0.5));
    float vignette = smoothstep(softness, softness - 0.2, value * multiplier);
    
    COLOR = vec4(vignette_color.rgb, (1.0 - vignette) * alpha);
}