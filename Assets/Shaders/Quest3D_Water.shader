Shader "Custom/Quest3D_Style_Water"
{
    Properties
    {
        [Header(Water Colors)]
        _ColorShallow ("Shallow Water Color", Color) = (0.1, 0.7, 0.7, 0.6)
        _ColorDeep ("Deep Water Color", Color) = (0.0, 0.1, 0.3, 0.9)
        _DepthMaxDistance ("Depth Max Distance", Float) = 5.0
        
        [Header(Shoreline Foam)]
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamDistance ("Foam Distance", Float) = 0.5
        
        [Header(Procedural Waves (No Textures!))]
        _WaveSpeed ("Global Wave Speed", Float) = 1.0
        _WaveScale ("Global Wave Scale", Float) = 0.5
        _WaveStrength ("Geometry Wave Height", Float) = 0.2
        _RippleStrength ("Ripple Normal Strength", Float) = 1.5
        
        [Header(Lighting)]
        _Glossiness ("Smoothness", Range(0,1)) = 0.95
        _Metallic ("Metallic", Range(0,1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        CGPROGRAM
        #pragma surface surf Standard vertex:vert alpha:fade keepalpha
        #pragma target 3.0

        sampler2D _CameraDepthTexture;

        struct Input
        {
            float4 screenPos;
            float3 worldPos;
        };

        fixed4 _ColorShallow;
        fixed4 _ColorDeep;
        float _DepthMaxDistance;
        
        fixed4 _FoamColor;
        float _FoamDistance;
        
        float _WaveSpeed;
        float _WaveScale;
        float _WaveStrength;
        float _RippleStrength;
        
        half _Glossiness;
        half _Metallic;

        // Математический расчет волн и производных (нормалей)
        float CalculateWaves(float2 pos, float time, out float2 derivatives)
        {
            float height = 0.0;
            derivatives = float2(0.0, 0.0);
            
            float2 d1 = normalize(float2(1.0, 0.5));
            float2 d2 = normalize(float2(-0.7, 0.7));
            float2 d3 = normalize(float2(0.2, -0.9));
            float2 d4 = normalize(float2(-0.5, -0.5));

            float f1 = 1.0, f2 = 2.3, f3 = 3.7, f4 = 5.1;
            float a1 = 0.5, a2 = 0.25, a3 = 0.12, a4 = 0.06;
            float s1 = 1.2, s2 = 1.5, s3 = 2.1, s4 = 2.5;

            // Wave 1
            float x = dot(d1, pos) * f1 * _WaveScale;
            float t = time * s1 * _WaveSpeed;
            float sinVal, cosVal;
            sincos(x + t, sinVal, cosVal);
            height += sinVal * a1;
            derivatives += d1 * (cosVal * a1 * f1 * _WaveScale);

            // Wave 2
            x = dot(d2, pos) * f2 * _WaveScale;
            t = time * s2 * _WaveSpeed;
            sincos(x + t, sinVal, cosVal);
            height += sinVal * a2;
            derivatives += d2 * (cosVal * a2 * f2 * _WaveScale);

            // Wave 3
            x = dot(d3, pos) * f3 * _WaveScale;
            t = time * s3 * _WaveSpeed;
            sincos(x + t, sinVal, cosVal);
            height += sinVal * a3;
            derivatives += d3 * (cosVal * a3 * f3 * _WaveScale);

            // Wave 4
            x = dot(d4, pos) * f4 * _WaveScale;
            t = time * s4 * _WaveSpeed;
            sincos(x + t, sinVal, cosVal);
            height += sinVal * a4;
            derivatives += d4 * (cosVal * a4 * f4 * _WaveScale);
            
            return height;
        }

        void vert (inout appdata_full v) {
            float2 derivatives;
            // Считаем волны в мировых координатах, чтобы они были бесконечными
            float2 worldXZ = mul(unity_ObjectToWorld, v.vertex).xz;
            
            float h = CalculateWaves(worldXZ, _Time.y, derivatives);
            
            // Применяем высоту к вершине (геометрическая волна)
            v.vertex.y += h * _WaveStrength;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 1. Глубина и Пена (как и было)
            float waterSurfaceDepth = IN.screenPos.w;
            float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
            float depthDifference = sceneZ - waterSurfaceDepth;
            
            float depthFactor = saturate(depthDifference / _DepthMaxDistance);
            fixed4 waterColor = lerp(_ColorShallow, _ColorDeep, depthFactor);
            
            float foamFactor = 1.0 - saturate(depthDifference / _FoamDistance);
            foamFactor = pow(foamFactor, 2.0);
            fixed4 finalColor = lerp(waterColor, _FoamColor, foamFactor);
            
            // 2. ПРОЦЕДУРНЫЕ НОРМАЛИ (Магия Quest3D)
            float2 derivatives;
            // Пересчитываем формулы волн для каждого пикселя для идеальной резкости ряби
            CalculateWaves(IN.worldPos.xz, _Time.y, derivatives);
            
            // Конструируем Normal Map математически из производных!
            float3 proceduralNormal = float3(-derivatives.x * _RippleStrength, -derivatives.y * _RippleStrength, 1.0);
            proceduralNormal = normalize(proceduralNormal);

            o.Albedo = finalColor.rgb;
            o.Normal = proceduralNormal; // Применяем чисто математическую рябь
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = saturate(finalColor.a + foamFactor);
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
