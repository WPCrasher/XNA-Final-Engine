/***********************************************************************************************************************************************
Copyright (c) 2008-2011, Laboratorio de Investigación y Desarrollo en Visualización y Computación Gráfica - 
                         Departamento de Ciencias e Ingeniería de la Computación - Universidad Nacional del Sur.
All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

•	Redistributions of source code must retain the above copyright, this list of conditions and the following disclaimer.

•	Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer
    in the documentation and/or other materials provided with the distribution.

•	Neither the name of the Universidad Nacional del Sur nor the names of its contributors may be used to endorse or promote products derived
    from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ''AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Author: Schneider, José Ignacio (jis@cs.uns.edu.ar)
************************************************************************************************************************************************/

#include <ColorCorrection.fxh>
#include <Filmgrain.fxh>
#include <ToneMapping.fxh>

//////////////////////////////////////////////
/////////////// Parameters ///////////////////
//////////////////////////////////////////////

float2 halfPixel;
// frame time delta in seconds.
float timeDelta;
// Adaptation rate
float tau = 1.25f;

// Lens exposure (fraction of light to display)
float lensExposure = 0.1f;

// Bloom scale.
float bloomScale;
bool bloomEnabled;
bool adjustLevelsEnabled;
bool adjustLevelsIndividualChannelsEnabled;

// Lookup Tables
bool colorCorrectOneLutEnabled;
bool colorCorrectTwoLutEnabled;
float lerpLookupTablesAmount;
float lerpOriginalColorAmount;

// Film grain
bool filmGrainEnabled;

//////////////////////////////////////////////
///////////////// Textures ///////////////////
//////////////////////////////////////////////

texture sceneTexture : register(t9);
sampler2D sceneSampler : register(s9) = sampler_state
{
	Texture = <sceneTexture>;
	/*MipFilter = NONE;
	MagFilter = POINT;
	MinFilter = POINT;*/
};

texture2D bloomTexture : register(t10);
sampler2D bloomSampler : register(s10) = sampler_state
{
	Texture = <bloomTexture>;
    /*MipFilter = LINEAR;
	MagFilter = ANISOTROPIC;
	MinFilter = ANISOTROPIC;*/
};

texture lastLumTexture : register(t12);
sampler2D lastLumSampler : register(s12) = sampler_state
{
	Texture = <lastLumTexture>;
	/*MipFilter = NONE;
	MagFilter = POINT;
	MinFilter = POINT;*/
};

//////////////////////////////////////////////
////////////// Data Structs //////////////////
//////////////////////////////////////////////

struct VS_OUT
{
	float4 position		: POSITION;
	float2 uv			: TEXCOORD0;
};

//////////////////////////////////////////////
//////////////// Functions ///////////////////
//////////////////////////////////////////////

// This function takes an input colour in linear space, and converts it to a tone mapped gamma corrected color output
float3 ToneMapping(float3 color)
{
	// Multiply the incomming light by the lens exposure value. Think of this in terms of a camera:
	// Exposure time on a camera adjusts how long the camera collects light on the main sensor.
	// This is a simple multiplication factor of the incomming light.	
	color *= lensExposure;		

	// Tone Mapping
	color = ToneMapFilmicALU(color);
	
	// Is already in gamma space.
	return float3(color);

} // ToneMapping

// Apply the bloom effect.
float3 Bloom(float3 color, float2 uv)
{	
	float4 bloomSample = tex2D(bloomSampler, uv);
	
	// Because the bloom texture is 8bit RGBA and is heavily blurred, it's very tricky to store the difference
	// between a moderate bloom and an extremely bright bloom spot (such as the sun).
	// So, the alpha channel of the bloom texture stores a 'bloom booster', storing bloom intensity * 0.1, so extra bright bloom is kept.
	
	float bloomBoost = bloomSample.a * 10;
	float3 bloom = bloomSample.rgb * bloomScale * (1 + bloomBoost);
	
	// Add bloom, using additive.
	// Except, in order to prevent the bloom from blowing out, multiply it by the inverse
	// of the colour it's being added to. So the output won't go above 1.	
	return bloom * saturate(1 - color.rgb);
} // Bloom

//////////////////////////////////////////////
////////////// Vertex Shader /////////////////
//////////////////////////////////////////////

VS_OUT vs_main(in float4 position : POSITION, in float2 uv : TEXCOORD)
{
	VS_OUT output = (VS_OUT)0;
	
	output.position = position;
	output.position.xy += halfPixel; // http://drilian.com/2008/11/25/understanding-half-pixel-and-half-texel-offsets/
	output.uv = uv; 
	
	return output;
} // vs_main

//////////////////////////////////////////////
/////////////// Pixel Shader /////////////////
//////////////////////////////////////////////

// Creates the luminance map for the scene
float4 psLuminanceMap(in float2 uv : TEXCOORD0) : COLOR0
{
    // Sample the input
    float3 color = tex2D(sceneSampler, uv);
   
    // calculate the luminance using a weighted average
    float luminance = max(dot(color, float3(0.299f, 0.587f, 0.114f)), 0.0001f);
                
    return float4(luminance, 1.0f, 1.0f, 1.0f);
} // psLuminanceMap

// Slowly adjusts the scene luminance based on the previous scene luminance
float4 psAdaptLuminance(in float2 uv : TEXCOORD0) : COLOR0
{
    float lastLum = tex2D(lastLumSampler, uv).r;
    float currentLum = tex2D(sceneSampler, uv).r;	
		       
    // Adapt the luminance using Pattanaik's technique    
    float adaptedLum = lastLum + (currentLum - lastLum) * (1 - exp(-timeDelta * tau));
    
    return float4(adaptedLum, 1, 1, 1);
} // psAdaptLuminance

// An easy optimization consists in make a specific technique without branching.
float4 psPostProcess(in float2 uv : TEXCOORD0) : COLOR0
{
	float3 color = tex2D(sceneSampler, uv);	// HDR Linear space	
		
	float avgLuminance = tex2Dlod(lastLumSampler, float4(uv, 0, 10)).x;	
		
	// Use geometric mean        
    avgLuminance = max(avgLuminance, 0.001f);
	float exposure;

    float keyValue = 0;
	keyValue = 1.03f - (2.0f / (2 + log10(avgLuminance + 1)));
    float linearExposure = (keyValue / avgLuminance);
    exposure = log2(max(linearExposure, 0.0001f));	
    exposure = exp2(exposure);	

	color = ToneMapFilmicALU(color * exposure); // LDR Gamma space 
	
	// Film grain has to be calculated before bloom to avoid artifacts and after film tone mapping.
	if (filmGrainEnabled)
		color = FilmGrain(color, uv);	

	if (bloomEnabled)
		color = color + Bloom(color, uv);
	
	if (adjustLevelsEnabled)
		color = AdjustLevels(color);

	if (adjustLevelsIndividualChannelsEnabled)
		color = AdjustLevelsIndividualChannels(color);

	if (colorCorrectOneLutEnabled)
		color = lerp(color, TransformColor(color, firstlookupTableSampler), lerpOriginalColorAmount);

	if (colorCorrectTwoLutEnabled)
		color = lerp(color, lerp(TransformColor(color, firstlookupTableSampler), TransformColor(color, secondlookupTableSampler), lerpLookupTablesAmount), lerpOriginalColorAmount);
	
	return float4(color, 1);
} // psPostProcess

//////////////////////////////////////////////
//////////////// Techniques //////////////////
//////////////////////////////////////////////

technique LuminanceMap
{
	pass p0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader  = compile ps_3_0 psLuminanceMap();
	}
} // LuminanceMap

technique AdaptLuminance
{
	pass p0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader  = compile ps_3_0 psAdaptLuminance();
	}
} // AdaptLuminance

technique PostProcessing
{
	pass p0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader  = compile ps_3_0 psPostProcess();
	}
} // PostProcessing