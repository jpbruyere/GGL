#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

//uniform float time;
//uniform vec2 mouse;
//uniform vec2 resolution;

float lengthsq(vec2 p) { return dot(p, p); }

float noise(vec2 p){
	return fract(sin(fract(sin(p.x) * (42.1)) + p.y) * 31.001);
}


float worley(vec2 p) {	
	// Initialise distance to a large value
	float d = 3.0;
	for (int xo = -1; xo <= 1; xo++) {
		for (int yo = -1; yo <= 1; yo++) {
			// Test all surrounding cells to see if distance is smaller.
			vec2 test_cell = floor(p) + vec2(xo, yo);
			// Update distance if smaller.
			vec2 c = test_cell + noise(test_cell);
			d = min(d, 
				lengthsq(p - c) + noise(test_cell) * 0.4
			       );
		}
	}
	return d;
}

void main() {
	float t = 0.9 * worley(texCoord.xy * 6.0);
	out_frag_color = vec4(vec3(t,sqrt(t),t), 1.0);
}
