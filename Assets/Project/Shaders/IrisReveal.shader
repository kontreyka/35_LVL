Shader "UI/IrisReveal"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Radius ("Radius", Range(0,1.2)) = 0
        _Softness ("Edge Softness", Range(0.001,0.1)) = 0.015
        _Aspect ("Aspect Ratio", Float) = 1.777777
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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

            fixed4 _Color;
            float _Radius;
            float _Softness;
            float _Aspect;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv - float2(0.5, 0.5);

                // Не даём кругу превратиться в овал на 16:9.
                p.x *= _Aspect;

                float distanceFromCenter = length(p);

                float mask = smoothstep(
                    _Radius,
                    _Radius + _Softness,
                    distanceFromCenter
                );

                fixed4 color = _Color;
                color.a *= mask;

                return color;
            }

            ENDCG
        }
    }
}