Shader "UI/EyeClosingVignette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0,0,0,1)
        _Progress ("Closing Progress", Range(0,1)) = 0
        _Softness ("Edge Softness", Range(0.001,0.1)) = 0.018
        _Curvature ("Eyelid Curvature", Range(0,1.5)) = 0.62
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
            float _Progress;
            float _Softness;
            float _Curvature;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 eyePosition = (input.uv - 0.5) * 2.0;
                float easedProgress = smoothstep(0.0, 1.0, _Progress);
                float halfOpening = lerp(1.08, -0.08, easedProgress);
                float curvedEdge = _Curvature * easedProgress * eyePosition.x * eyePosition.x;
                float openingAtX = halfOpening - curvedEdge;
                float outsideEye = smoothstep(
                    -_Softness,
                    _Softness,
                    abs(eyePosition.y) - openingAtX
                );

                fixed4 color = _Color;
                color.a *= outsideEye;
                return color;
            }

            ENDCG
        }
    }
}
