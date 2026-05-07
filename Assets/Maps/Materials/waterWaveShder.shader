Shader "Custom/WaterWaveShader"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.1, 0.4, 0.7, 1)
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveScale ("Wave Scale", Float) = 5.0
        _WaveStrength ("Wave Strength", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            float _WaveSpeed;
            float _WaveScale;
            float _WaveStrength;

            v2f vert (appdata v)
            {
                v2f o;

                float wave = sin((v.vertex.x + _Time.y * _WaveSpeed) * _WaveScale)
                           * cos((v.vertex.z + _Time.y * _WaveSpeed) * _WaveScale)
                           * _WaveStrength;

                v.vertex.y += wave;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}