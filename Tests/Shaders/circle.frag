#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

uniform float radius;
uniform vec4 color;

float border = 0.002;
vec4 bkg_color = vec4(0.0, 0.0, 0.0, 0.0);
vec2 circle_center = vec2(0.5, 0.5);   

void main ()
{	
	vec2 uv = texCoord - circle_center;

	float dist =  sqrt(dot(uv, uv));

	float t = 1.0 + smoothstep(radius, radius + border, dist) 
	            - smoothstep(radius - border, radius, dist);

	gl_FragColor = mix(color, bkg_color, t);
}