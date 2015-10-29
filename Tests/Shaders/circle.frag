#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

float border = 0.01;
float circle_radius = 0.002;
vec4 circle_color = vec4(1.0, 1.0, 1.0, 1.0);
vec4 bkg_color = vec4(0.0, 0.0, 0.0, 0.0);
vec2 circle_center = vec2(0.5, 0.5);   

void main ()
{
  vec2 uv = texCoord - circle_center;

  float dist =  sqrt(dot(uv, uv));

  float t = 1.0 + smoothstep(circle_radius, circle_radius+border, dist) 
                - smoothstep(circle_radius-border, circle_radius, dist);

  gl_FragColor = mix(circle_color, bkg_color, t);
}