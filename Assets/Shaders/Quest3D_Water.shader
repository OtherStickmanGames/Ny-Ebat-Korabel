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
            
            // 4 математические волны в разных направлениях и с разными частотами
            float2 dirs[4] = {
                normalize(float2(1.0, 0.5)),
                normalize(float2(-0.7, 0.7)),
                normalize(float2(0.2, -0.9)),
                normalize(float2(-0.5, -0.5))
            };
            float frequencies[4] = {1.0, 2.3, 3.7, 5.1};
            float amplitudes[4] = {0.5, 0.25, 0.12, 0.06};
            float speeds[4] = {1.2, 1.5, 2.1, 2.5};

            for (int i = 0; i < 4; i++)
            {
                // Позиция умноженная на направление и масштаб
                float x = dot(dirs[i], pos) * frequencies[i] * _WaveScale;
                float t = time * speeds[i] * _WaveSpeed;
                
                float sinVal, cosVal;
                sincos(x + t, sinVal, cosVal);
                
                // Синус дает высоту волны
                height += sinVal * amplitudes[i];
                
                // Косинус дает производную (наклон волны) для расчета нормалей
                derivatives += dirs[i] * (cosVal * amplitudes[i] * frequencies[i] * _WaveScale);
            }
            
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
