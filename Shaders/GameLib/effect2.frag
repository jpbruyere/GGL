#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

uniform float time;
uniform vec2 resolution;

vec2 position;

vec3 ball(vec3 colour, float sizec, float xc, float yc){
	return colour * (sizec / distance(position, vec2(xc, yc)));
}

vec3 circle(vec3 colour, float size, float linesize, float xc, float yc){
	float dist = 0.01*distance(2.0*position, vec2(2.0*xc, yc));
	return colour * clamp(-(abs(dist - size)*linesize * 40.0) + 0.5, 0.1, 0.1);
}

vec3 red = vec3(2, 1, 1);
vec3 green = vec3(1, 2, 1);
vec3 blue = vec3(1, 1, 2);
vec3 white = vec3(0, 0, 0);
void main( void ) {

	position = texCoord;
	//position.y = position.y * resolution.y/resolution.x + 0.25;
	
	vec3 color = vec3(0.0);
	color += circle(green, 0.05, 0.6, 0.5, 0.5);
	
	color *= 1.0 - distance(position, vec2(0.5, 0.5));
	color += ball(blue, 0.01, sin(time*4.0) / 12.0 + 0.5, cos(time*4.1) / 12.0 + 0.5);
	color *= ball(blue * 0.9, 0.02, -sin(time*-4.0) / 12.0 + 0.5, -cos(time*-4.1) / 12.0 + 0.5) + 0.5;
	color *= ball(red * 0.9, 0.02, -sin(time*-1.0)*-sin(time*-1.1) / 12.0 + 0.5, cos(time*1.1) / 12.0 + 0.5) + 0.9;
	color *= ball(red * 0.9, 0.02, sin(time*-1.4) / 12.0 + 0.5, cos(time*1.7) / 12.0 + 0.5) + 0.9;
	color *= ball(blue * 0.9, 0.02, sin(time*-2.2) / 12.0 + 0.5, cos(time*1.4) / 12.0 + 0.5) + 0.9;
	color *= ball(red * 0.9, 0.02, sin(time*-2.4) / 12.0 + 0.5, cos(time*1.7) / 12.0 + 0.5) + 0.9;
	color *= ball(green * 0.9, 0.02, sin(time*-2.1) / 12.0 + 0.5, cos(time*2.1) / 12.0 + 0.5) + 0.9;
	color *= ball(red * 0.9, 0.02, sin(time*-2.8) / 12.0 + 0.5, cos(time*1.3) / 12.0 + 0.5) + 0.9;
	out_frag_color = vec4(color, 1.0 );

}