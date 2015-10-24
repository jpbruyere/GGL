#version 130
precision highp float;

uniform sampler2D tex;

in vec2 texCoord;
in vec3 normal;
in vec3 v;
out vec4 out_frag_color;

void main(void)
{
	vec3 L = normalize(vec3(0,0,200) - v);   
	vec4 t = texture( tex, texCoord);
	out_frag_color = t * max(dot(normal,L), 0.0);
}

