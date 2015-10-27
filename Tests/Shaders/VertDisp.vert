#version 330

precision highp float;

uniform mat4 Projection;
uniform mat4 ModelView;
uniform mat4 Model;
uniform mat4 Normal;
uniform vec4 lightPos;

uniform vec2 mapSize;
uniform float heightScale;

uniform sampler2D heightMap;

in vec3 in_position;
in vec2 in_tex;


out vec2 texCoord;
out vec2 splatTexCoord;
//flat out float layer;
out vec3 v;
out vec3 n;
out vec3 lpos;

out vec4 vertex;

void main(void)
{
	vec2[] offsets = vec2[]
	(
		vec2(0,0),
		vec2(0,1),
		vec2(1,0),
		vec2(0,-1),
		vec2(-1,0)
	);
	vec3[5] pos;

	texCoord = in_tex;

	vec4 hm0 = texture2D(heightMap, in_position.xy / mapSize);
	pos[0] = vec3(in_position.xy, hm0.g * heightScale);
	//layer = hm0.b * 255.0;
	splatTexCoord = in_position.xy / vec2(512.0,512.0);

	for(int i = 1; i < 5; i++){
		vec2 xy = in_position.xy + offsets[i];
		float h = texture2D( heightMap, xy / mapSize).g * heightScale;
		pos[i] = vec3(xy, h);
	}

	//if (mod(gl_VertexID, 2)==0)
	/*
		n = normalize(
			cross(pos[2] - pos[0], pos[1] - pos[0])
		  + cross(pos[3] - pos[0], pos[2] - pos[0]) 
		  + cross(pos[4] - pos[0], pos[3] - pos[2]) 
		  + cross(pos[1] - pos[0], pos[4] - pos[2]) );
	*/
		n = normalize(
			normalize(cross(pos[2] - pos[0], pos[1] - pos[0]))
		  + normalize(cross(pos[3] - pos[0], pos[2] - pos[0])) 
		  + normalize(cross(pos[4] - pos[0], pos[3] - pos[2])) 
		  + normalize(cross(pos[1] - pos[0], pos[4] - pos[2])) / 4.0);
	//else
	//	n = normalize(cross(pos[3] - pos[1], pos[2] - pos[1]));

	//n = normalize(vec3(Normal * Model * vec4(0.0,0.0,1.0, 0.0)));
	//v = normalize(vec3(Model * pos[0]));
	v = vec3(ModelView * Model * vec4(pos[0], 1));
	lpos = vec3(ModelView * lightPos);
	vertex = vec4((pos[0].xy-vec2(0.5,0.5)) / (mapSize-vec2(1.0,1.0)), pos[0].z / heightScale, 1.0);
	gl_Position = Projection * ModelView * Model * vec4(pos[0], 1.0);
}
