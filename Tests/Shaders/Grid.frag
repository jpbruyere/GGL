#version 330
precision highp float;

uniform sampler2DArray tex;
uniform sampler2D splatTex;

in vec2 texCoord;
in vec2 splatTexCoord;
//flat in float layer;
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

	vec4 splat = texture (splatTex, splatTexCoord);

	vec3 t1 = texture( tex, vec3(texCoord, splat.r * 255.0)).rgb;
	vec3 t2 = texture( tex, vec3(texCoord, splat.g * 255.0)).rgb;

	vec3 c = mix (t1, t2, splat.b);

	out_frag_color = vec4(c * nl, 1.0);
	out_frag_selection = vertex;
}

