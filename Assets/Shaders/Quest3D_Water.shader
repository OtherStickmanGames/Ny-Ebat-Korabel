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
        
        [Header(Procedural Waves)]
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

        // === ПРОЦЕДУРНЫЙ ШУМ (Борьба с тайлингом) ===
        // Хэш-функция Dave Hoskins (Высококачественный шум без паттернов сетки)
        float hash(float2 p) {
            float3 p3  = frac(float3(p.xyx) * .1031);
            p3 += dot(p3, p3.yzx + 33.33);
            return frac((p3.x + p3.y) * p3.z);
        }

        // Двумерный Value Noise
        float noise(float2 p) {
            float2 i = floor(p);
            float2 f = frac(p);
            // Плавная интерполяция
            float2 u = f * f * (3.0 - 2.0 * f); 
            
            float a = hash(i + float2(0.0, 0.0));
            float b = hash(i + float2(1.0, 0.0));
            float c = hash(i + float2(0.0, 1.0));
            float d = hash(i + float2(1.0, 1.0));
            
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }

        // Fractional Brownian Motion (FBM) с 3 октавами и поворотами
        float fbm(float2 p) {
            float value = 0.0;
            float amplitude = 0.5;
            // Матрица поворота для слома осевой симметрии между октавами
            float2x2 rot = float2x2(0.8, -0.6, 0.6, 0.8); 
            
            // Октава 1
            value += amplitude * noise(p);
            p = mul(rot, p) * 2.0;
            amplitude *= 0.5;
            
            // Октава 2
            value += amplitude * noise(p);
            p = mul(rot, p) * 2.0;
            amplitude *= 0.5;
            
            // Октава 3
            value += amplitude * noise(p);
            
            return value;
        }

        // === РАСЧЕТ ВЫСОТЫ ВОДЫ ===
        float GetHeight(float2 pos, float time) 
        {
            // 1. FBM DOMAIN WARPING (Искажение пространства)
            // Использование FBM вместо обычного шума дает фрактальное искажение, 
            // которое полностью ломает сетку синусоид на любых масштабах.
            float2 warp = float2(fbm(pos * 0.1), fbm(pos * 0.1 + 100.0));
            pos += warp * 5.0; // Сила искажения
            
            // Направления волн сделаны иррациональными
            float2 d1 = normalize(float2(1.0, 0.414));
            float2 d2 = normalize(float2(-0.732, 0.618));
            float2 d3 = normalize(float2(0.223, -0.985));
            float2 d4 = normalize(float2(-0.551, -0.421));

            // Частоты волн идут по Золотому Сечению (1.618), чтобы никогда не входить в резонанс
            float f1 = 1.0, f2 = 1.618, f3 = 2.618, f4 = 4.236;
            float a1 = 0.5, a2 = 0.25, a3 = 0.12, a4 = 0.06;
            float s1 = 1.2, s2 = 1.5, s3 = 2.1, s4 = 2.5;

            float h = 0.0;
            h += sin(dot(d1, pos) * f1 * _WaveScale + time * s1 * _WaveSpeed) * a1;
            h += sin(dot(d2, pos) * f2 * _WaveScale + time * s2 * _WaveSpeed) * a2;
            h += sin(dot(d3, pos) * f3 * _WaveScale + time * s3 * _WaveSpeed) * a3;
            h += sin(dot(d4, pos) * f4 * _WaveScale + time * s4 * _WaveSpeed) * a4;
            
            // Добавляем мелкую высокочастотную FBM-рябь поверх волн
            h += fbm(pos * 2.0 - time * 0.5) * 0.1;
            
            return h;
        }

        void vert (inout appdata_full v) {
            float2 worldXZ = mul(unity_ObjectToWorld, v.vertex).xz;
            
            // Применяем высоту только к сетке
            float h = GetHeight(worldXZ, _Time.y);
            v.vertex.y += h * _WaveStrength;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float waterSurfaceDepth = IN.screenPos.w;
            float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
            float depthDifference = sceneZ - waterSurfaceDepth;
            
            float depthFactor = saturate(depthDifference / _DepthMaxDistance);
            fixed4 waterColor = lerp(_ColorShallow, _ColorDeep, depthFactor);
            
            // === ПРОЦЕДУРНАЯ ПЕНА (Shoreline Foam) ===
            float rawFoam = 1.0 - saturate(depthDifference / _FoamDistance);
            
            // Используем наш многооктавный FBM для генерации "пузырьков" пены, которые плывут по течению
            float foamNoise = fbm(IN.worldPos.xz * 3.0 - _Time.y * 0.8);
            
            // smoothstep делает пену не просто градиентом, а рваными кусками (как настоящая пена, бьющаяся о скалы)
            float foamFactor = smoothstep(0.3, 0.7, rawFoam * (foamNoise + 0.5));
            
            fixed4 finalColor = lerp(waterColor, _FoamColor, foamFactor);
            
            // === РАСЧЕТ ПРОЦЕДУРНЫХ НОРМАЛЕЙ ===
            // Вместо аналитических производных мы используем Finite Differences (Конечные разности).
            // Мы "щупаем" высоту воды чуть правее и чуть выше текущего пикселя.
            float epsilon = 0.05; // Шаг смещения
            
            float hBase = GetHeight(IN.worldPos.xz, _Time.y);
            float hX = GetHeight(IN.worldPos.xz + float2(epsilon, 0), _Time.y);
            float hZ = GetHeight(IN.worldPos.xz + float2(0, epsilon), _Time.y);
            
            // Разница высот дает идеальную нормаль, учитывающую и волны, и шум, и искажения
            float3 proceduralNormal = float3(-(hX - hBase) / epsilon, -(hZ - hBase) / epsilon, 1.0);
            proceduralNormal.xy *= _RippleStrength;
            proceduralNormal = normalize(proceduralNormal);

            o.Albedo = finalColor.rgb;
            o.Normal = proceduralNormal;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = saturate(finalColor.a + foamFactor);
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
