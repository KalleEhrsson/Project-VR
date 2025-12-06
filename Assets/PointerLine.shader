Shader "Custom/LaserTube"
{
    Properties
    {
        _Color ("Laser Color", Color) = (0, 1, 1, 1)
        _Intensity ("Glow Intensity", Float) = 4.0
        _Radius ("Beam Radius", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _Color;
            float _Intensity;
            float _Radius;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // push vertices outward along normal to create tube thickness
                float3 normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 offset = normal * _Radius;

                float4 pos = mul(unity_ObjectToWorld, v.vertex + float4(offset, 0));
                o.pos = mul(UNITY_MATRIX_VP, pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // glowing additive color
                return _Color * _Intensity;
            }
            ENDCG
        }
    }
}
