Shader "GFL2/Character/Hair"
{
    Properties
    {
        [Header(Base)]
        _MainTex         ("Base Color (with AO)", 2D) = "white" {}
        _Color           ("Tint Color",         Color) = (1, 1, 1, 1)

        [Header(Toon Shadow)]
        _ShadowRampTex   ("Shadow Ramp",        2D) = "white" {}
        _ShadowThreshold ("Shadow Threshold",   Range(0, 1)) = 0.45
        _ShadowSmooth    ("Shadow Smooth",      Range(0, 0.5)) = 0.06
        _DarkShadowColor ("Dark Shadow Color",  Color) = (0.12, 0.08, 0.12, 1)

        [Header(Anisotropic Hair Specular)]
        _HairSpecTex     ("Hair Spec Mask (UV1)", 2D) = "white" {}
        _SpecColor2      ("Spec Color",         Color) = (1, 0.95, 0.85, 1)
        _SpecShift       ("Spec UV Shift",      Range(-1, 1)) = 0.2
        _SpecMinimum     ("Spec Minimum",       Range(0, 1)) = 0.05
        _BlinnPhongPow   ("Spec Sharpness",     Range(1, 256)) = 64
        _SpecIntensity   ("Spec Intensity",     Range(0, 5)) = 1.2

        [Header(Rim Light)]
        _RimPower        ("Rim Power",          Range(1, 10)) = 4
        _RimIntensity    ("Rim Intensity",      Range(0, 3)) = 0.5
        _LeftRimColor    ("Left Rim Color",     Color) = (0.3, 0.5, 0.9, 1)
        _RightRimColor   ("Right Rim Color",    Color) = (0.9, 0.4, 0.3, 1)
        _RimHighlightColor ("Rim Highlight",    Color) = (0.8, 0.8, 0.8, 1)

        [Header(Environment)]
        _EnvCubeMap      ("Environment Cube",   Cube) = "" {}
        _EnvMipLevel     ("Env Mip Level",      Range(0, 10)) = 7
        _EnvIntensity    ("Env Intensity",      Range(0, 2)) = 0.2

        [Header(Outline)]
        _OutlineWidth    ("Outline Width",      Range(0, 5)) = 1.2
        _OutlineColor    ("Outline Color",      Color) = (0.1, 0.06, 0.08, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }

        // ======== Pass 0: Hair Lit ========
        Pass
        {
            Name "HAIR_FORWARD"
            Tags { "LightMode"="ForwardBase" }

            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #include "GFL2CharacterCommon.cginc"

            sampler2D _MainTex, _ShadowRampTex, _HairSpecTex;
            float4 _MainTex_ST;
            half4 _Color;
            half _ShadowThreshold, _ShadowSmooth;
            half4 _DarkShadowColor;
            half4 _SpecColor2;
            half _SpecShift, _SpecMinimum, _BlinnPhongPow, _SpecIntensity;
            half _RimPower, _RimIntensity;
            half4 _LeftRimColor, _RightRimColor, _RimHighlightColor;
            samplerCUBE _EnvCubeMap;
            half _EnvMipLevel, _EnvIntensity;

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float2 uv0     : TEXCOORD0;
                float2 uv1     : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv0      : TEXCOORD0;
                float2 uv1      : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
                SHADOW_COORDS(4)
                UNITY_FOG_COORDS(5)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                o.uv1 = v.uv1;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half3 normalWS = normalize(i.normalWS);
                half3 viewDir  = normalize(_WorldSpaceCameraPos - i.worldPos);
                half3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                half3 halfDir  = normalize(viewDir + lightDir);

                half NdotL = dot(normalWS, lightDir);
                half NdotH = saturate(dot(normalWS, halfDir));
                half NdotV = saturate(dot(normalWS, viewDir));

                half4 baseColor = tex2D(_MainTex, i.uv0) * _Color;
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                // --- Toon Shadow ---
                half3 toonShadow = GFL2ToonShadow(
                    NdotL, atten,
                    _ShadowThreshold, _ShadowSmooth,
                    _ShadowRampTex, _DarkShadowColor.rgb, 1.0
                );

                // --- Anisotropic Hair Specular ---
                float2 specUV = float2(i.uv1.x, i.uv1.y + _SpecShift * NdotV);
                half hairSpecMask = tex2D(_HairSpecTex, specUV).r;
                half hairSpecStrength = _SpecMinimum + pow(NdotH, _BlinnPhongPow) * saturate(NdotL * 0.5 + 0.5);
                half3 hairSpec = hairSpecMask * _SpecColor2.rgb * hairSpecStrength * _SpecIntensity;

                // --- Rim Light ---
                half3 rim = GFL2RimLight(
                    normalWS, viewDir, saturate(NdotL),
                    _RimPower, _RimIntensity,
                    _LeftRimColor.rgb, _RightRimColor.rgb, _RimHighlightColor.rgb
                );

                // --- Environment ---
                half3 reflectDir = reflect(-viewDir, normalWS);
                half3 env = GFL2EnvAmbient(_EnvCubeMap, reflectDir, _EnvMipLevel, _EnvIntensity);

                // --- Compose ---
                half3 diffuse = baseColor.rgb * toonShadow * _LightColor0.rgb;
                half3 finalColor = diffuse + env * baseColor.rgb + hairSpec + rim;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return half4(finalColor, 1.0);
            }
            ENDCG
        }

        // ======== Pass 1: Outline ========
        Pass
        {
            Name "HAIR_OUTLINE"
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
                half3 base = tex2D(_MainTex, i.uv).rgb;
                return half4(_OutlineColor.rgb * base * 0.4, 1.0);
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
