Shader "ZHKH/Foliage"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.15
        _WindStrength ("Wind Strength", Range(0,0.3)) = 0.04
        _WindSpeed ("Wind Speed", Range(0,5)) = 1.2
        _Variation ("Color Variation", Range(0,0.4)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        half _Glossiness;
        half _WindStrength;
        half _WindSpeed;
        half _Variation;
        fixed4 _Color;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
        };

        void vert (inout appdata_full v)
        {
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            float t = _Time.y * _WindSpeed;
            float s = sin(t + wp.x * 0.7 + wp.z * 0.5) + 0.5 * sin(t * 2.3 + wp.z * 1.1);
            float h = saturate(v.vertex.y + 0.6);
            v.vertex.xz += s * _WindStrength * h;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            float k = sin(IN.worldPos.x * 3.1 + IN.worldPos.y * 4.7 + IN.worldPos.z * 2.3) * 0.5 + 0.5;
            c.rgb *= 1.0 + (k - 0.5) * _Variation;
            o.Albedo = c.rgb;
            o.Smoothness = _Glossiness;
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            o.Emission = c.rgb * 0.06 + c.rgb * pow(rim, 3.0) * 0.12;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
