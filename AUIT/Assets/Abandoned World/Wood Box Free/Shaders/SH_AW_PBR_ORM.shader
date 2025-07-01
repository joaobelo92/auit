Shader "SH_AW_PBR_ORM" // Especially for the store "Abandoned World"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Normal ("Normal Map", 2D) = "normal" {}
		_ORM ("AO,Roughness,Metallic", 2D) = "orm" {}
		_Emissive("Emissive", 2D) = "emissive" {}
        _NormalStrength("Normal Map Strength", Float) = 1.0
		_Roughness ("Roughness", Range(0,2)) = 1
        _Metallic ("Metallic", Range(0,2)) = 1
		_AO_Intensity ("AO Intensity", Range(0,10)) = 1
		_Emission ("Emission", float) = 0
		_ColorEmission ("Color Emissive", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
		sampler2D _Normal;
		sampler2D _ORM;
		sampler2D _Emissive;


        struct Input
        {
            float2 uv_MainTex;
			float2 uv_Normal;
			float2 uv_ORM;
			float2 uv_Emissive;
        };

        half _Roughness;
        half _Metallic;
		half _Emission;
		half _AO_Intensity;
		half _NormalStrength;
        fixed4 _Color;
		fixed4 _ColorEmission;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
			o.Normal = UnpackNormal(tex2D(_Normal, IN.uv_Normal)) * float3(1, _NormalStrength, 1);

            // ORM - Occlusion, Roughness, Metallic
			fixed4 orm = tex2D(_ORM, IN.uv_ORM);
			fixed4 emissive = tex2D(_Emissive, IN.uv_Emissive);

			o.Metallic =  orm.b * _Metallic;
            o.Smoothness = (abs(1-orm.g)) * _Roughness;
			o.Occlusion = orm.r * _AO_Intensity;
			o.Emission = emissive * _Emission * _ColorEmission;
            o.Alpha = c.a;
			
        }
        ENDCG
    }
    FallBack "Diffuse"
}