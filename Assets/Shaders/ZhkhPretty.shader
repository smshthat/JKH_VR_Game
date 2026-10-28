Shader "ZHKH/Pretty"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.35
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _RimColor ("Rim Color", Color) = (0.6,0.75,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 3.0
        _RimStrength ("Rim Strength", Range(0,1)) = 0.16
        _Variation ("Color Variation", Range(0,0.3)) = 0.05
        _Saturation ("Saturation", Range(0.5,1.6)) = 1.1
        _EmissionColor ("Emission", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 viewDir;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        half _RimPower;
        half _RimStrength;
        half _Variation;
        half _Saturation;
        fixed4 _Color;
        fixed4 _RimColor;
        fixed4 _EmissionColor;

        float hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float vnoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            float a = hash21(i);
            float b = hash21(i + float2(1, 0));
            float c = hash21(i + float2(0, 1));
            float d = hash21(i + float2(1, 1));
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // мягкие пятна цвета, чтобы большие плоскости не выглядели плоско
            float n = vnoise(IN.worldPos.xz * 0.35 + IN.worldPos.y * 0.2);
            c.rgb *= 1.0 + (n - 0.5) * 2.0 * _Variation;

            // лёгкое повышение насыщенности
            half lum = dot(c.rgb, half3(0.299, 0.587, 0.114));
            c.rgb = lerp(half3(lum, lum, lum), c.rgb, _Saturation);

            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;

            // мягкая подсветка по краю (rim)
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            o.Emission = _EmissionColor.rgb + _RimColor.rgb * pow(rim, _RimPower) * _RimStrength;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
