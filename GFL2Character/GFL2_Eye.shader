Shader "GFL2/Character/Eye"
{
    Properties
    {
        [Header(Iris)]
        _MainTex         ("Iris Texture",         2D) = "white" {}
        _IrisColor       ("Iris Tint",            Color) = (1, 1, 1, 1)
        _ParallaxDepth   ("Parallax Depth",       Range(0, 0.15)) = 0.05

        [Header(Highlight)]
        _HighlightTex    ("Highlight Texture",    2D) = "white" {}
        _HighlightColor  ("Highlight Color",      Color) = (1, 1, 1, 1)
        _HighlightIntensity ("Highlight Intensity", Range(0, 5)) = 2.0

        [Header(Shadow Rim)]
        _EyeShadowColor  ("Eye Shadow Color",     Color) = (0.3, 0.2, 0.25, 0.6)
        _EyeShadowRange  ("Shadow Range",         Range(0, 1)) = 0.4
        _EyeShadowSmooth ("Shadow Smooth",        Range(0, 0.5)) = 0.15

        [Header(Reflection)]
        _EnvCubeMap      ("Reflection Cube",      Cube) = "" {}
        _ReflIntensity   ("Reflection Intensity", Range(0, 2)) = 0.3
        _ReflMipLevel    ("Reflection Blur",      Range(0, 8)) = 3

        [Header(Emission)]
        _EmissionColor   ("Emission Color",       Color) = (0, 0, 0, 0)
        _EmissionIntensity ("Emission Intensity", Range(0, 3)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+30" }

        // ======== Pass 0: Eye Lit ========
        Pass
        {
            Name "EYE_FORWARD"
            Tags { "LightMode"="ForwardBase" }

            Cull Back
            ZWrite On

            Stencil
            {
                Ref 200
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex, _HighlightTex;
            float4 _MainTex_ST;
            half4 _IrisColor, _HighlightColor;
            half _ParallaxDepth, _HighlightIntensity;
            half4 _EyeShadowColor;
            half _EyeShadowRange, _EyeShadowSmooth;
            samplerCUBE _EnvCubeMap;
            half _ReflIntensity, _ReflMipLevel;
            half4 _EmissionColor;
            half _EmissionIntensity;

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float2 uv      : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float2 uv        : TEXCOORD0;
                float3 normalWS  : TEXCOORD1;
                float3 worldPos  : TEXCOORD2;
                float3 viewDirTS : TEXCOORD3;
                SHADOW_COORDS(4)
                UNITY_FOG_COORDS(5)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS = UnityObjectToWorldNormal(v.normal);

                float3 worldTangent  = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBinormal = cross(o.normalWS, worldTangent) * v.tangent.w;
                float3 viewDir = _WorldSpaceCameraPos - o.worldPos;
                o.viewDirTS = float3(
                    dot(viewDir, worldTangent),
                    dot(viewDir, worldBinormal),
                    dot(viewDir, o.normalWS)
                );

                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half3 normalWS = normalize(i.normalWS);
                half3 viewDir  = normalize(_WorldSpaceCameraPos - i.worldPos);
                half3 viewDirTS = normalize(i.viewDirTS);

                // --- Parallax UV offset for iris depth ---
                float2 parallaxUV = i.uv + viewDirTS.xy * _ParallaxDepth;
                half4 irisColor = tex2D(_MainTex, parallaxUV) * _IrisColor;

                // --- Highlight layer ---
                half4 highlight = tex2D(_HighlightTex, i.uv);
                half3 highlightFinal = highlight.rgb * _HighlightColor.rgb
                                     * _HighlightIntensity * highlight.a;

                // --- Eye shadow rim (top darker) ---
                half uvY = 1.0 - i.uv.y;
                half eyeShadow = smoothstep(_EyeShadowRange - _EyeShadowSmooth,
                                            _EyeShadowRange + _EyeShadowSmooth, uvY);
                half3 shadowTint = lerp(_EyeShadowColor.rgb, half3(1,1,1), eyeShadow);

                // --- Reflection ---
                half3 reflDir = reflect(-viewDir, normalWS);
                half3 refl = texCUBElod(_EnvCubeMap, float4(reflDir, _ReflMipLevel)).rgb;
                half fresnel = pow(1.0 - saturate(dot(viewDir, normalWS)), 3.0);
                half3 reflection = refl * fresnel * _ReflIntensity;

                // --- Simple lighting ---
                half3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                half NdotL = saturate(dot(normalWS, lightDir) * 0.5 + 0.5);
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                // --- Compose ---
                half3 diffuse = irisColor.rgb * shadowTint * NdotL * _LightColor0.rgb;
                half3 emission = _EmissionColor.rgb * _EmissionIntensity * irisColor.rgb;
                half3 finalColor = diffuse + highlightFinal + reflection + emission;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return half4(finalColor, 1.0);
            }
            ENDCG
        }

        // ======== Pass 1: ShadowCaster ========
        Pass
        {
            Name "SHADOW_CASTER"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual Cull Back

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
