#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

uniform float time;
uniform vec2 mouse;
uniform vec2 resolution;
varying vec2 surfacePosition;

#define PI 3.14159265358979
#define N 5
void main( void ) {
	float s = sin(time);
	float t = time * 1.75 + .75 * s;
	float size = 1.;
	float dist = 0.0;
	float ang = t;
	vec2 pos = vec2(0.0,0.0);
	float color = 0.;
	
	for(int i=0; i<N; i++){
		float r = 0.3;
		ang += PI / (float(N)*0.5);
		pos = vec2(cos(ang),sin(ang))*r*cos(t+ang/.5);
		dist += 1.0 / distance(pos,texCoord*7.0);
		float c = (0.06 + .02 * s);
		color = c*dist;
	}
	out_frag_color = vec4(color * .25, color * .5, color, 1.0);
}
