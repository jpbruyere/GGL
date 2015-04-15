// By Michal Cwiek
// cwiek.michal at g mail dot com

#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

uniform float time;
uniform vec2 mouse;
uniform vec2 resolution;

const float AMPLITUDE_1 = 0.3;
const float AMPLITUDE_2 = 0.5;
const float AMPLITUDE_3 = 0.6;

const float STAR_VISIBILITY_FACTOR = 0.01;

const vec3 wave1_1 = vec3(0.02, 0.03, 0.13);
const vec3 wave1_2 = vec3(0.03, 0.06, 0.23);
const vec3 wave1_3 = vec3(0.04, 0.08, 0.26);

const vec3 wave3_1 = vec3(0.01, 0.01, 0.3);

const vec3 wave4_1 = vec3(0.02, 0.05, 0.03);
const vec3 wave4_2 = vec3(0.02, 0.05, 0.03);
const vec3 wave4_3 = vec3(0.02, 0.05, 0.03);
	
const float WAVE_OFFSET_SMALL  = 5.0;
const float WAVE_OFFSET_MEDIUM = 15.0;

// Default noise
float rand(vec2 co) {
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}



void main( void ) 
{
	vec2 p = ( texCoord.yx ) * 2.0 - 1.0;
	//p.y += (mouse.x / 5.);
	
	vec3 c = vec3( 0.0 );
	
	float waveShineFactor = mix( 0.10, 0.4, 0.3 * sin(time) + 0.5);
	float starShineFactor = mix( 0.10, 0.4, 1.5 * sin(atan(time * 0.2)) + 0.9);
	
	c += wave1_1 * (  waveShineFactor *        abs( 1.0 / sin( p.x         + sin( p.y + time )  * AMPLITUDE_1 ) ));
	c += wave1_2 * ( (waveShineFactor * 0.4) * abs( 1.0 / sin((p.x + 44.0) + sin( p.y + time )  * AMPLITUDE_1 - 0.01 ) ));
	c += wave1_3 * ( (waveShineFactor * 0.1) * abs( 1.0 / sin((p.x + 0.07) + sin( p.y + time )  * AMPLITUDE_1 - 0.02 ) ));
	
	c += vec3(0.05, 0.05, 0.15) * (  waveShineFactor        * abs( 1.0 / sin( p.x + 0.04  + sin( p.y + time + WAVE_OFFSET_SMALL )    * AMPLITUDE_2 ) ));

	
	c += wave3_1 * (  waveShineFactor        * abs(  .8 / sin( p.x         + sin( p.y + time         + WAVE_OFFSET_MEDIUM )      * AMPLITUDE_3 ) ));
	
	c += wave4_1 * (  waveShineFactor        * abs( 1.0 / sin( p.x         + sin( p.y + sin(time)    + WAVE_OFFSET_MEDIUM )      * AMPLITUDE_3 ) ));
	c += wave4_2 * (  waveShineFactor        * abs( 1.0 / sin( p.x         + sin( p.y + sin(time/2.) + WAVE_OFFSET_MEDIUM )      * AMPLITUDE_3 ) ));
	c += wave4_3 * (  waveShineFactor        * abs( 1.0 / sin( p.x         + sin( p.y + sin(time/4.) + WAVE_OFFSET_MEDIUM )      * AMPLITUDE_3 ) ));
	

	
	// Blinking shiet
	float star = 0.0;
	if (rand(texCoord.xy ) > 0.99 && ( (texCoord.y + sin(p.y + time) * 150.) < 300.  && (texCoord.y*resolution.y + cos(p.y + time) * 20.) > 50.)) {
		float r = rand(gl_FragCoord.xy);
		star = r * (1.625 * sin(time * (r * 5.0) + 2.0 * r) + 0.95);
	} 
	
	vec4 color = vec4(c,1.0);
	
	// Do not apply "black stars"
	star = star * starShineFactor * (abs( .5 / sin(p.x + sin( p.y + time* 1.5 ))));
	if (star >= STAR_VISIBILITY_FACTOR) 
		color += pow(star,8.);

	
	out_frag_color = color;

}
