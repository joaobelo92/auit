Shader "Custom/StandardWithWireToggle"
{
    Properties
    {
        _Color      ("Tint", Color) = (1,1,1,1)
        _MainTex    ("Albedo", 2D) = "white" {}
        _Metallic   ("Metallic", Range(0,1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5

        _WireColor      ("Wire Color (RGBA)", Color) = (0,0,0,1)
        _WireThickness  ("Wire Thickness", Range(0.5, 5.0)) = 5

        _EnableEdge0 ("Enable Edge 0 (between v1-v2)", Float) = 1
        _EnableEdge1 ("Enable Edge 1 (between v2-v3)", Float) = 1
        _EnableEdge2 ("Enable Edge 2 (between v3-v1)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        // --- Pass 1: Standard-like surface ---
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Metallic;
        half _Glossiness;

        struct Input {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo     = c.rgb;
            o.Metallic   = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha      = c.a;
        }
        ENDCG

        // --- Pass 2: Wireframe Overlay ---
        Pass
        {
            Name "WireOverlay"
            ZTest LEqual
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma target 4.0
            #pragma vertex   vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _WireColor;
            float  _WireThickness;
            float  _EnableEdge0, _EnableEdge1, _EnableEdge2;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2g
            {
                float4 pos : SV_POSITION;
            };

            struct g2f
            {
                float4 pos  : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            v2g vert (appdata v)
            {
                v2g o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            [maxvertexcount(3)]
            void geom (triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;
                o.pos = IN[0].pos; o.bary = float3(1,0,0); triStream.Append(o);
                o.pos = IN[1].pos; o.bary = float3(0,1,0); triStream.Append(o);
                o.pos = IN[2].pos; o.bary = float3(0,0,1); triStream.Append(o);
            }

            // Edge factor with per-edge toggle
            float edgeFactor (float3 bary)
            {
                float3 d = fwidth(bary);
                float3 a3 = smoothstep(float3(0,0,0), d * _WireThickness, bary);

                float e0 = (1.0 - a3.x) * _EnableEdge0; // edge opposite vertex 0
                float e1 = (1.0 - a3.y) * _EnableEdge1; // edge opposite vertex 1
                float e2 = (1.0 - a3.z) * _EnableEdge2; // edge opposite vertex 2

                return max(max(e0, e1), e2);
            }

            fixed4 frag (g2f i) : SV_Target
            {
                float ef = edgeFactor(i.bary);
                return float4(_WireColor.rgb, _WireColor.a * saturate(ef));
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
