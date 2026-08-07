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
        
        [Header(Surface Ripples (Normals))]
        _NormalMap1 ("Normal Map 1", 2D) = "bump" {}
        _NormalMap2 ("Normal Map 2", 2D) = "bump" {}
        _NormalSpeed1 ("Scroll Speed 1 (X,Y)", Vector) = (0.05, 0.05, 0, 0)
        _NormalSpeed2 ("Scroll Speed 2 (X,Y)", Vector) = (-0.03, 0.04, 0, 0)
        _NormalScale ("Normal Strength", Range(0, 2)) = 1.0

        [Header(Geometry Waves)]
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveHeight ("Wave Height", Float) = 0.15
        _WaveFrequency ("Wave Frequency", Float) = 2.0
        
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
            float2 uv_NormalMap1;
            float2 uv_NormalMap2;
            float4 screenPos;
            float3 viewDir;
        };

        fixed4 _ColorShallow;
        fixed4 _ColorDeep;
        float _DepthMaxDistance;
        
        fixed4 _FoamColor;
        float _FoamDistance;
        
        sampler2D _NormalMap1;
        sampler2D _NormalMap2;
        float4 _NormalSpeed1;
        float4 _NormalSpeed2;
        float _NormalScale;
        
        float _WaveSpeed;
        float _WaveHeight;
        float _WaveFrequency;
        
        half _Glossiness;
        half _Metallic;

        void vert (inout appdata_full v) {
            float t = _Time.y * _WaveSpeed;
            float wave = sin(v.vertex.x * _WaveFrequency + t) + cos(v.vertex.z * _WaveFrequency + t);
            v.vertex.y += wave * _WaveHeight;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float waterSurfaceDepth = IN.screenPos.w;
            float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
            float depthDifference = sceneZ - waterSurfaceDepth;
            
            float depthFactor = saturate(depthDifference / _DepthMaxDistance);
            fixed4 waterColor = lerp(_ColorShallow, _ColorDeep, depthFactor);
            
            float foamFactor = 1.0 - saturate(depthDifference / _FoamDistance);
            foamFactor = pow(foamFactor, 2.0);
            
            fixed4 finalColor = lerp(waterColor, _FoamColor, foamFactor);
            
            float2 uv1 = IN.uv_NormalMap1 + _Time.y * _NormalSpeed1.xy;
            float2 uv2 = IN.uv_NormalMap2 + _Time.y * _NormalSpeed2.xy;
            
            float3 n1 = UnpackNormal(tex2D(_NormalMap1, uv1));
            float3 n2 = UnpackNormal(tex2D(_NormalMap2, uv2));
            
            float3 finalNormal = normalize(n1 + n2);
            finalNormal.xy *= _NormalScale;
            
            o.Albedo = finalColor.rgb;
            o.Normal = finalNormal;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            
            o.Alpha = saturate(finalColor.a + foamFactor);
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
