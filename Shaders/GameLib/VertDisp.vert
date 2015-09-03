#version 330

precision highp float;

uniform mat4 Projection;
uniform mat4 ModelView;
uniform mat4 Model;
uniform mat4 Normal;

uniform vec2 mapSize;

uniform sampler2D heightMap;

in vec3 in_position;
in vec2 in_tex;

out vec2 texCoord;


void main(void)
{
	texCoord = in_tex;
	vec4 dv = texture2D( heightMap, texCoord / mapSize);
	vec4 pos = vec4(in_position.x, in_position.y, in_position.z + dv.g*5.0, 1);
	gl_Position = Projection * ModelView * Model * pos;
}
