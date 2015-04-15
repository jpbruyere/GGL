#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

uniform float time;
varying vec2 surfacePosition;

#define MAX_ITER 3
void main( void ) {
	vec2 sp = texCoord;//vec2(.4, .7);
	vec2 p = sp*6.0 - vec2(205.0);
	vec2 i = p;
	float c = 0.5;
	
	float inten = 0.001;

	for (int n = 0; n < MAX_ITER; n++) 
	{
		float t = time/1.0* (1.0 - (3.0 / float(n+1)));
		i = p + vec2(cos(t - i.x) + sin(t + i.y), sin(t - i.y) + cos(t + i.x));
		c += 1.0/length(vec2(p.x / (sin(i.x+t)/inten),p.y / (cos(i.y+t)/inten)));
	}
	c /= float(MAX_ITER);
	c = 1.5-sqrt(c);
	out_frag_color = vec4(vec3(c*c*c*c), 999.0) + vec4(0.0, 0.3, 0.5, 4.0);

}
