#version 330
#extension GL_ARB_shader_texture_image_samples : require
#extension GL_ARB_sample_shading : require

precision highp float;

uniform sampler2DMS tex;

in vec2 texCoord;
out vec4 fragColor;

//const int samples = 8;
//float div= 1.0/samples;
 
void main()
{
	ivec2 texcoord = ivec2(textureSize(tex) * texCoord); // used to fetch msaa texel location
	vec4 c = texelFetch (tex, texcoord, gl_SampleID);
	fragColor = c;
	gl_FragDepth = c.x;
}
