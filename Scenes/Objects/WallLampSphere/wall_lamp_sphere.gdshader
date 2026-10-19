shader_type spatial;
render_mode blend_mix, depth_draw_always, cull_back, diffuse_burley, specular_schlick_ggx;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
uniform float roughness : hint_range(0.0, 1.0) = 0.5;
uniform float blur_amount : hint_range(0.0, 5.0) = 2.5;
uniform vec4 glass_color : source_color = vec4(1.0, 1.0, 1.0, 0.2);

void fragment() {
    // Odczyt koloru z ekranu za obiektem z uwzględnieniem poziomu rozmycia (LOD)
    vec4 screen_color = textureLod(screen_texture, SCREEN_UV, blur_amount);
    
    // Połączenie koloru tła z kolorem szkła
    ALBEDO = mix(screen_color.rgb, glass_color.rgb, glass_color.a);
    ROUGHNESS = roughness;
    METALLIC = 0.0;
    SPECULAR = 0.5;
    ALPHA = 1.0; // Używamy pełnego alpha, bo kolor tła bierzemy bezpośrednio z SCREEN_TEXTURE
}