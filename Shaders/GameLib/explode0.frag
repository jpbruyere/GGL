#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

uniform float time;
uniform vec2 mouse;
uniform vec2 resolution;

float rand(vec2 v){
	return fract(sin(dot(v.xy,vec2(33.9898,78.233))) * 43758.5453);
}
void main( void ) {

	vec2 p = 1.0-texCoord.xy*2.0 ;
	float c = 0.0;
	float t = fract(time * 2.0);
	for(int i = 0; i < 30; i++){
		float f = float(i);
		c += max(smoothstep(0.0, 0.5, t)/3.5 - length((vec2(rand(vec2(f, 1.0)), rand(vec2(-1.0, f))) * 2.0 - 1.0) * t - p), 0.0);
	}
	out_frag_color = vec4(vec3(smoothstep(0.1, 0.6, c), smoothstep(0.4, 1.0, c), smoothstep(0.8, 1.0, c)), 1.0 );

}
