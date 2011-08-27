/******************************************************************************

    From MJP example (mpettineo@gmail.com)	
	Modified by: Schneider, Jos� Ignacio (jis@cs.uns.edu.ar)

******************************************************************************/

#include <ShadowMapCommon.fxh>

//////////////////////////////////////////////
/////////////// Parameters ///////////////////
//////////////////////////////////////////////

// Number of cascaded splits
static const int NUM_SPLITS = 4;

float4x4	viewToLightViewProj[NUM_SPLITS];

float2		clipPlanes[NUM_SPLITS];

//////////////////////////////////////////////
///////////// Render Shadow Map //////////////
//////////////////////////////////////////////

// Pixel shader for computing the shadow occlusion factor
float4 ps_main(in float2 uv : TEXCOORD0, in float3 frustumRay : TEXCOORD1, uniform int iFilterSize	) : COLOR0
{
	// Reconstruct position from the depth value, making use of the ray pointing towards the far clip plane	
	float depth = tex2D(depthSampler, uv).r;

	if (depth > 0.99)
		return float4(1, 1, 1, 1);

	float3 positionVS = frustumRay * depth; // To convert this position into world space it only needs to add the camera position (in the pixel shader), and the frustumray multiply by the camera orientation (in the vertex shader).
	
	int winningSplit = 0;
	float fOffset;

	if (positionVS.z <= clipPlanes[0].y)
	{
		// Unrolling the loop allows for a performance boost on the 360.
		[unroll(NUM_SPLITS - 1)]
		for (int i = 1; i < NUM_SPLITS; i++)
		{	
			[flatten] // Same for flatten.
			if (positionVS.z <= clipPlanes[i].x && positionVS.z > clipPlanes[i].y)
			{	
				winningSplit = i;
			}
		}
	}
	fOffset = winningSplit / (float)NUM_SPLITS;

	// Determine the depth of the pixel with respect to the light
	float4 positionLightCS = mul(float4(positionVS, 1), viewToLightViewProj[winningSplit]);
		
	float depthLightSpace = positionLightCS.z / positionLightCS.w; // range 0 to 1
	
	// Transform from light space to shadow map texture space.
    float2 shadowTexCoord = 0.5 * positionLightCS.xy / positionLightCS.w + float2(0.5f, 0.5f);
	shadowTexCoord.x = shadowTexCoord.x / NUM_SPLITS + fOffset;
    shadowTexCoord.y = 1.0f - shadowTexCoord.y;
        
    // Offset the coordinate by half a texel so we sample it correctly
    shadowTexCoord += (0.5f / shadowMapSize);
	
	// Get the shadow occlusion factor and output it
	float shadowTerm;
	if (iFilterSize == 0)
		shadowTerm = CalculateShadowTermPoisonPCF(depthLightSpace, shadowTexCoord);
	else if (iFilterSize == 2)		
		shadowTerm = CalculateShadowTermBilinearPCF(depthLightSpace, shadowTexCoord);
	else
		shadowTerm = CalculateShadowTermSoftPCF(depthLightSpace, shadowTexCoord, iFilterSize);
	
	/*// For testing. A color render target is need it.
	switch (winningSplit)
	{
		case 0: return float4(shadowTerm, 0, 0, 1); break;
		case 1: return float4(0, shadowTerm, 0, 1); break;
		case 2: return float4(0, 0, shadowTerm, 1); break;
		case 3: return float4(shadowTerm, shadowTerm, 0, 1); break;
	}*/
	return float4(shadowTerm, 1, 1, 1);
	
} // ps_main

//////////////////////////////////////////////
//////////////// Techniques //////////////////
//////////////////////////////////////////////

technique GenerateShadowMap
{
	pass P0
	{
		VertexShader = compile vs_3_0 VS_GenerateShadowMap();
		PixelShader  = compile ps_3_0 PS_GenerateShadowMap();
	}
} // GenerateShadowMap

technique RenderShadowMap2x2PCF
{
    pass p0
    {
        VertexShader = compile vs_3_0 vs_main();
        PixelShader  = compile ps_3_0 ps_main(2);	
    }
} // RenderShadowMap2x2PCF

technique RenderShadowMap3x3PCF
{
    pass p0
    {
        VertexShader = compile vs_3_0 vs_main();
        PixelShader  = compile ps_3_0 ps_main(3);	
    }
} // RenderShadowMap3x3PCF

technique RenderShadowMap5x5PCF
{
    pass p0
    {
        VertexShader = compile vs_3_0 vs_main();
        PixelShader  = compile ps_3_0 ps_main(5);	
    }
} // RenderShadowMap5x5PCF

technique RenderShadowMap7x7PCF
{
    pass p0
    {
        VertexShader = compile vs_3_0 vs_main();
        PixelShader  = compile ps_3_0 ps_main(7);	
    }
} // RenderShadowMap7x7PCF

technique RenderShadowMapPoisonPCF
{
    pass p0
    {
        VertexShader = compile vs_3_0 vs_main();
        PixelShader  = compile ps_3_0 ps_main(0);	
    }
} // RenderShadowMapPoisonPCF
