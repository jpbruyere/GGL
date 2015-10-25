#version 330
precision highp float;

uniform sampler2D tex;

in vec2 texCoord;
in vec3 normal;
in vec3 v;
flat in vec4 vertex;

layout(location = 0) out vec4 out_frag_color;
layout(location = 1) out vec4 out_frag_selection;

void main(void)
{
	vec3 L = normalize(vec3(0,0,200) - v);   
	vec3 t = texture( tex, texCoord).xyz;
	out_frag_color = vec4(t * max(dot(normal,L), 0.0), 1.0);
	out_frag_selection = vertex;
}

