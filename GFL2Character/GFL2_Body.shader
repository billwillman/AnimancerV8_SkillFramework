Shader "GFL2/Character/Body"
{
    Properties
    {
        [Header(Base)]
        _MainTex        ("Base Color",       2D) = "white" {}
        _NormalMap       ("Normal Map",       2D) = "bump"  {}
        _NormalScale     ("Normal Intensity", Range(0, 2)) = 1.0
        _RMOTex          ("RMO (R=Rough G=Metal B=AO)", 2D) = "white" {}

        [Header(Toon Shadow)]
        _ShadowRampTex   ("Shadow Ramp",     2D) = "white" {}
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowSmooth    ("Shadow Smooth",    Range(0, 0.5)) = 0.05
        _DarkShadowColor ("Dark Shadow Color", Color) = (0.15, 0.1, 0.15, 1)
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 0.85

        [Header(Specular)]
        _SpecColor2      ("Specular Color",  Color) = (1, 1, 1, 1)
        _SpecPower       ("Specular Power",  Range(1, 128)) = 32
        _SpecIntensity   ("Specular Intensity", Range(0, 3)) = 0.5

        [Header(Rim Light)]
        _RimPower        ("Rim Power",       Range(1, 10)) = 4
        _RimIntensity    ("Rim Intensity",   Range(0, 3)) = 0.6
        _LeftRimColor    ("Left Rim Color",  Color) = (0.4, 0.6, 1.0, 1)
        _RightRimColor   ("Right Rim Color", Color) = (1.0, 0.5, 0.3, 1)
        _RimHighlightColor ("Rim Highlight Color", Color) = (1, 1, 1, 1)

        [Header(Environment)]
        _EnvCubeMap      ("Environment Cube", Cube) = "" {}
        _EnvMipLevel     ("Env Mip Level",   Range(0, 10)) = 6
        _EnvIntensity    ("Env Intensity",   Range(0, 2)) = 0.3

        [Header(Forward Light)]
        _ForwardLightIntensity ("Forward Light Intensity", Range(0, 2)) = 0.2
        _ForwardLightColor     ("Forward Light Color", Color) = (1, 0.95, 0.9, 1)

        [Header(Outline)]
        _OutlineWidth    ("Outline Width",   Range(0, 5)) = 1.0
        _OutlineColor    ("Outline Color",   Color) = (0.15, 0.1, 0.1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // ======== Pass 0: Main Lit ========
        Pass
        {
            Name "BODY_FORWARD"
            Tags { "LightMode"="ForwardBase" }

            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #include "GFL2CharacterCommon.cginc"

            sampler2D _MainTex, _NormalMap, _RMOTex, _ShadowRampTex;
            float4 _MainTex_ST;
            half _NormalScale;
            half _ShadowThreshold, _ShadowSmooth, _ShadowIntensity;
            half4 _DarkShadowColor;
            half4 _SpecColor2;
            half _SpecPower, _SpecIntensity;
            half _RimPower, _RimIntensity;
            half4 _LeftRimColor, _RightRimColor, _RimHighlightColor;
            samplerCUBE _EnvCubeMap;
            half _EnvMipLevel, _EnvIntensity;
            half _ForwardLightIntensity;
            half4 _ForwardLightColor;

            struct appdata
            {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float4 tangent  : TANGENT;
                float2 uv       : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float2 uv        : TEXCOORD0;
                float3 worldPos  : TEXCOORD1;
                float3 tSpace0   : TEXCOORD2;
                float3 tSpace1   : TEXCOORD3;
                float3 tSpace2   : TEXCOORD4;
                SHADOW_COORDS(5)
                UNITY_FOG_COORDS(6)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 worldNormal  = UnityObjectToWorldNormal(v.normal);
                float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;

                o.tSpace0 = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
                o.tSpace1 = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
                o.tSpace2 = float3(worldTangent.z, worldBinormal.z, worldNormal.z);

                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 baseColor = tex2D(_MainTex, i.uv);
                half3 rmo = tex2D(_RMOTex, i.uv).rgb;
                half roughness = rmo.r;
                half metallic  = rmo.g;
                half ao        = rmo.b;

                half3 tn = UnpackNormal(tex2D(_NormalMap, i.uv));
                tn.xy *= _NormalScale;
                half3 normalWS = normalize(half3(
                    dot(i.tSpace0, tn),
                    dot(i.tSpace1, tn),
                    dot(i.tSpace2, tn)
                ));

                half3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                half3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                half3 halfDir = normalize(viewDir + lightDir);

                half NdotL = dot(normalWS, lightDir);
                half NdotH = saturate(dot(normalWS, halfDir));
                half NdotV = saturate(dot(normalWS, viewDir));

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                // --- Toon Shadow ---
                half3 toonShadow = GFL2ToonShadow(
                    NdotL, atten,
                    _ShadowThreshold, _ShadowSmooth,
                    _ShadowRampTex, _DarkShadowColor.rgb, ao
                );

                // --- Specular (Blinn-Phong, toon-ified) ---
                half specMask = 1.0 - roughness;
                half spec = pow(NdotH, _SpecPower) * specMask * saturate(NdotL * 0.5 + 0.5);
                half3 specular = spec * _SpecColor2.rgb * _SpecIntensity;
                specular = lerp(specular, specular * baseColor.rgb, metallic);

                // --- Rim Light ---
                half3 rim = GFL2RimLight(
                    normalWS, viewDir, saturate(NdotL),
                    _RimPower, _RimIntensity,
                    _LeftRimColor.rgb, _RightRimColor.rgb, _RimHighlightColor.rgb
                );

                // --- Environment ---
                half3 reflectDir = reflect(-viewDir, normalWS);
                half3 env = GFL2EnvAmbient(_EnvCubeMap, reflectDir, _EnvMipLevel, _EnvIntensity);
                env *= lerp(half3(1,1,1), baseColor.rgb, metallic);

                // --- Forward Light ---
                half3 fwdLight = GFL2ForwardLight(normalWS, viewDir,
                                                   _ForwardLightIntensity, _ForwardLightColor.rgb);

                // --- Compose ---
                half3 diffuse = baseColor.rgb * toonShadow * _LightColor0.rgb;
                half3 ambient = env * ao;
                half3 finalColor = diffuse + ambient + specular + rim + fwdLight * baseColor.rgb;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return half4(finalColor, 1.0);
            }
            ENDCG
        }

        // ======== Pass 1: Outline ========
        Pass
        {
            Name "BODY_OUTLINE"
            Tags { "LightMode"="Always" }

            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "GFL2CharacterCommon.cginc"

            half _OutlineWidth;
            half4 _OutlineColor;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = GFL2OutlineClipPos(v.vertex, v.normal, _OutlineWidth);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half3 baseColor = tex2D(_MainTex, i.uv).rgb;
                half3 outlineCol = _OutlineColor.rgb * baseColor * 0.5;
                return half4(outlineCol, 1.0);
            }
            ENDCG
        }

        // ======== Pass 2: ShadowCaster ========
        Pass
        {
            Name "SHADOW_CASTER"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On ZTest LEqual
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f { V2F_SHADOW_CASTER; };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
