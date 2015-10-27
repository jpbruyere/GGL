#version 330
precision highp float;

uniform sampler2DArray tex;

in vec2 texCoord;
flat in float layer;
in vec3 n;
in vec3 v;
in vec3 lpos;
in vec4 vertex;

layout(location = 0) out vec4 out_frag_color;
layout(location = 1) out vec4 out_frag_selection;

void main(void)
{
	
	vec3 l = normalize(lpos-v);
	float nl = clamp(max(dot(n,l), 0.0),0.7,1.0);

	vec3 t = texture( tex, vec3(texCoord, layer)).xyz;

	out_frag_color = vec4(t * nl, 1.0);
	out_frag_selection = vertex;
}

