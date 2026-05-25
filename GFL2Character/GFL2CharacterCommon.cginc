#ifndef GFL2_CHARACTER_COMMON_INCLUDED
#define GFL2_CHARACTER_COMMON_INCLUDED

#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "Lighting.cginc"

// ============================================================
// Toon Shadow
// ============================================================
half3 GFL2ToonShadow(half NdotL, half shadow, half shadowThreshold, half shadowSmooth,
                      sampler2D rampTex, half3 darkColor, half aoMask)
{
    half halfLambert = NdotL * 0.5 + 0.5;
    half toonNdotL = halfLambert * lerp(1.0, shadow, 0.85);
    half toonStep = smoothstep(shadowThreshold - shadowSmooth,
                               shadowThreshold + shadowSmooth, toonNdotL);
    half3 rampColor = tex2D(rampTex, float2(toonStep, 0.5)).rgb;
    return lerp(darkColor, rampColor, saturate(toonStep + aoMask));
}

// ============================================================
// Dual Rim Light (left/right + fresnel highlight)
// ============================================================
half3 GFL2RimLight(half3 normalWS, half3 viewDir, half NdotL,
                    half rimPower, half rimIntensity,
                    half3 leftColor, half3 rightColor, half3 highlightColor)
{
    half fresnel = pow(1.0 - saturate(dot(viewDir, normalWS)), rimPower);
    half leftMask  = step(0, normalWS.x);
    half rightMask = 1.0 - leftMask;
    half3 leftRim  = leftMask  * fresnel * leftColor;
    half3 rightRim = rightMask * fresnel * rightColor;
    half3 fresnelHighlight = fresnel * saturate(NdotL) * highlightColor;
    return (leftRim + rightRim + fresnelHighlight) * rimIntensity;
}

// ============================================================
// Environment Cubemap (high mip for soft ambient)
// ============================================================
half3 GFL2EnvAmbient(samplerCUBE envCube, half3 reflectDir, half mipLevel, half intensity)
{
    half3 envColor = texCUBElod(envCube, float4(reflectDir, mipLevel)).rgb;
    return envColor * intensity;
}

// ============================================================
// Forward Fake Light (camera-direction based fill)
// ============================================================
half3 GFL2ForwardLight(half3 normalWS, half3 viewDir, half intensity, half3 color)
{
    half fresnel = pow(1.0 - saturate(dot(viewDir, normalWS)), 3.0);
    half NdotV = saturate(dot(normalWS, viewDir));
    return color * (NdotV * 0.5 + fresnel * 0.5) * intensity;
}

// ============================================================
// Outline Vertex Transform
// ============================================================
float4 GFL2OutlineClipPos(float4 posOS, float3 normalOS, half outlineWidth)
{
    float4 clipPos = UnityObjectToClipPos(posOS);
    float3 clipNormal = mul((float3x3)UNITY_MATRIX_VP, mul((float3x3)unity_ObjectToWorld, normalOS));
    float2 offset = normalize(clipNormal.xy) * outlineWidth * clipPos.w * 0.01;
    clipPos.xy += offset;
    return clipPos;
}

#endif
