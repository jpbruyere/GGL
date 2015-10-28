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


vec2 EncodeFloatRGBA( float v ) {
  vec2 enc = vec2(1.0, 255.0) * v;
  enc = fract(enc);
  enc -= enc.yy * vec2(1.0/255.0,0.0);
  return enc;
}
//float DecodeFloatRGBA( float4 rgba ) {
//  return dot( rgba, float4(1.0, 1/255.0, 1/65025.0, 1/160581375.0) );
//}

void main(void)
{
	
	vec3 l = normalize(lpos-v);
	float nl = clamp(max(dot(n,l), 0.0),0.7,1.0);

	vec4 splat = texture (splatTex, splatTexCoord);

	vec3 t1 = texture( tex, vec3(texCoord, splat.r * 255.0)).rgb;
	vec3 t2 = texture( tex, vec3(texCoord, splat.g * 255.0)).rgb;

	vec3 c = mix (t1, t2, splat.b);

	out_frag_color = vec4(c * nl, 1.0);
//	ivec2 i = floatBitsToInt(vertex.xy);
//	vec4 res = intBitsToFloat(ivec4(i.x , i.y , 1, 1));
//	int x = floatBitsToInt(vertex.x);
//	int y = floatBitsToInt(vertex.y);
//	vec4 res = vec4(intBitsToFloat(x), intBitsToFloat(y), intBitsToFloat(x*256), 1.0);

	//out_frag_selection = vertex;
	//out_frag_selection = vec4(vertex.x, fract(vertex.x * 255.0), vertex.y, 1.0);
	vec2 resx = EncodeFloatRGBA(vertex.x);
	vec2 resy = EncodeFloatRGBA(vertex.y);
	out_frag_selection = vec4(resx, resy);
}

