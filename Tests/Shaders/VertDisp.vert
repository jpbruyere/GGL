#version 330

precision highp float;

uniform mat4 Projection;
uniform mat4 ModelView;
uniform mat4 Model;
uniform mat4 Normal;

uniform vec2 mapSize;
uniform float heightScale;

uniform sampler2D heightMap;

in vec3 in_position;
in vec2 in_tex;


out vec2 texCoord;
out vec3 v;
out vec3 normal;
flat out vec4 vertex;

void main(void)
{
	vec2[] offsets = vec2[]
	(
		vec2(0,0),
		vec2(1,0),
		vec2(0,1),
		vec2(1,1)
	);
	vec4[4] pos;

	texCoord = in_tex;

	for(int i = 0; i < 4; i++){
		vec2 xy = in_position.xy + offsets[i];
		float h = texture2D( heightMap, xy / mapSize).g * heightScale;
		pos[i] = vec4(xy.x, xy.y, h, 1.0);
	}
	normal = normalize(cross(pos[0].xyz - pos[1].xyz, pos[0].xyz - pos[2].xyz));
	v = vec3(Model * pos[0]);
	vertex = vec4(pos[0].xy / mapSize, pos[0].z / heightScale, pos[0].w);
	gl_Position = Projection * ModelView * Model * pos[0];
}
