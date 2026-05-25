Shader "GFL2/Character/Face"
{
    Properties
    {
        [Header(Base)]
        _MainTex          ("Base Color",          2D) = "white" {}
        _Color            ("Tint",                Color) = (1, 1, 1, 1)

        [Header(SDF Face Shadow)]
        _FaceSDFTex       ("Face SDF Map",        2D) = "white" {}
        _FaceForward      ("Face Forward Dir",    Vector) = (0, 0, 1, 0)
        _FaceRight         ("Face Right Dir",      Vector) = (1, 0, 0, 0)
        _SDFShadowSmooth  ("SDF Shadow Smooth",   Range(0, 0.5)) = 0.05
        _ShadowColor      ("Shadow Color",        Color) = (0.7, 0.55, 0.6, 1)

        [Header(Fringe Shadow)]
        _FringeShadowTex  ("Fringe Shadow Map",   2D) = "white" {}
        _FringeShadowOffset ("Fringe Offset",     Range(-0.1, 0.1)) = 0.02
        _FringeShadowIntensity ("Fringe Intensity", Range(0, 1)) = 0.6
        _FringeShadowColor ("Fringe Shadow Color", Color) = (0.6, 0.45, 0.5, 1)

        [Header(Rim Light)]
        _RimPower         ("Rim Power",           Range(1, 10)) = 5
        _RimIntensity     ("Rim Intensity",       Range(0, 3)) = 0.4
        _LeftRimColor     ("Left Rim Color",      Color) = (0.4, 0.5, 0.9, 1)
        _RightRimColor    ("Right Rim Color",     Color) = (0.9, 0.4, 0.3, 1)
        _RimHighlightColor ("Rim Highlight",      Color) = (0.6, 0.6, 0.6, 1)

        [Header(Forward Light)]
        _ForwardLightIntensity ("Forward Light", Range(0, 2)) = 0.25
        _ForwardLightColor ("Forward Light Color", Color) = (1, 0.95, 0.9, 1)

        [Header(Outline)]
        _OutlineWidth     ("Outline Width",       Range(0, 5)) = 0.8
        _OutlineColor     ("Outline Color",       Color) = (0.2, 0.12, 0.12, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+20" }

        // ======== Pass 0: Face Lit ========
        Pass
        {
            Name "FACE_FORWARD"
            Tags { "LightMode"="ForwardBase" }

            Cull Back
            ZWrite On

            Stencil
            {
                Ref 100
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #include "GFL2CharacterCommon.cginc"

            sampler2D _MainTex, _FaceSDFTex, _FringeShadowTex;
            float4 _MainTex_ST;
            half4 _Color;
            half4 _FaceForward, _FaceRight;
            half _SDFShadowSmooth;
            half4 _ShadowColor;
            half _FringeShadowOffset, _FringeShadowIntensity;
            half4 _FringeShadowColor;
            half _RimPower, _RimIntensity;
            half4 _LeftRimColor, _RightRimColor, _RimHighlightColor;
            half _ForwardLightIntensity;
            half4 _ForwardLightColor;

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float2 uv      : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                SHADOW_COORDS(3)
                UNITY_FOG_COORDS(4)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 baseColor = tex2D(_MainTex, i.uv) * _Color;
                half3 normalWS = normalize(i.normalWS);
                half3 viewDir  = normalize(_WorldSpaceCameraPos - i.worldPos);
                half3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                // --- SDF Face Shadow ---
                half3 faceForward = normalize(mul((float3x3)unity_ObjectToWorld, _FaceForward.xyz));
                half3 faceRight   = normalize(mul((float3x3)unity_ObjectToWorld, _FaceRight.xyz));

                half lightDotFwd   = dot(lightDir, faceForward);
                half lightDotRight = dot(lightDir, faceRight);

                // Remap angle to [0,1] for SDF sampling
                half sdfAngle = lightDotRight * 0.5 + 0.5;

                half sdfLeft  = tex2D(_FaceSDFTex, float2(    i.uv.x, i.uv.y)).r;
                half sdfRight = tex2D(_FaceSDFTex, float2(1 - i.uv.x, i.uv.y)).r;
                half sdfValue = lightDotRight > 0 ? sdfRight : sdfLeft;

                half faceShadow = smoothstep(sdfAngle - _SDFShadowSmooth,
                                             sdfAngle + _SDFShadowSmooth, sdfValue);
                faceShadow *= step(0, lightDotFwd);

                half3 shadowResult = lerp(_ShadowColor.rgb, half3(1,1,1), faceShadow);

                // --- Fringe Shadow (hair cast onto face) ---
                float2 fringeUV = i.uv + float2(lightDotRight, -lightDotFwd) * _FringeShadowOffset;
                half fringeMask = tex2D(_FringeShadowTex, fringeUV).r;
                half3 fringeShadow = lerp(half3(1,1,1), _FringeShadowColor.rgb,
                                          fringeMask * _FringeShadowIntensity);

                // --- Rim Light ---
                half NdotL = saturate(dot(normalWS, lightDir));
                half3 rim = GFL2RimLight(
                    normalWS, viewDir, NdotL,
                    _RimPower, _RimIntensity,
                    _LeftRimColor.rgb, _RightRimColor.rgb, _RimHighlightColor.rgb
                );

                // --- Forward Light ---
                half3 fwdLight = GFL2ForwardLight(normalWS, viewDir,
                                                   _ForwardLightIntensity, _ForwardLightColor.rgb);

                // --- Compose ---
                half3 diffuse = baseColor.rgb * shadowResult * fringeShadow * _LightColor0.rgb;
                half3 finalColor = diffuse + rim + fwdLight * baseColor.rgb;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return half4(finalColor, 1.0);
            }
            ENDCG
        }

        // ======== Pass 1: Face Outline (thinner) ========
        Pass
        {
            Name "FACE_OUTLINE"
            Tags { "LightMode"="Always" }

            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "GFL2CharacterCommon.cginc"

            half _OutlineWidth;
            half4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = GFL2OutlineClipPos(v.vertex, v.normal, _OutlineWidth * 0.6);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // ======== Pass 2: ShadowCaster ========
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
