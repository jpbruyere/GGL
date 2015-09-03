//-- Fragment shader for the normal map filter --

#version 330
//precision highp float;


in vec2 texCoord;
out vec4 out_frag_color;

 //The heightmap
uniform sampler2D heightMap;
uniform sampler2D tex;


//Scharr operator constants combined with luminance weights
const vec3 Sobel1 = vec3(0.2990, 0.5870, 0.1140) * vec3(3.0);
const vec3 Sobel2 = vec3(0.2990, 0.5870, 0.1140) * vec3(10.0);

//Luminance weights, scaled by the average of 3x3 normalised kernel weights (including zeros)
const vec3 Lum = vec3(0.2990, 0.5870, 0.1140) * vec3(0.355556);

//Blur level (mip map LOD bias)
const float Blur  = 0.5;

void main()
{
	vec2 Coord[3], d;
	vec4 Texel[6];
	vec3 Normal;

	//3x3 kernel offsets
	d = vec2(dFdx(texCoord.s), dFdy(texCoord.t));
	Coord[0] = texCoord.st - d;
	Coord[1] = texCoord.st;
	Coord[2] = texCoord.st + d;

	//Sobel operator, U direction
	Texel[0] = texture2D(heightMap, vec2(Coord[2].s, Coord[0].t), Blur) - texture2D(heightMap, vec2(Coord[0].s, Coord[0].t), Blur);
	Texel[1] = texture2D(heightMap, vec2(Coord[2].s, Coord[1].t), Blur) - texture2D(heightMap, vec2(Coord[0].s, Coord[1].t), Blur);
	Texel[2] = texture2D(heightMap, vec2(Coord[2].s, Coord[2].t), Blur) - texture2D(heightMap, vec2(Coord[0].s, Coord[2].t), Blur);

	//Sobel operator, V direction
	Texel[3] = texture2D(heightMap, vec2(Coord[0].s, Coord[0].t), Blur) - texture2D(heightMap, vec2(Coord[0].s, Coord[2].t), Blur);
	Texel[4] = texture2D(heightMap, vec2(Coord[1].s, Coord[0].t), Blur) - texture2D(heightMap, vec2(Coord[1].s, Coord[2].t), Blur);
	Texel[5] = texture2D(heightMap, vec2(Coord[2].s, Coord[0].t), Blur) - texture2D(heightMap, vec2(Coord[2].s, Coord[2].t), Blur);

	//Compute luminance from each texel, apply kernel weights, and sum them all
	Normal.s  = dot(Texel[0].rgb, Sobel1);
	Normal.s += dot(Texel[1].rgb, Sobel2);
	Normal.s += dot(Texel[2].rgb, Sobel1);

	Normal.t  = dot(Texel[3].rgb, Sobel1);
	Normal.t += dot(Texel[4].rgb, Sobel2);
	Normal.t += dot(Texel[5].rgb, Sobel1);

	Normal.p = dot(texture2D(heightMap, Coord[1], Blur).rgb, Lum);

	//normal map color
	vec4 nmc = vec4(vec3(0.5) + normalize(Normal) * 0.5, 1.0);

	//normal in tangent space
	vec3 n = normalize(nmc.rgb*2.0 - 1.0);

	vec3 l = vec3(-1,-1,1);
	float cosTheta = clamp( dot( n,l ), 0,1 );

	vec4 kd = texture(tex, texCoord);

	out_frag_color = cosTheta * kd;// vec4(0.8,0.8,0.8,1);

}