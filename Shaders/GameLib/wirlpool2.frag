#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

uniform float time;
uniform vec2 mouse;
uniform vec2 resolution;
uniform vec4 color;
//uniform sampler2D tex;

void main()
{
	
	vec2 uv = texCoord-0.5;//(gl_FragCoord.xy/resolution.xy)-.5;

	float time = time*0.6 + ((.25+.05*sin(time*.1))/(length(uv.xy)+.07))* 2.2;
	float si = sin(time);
	float co = cos(time);
	mat2 ma = mat2(co, si, -si, co);

	float c = 0.0;
	float v1 = 0.0;
	float v2 = 0.0;
	
	for (int i = 0; i < 10; i++)
	{
		float s = float(i) * .45;
		vec3 p = s * vec3(uv, 0.0);
		p.xy *= ma;
		p += vec3(.22,.3, s-1.5-sin(time*.9)*.1);
		for (int i = 0; i < 8; i++)
		{
			p = abs(p) / dot(p,p) - 0.659;
		}
		v1 += dot(p,p)*.15 * (1.8+sin(length(uv.xy*13.0)+.5-time*.2));
		v2 += dot(p,p)*.015 * (1.5+sin(length(uv.xy*13.5)+2.2-time*.3));
		c = length(p.xy*.5) * .35;
	}
	
	float len = length(uv);
	v1 *= smoothstep(.01, .0, len);
	v2 *= smoothstep(.5, .0, len);
	
	float re = clamp(c, 0.0, 1.0);
	float gr = clamp((v1+c)*.025, 0.0, 1.0);
	float bl = clamp(v2, 0.0, 1.0);
	vec3 col = vec3(re, gr, bl) + smoothstep(0.0015, .0, len) * .9;

	if (col.r < 0.8 && col.g < 0.08 && col.b < 0.08)
		out_frag_color = vec4(0.0,0.0,0.0,0.0);
	else
		out_frag_color = vec4(col, 0.7);
}

