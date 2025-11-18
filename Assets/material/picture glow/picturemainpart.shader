Shader "Custom/UIPhotoGlowBlink"
{
    Properties
    {
        [PerRendererData] _MainTex ("Photo Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,0.2,0.8,1) // 更鲜艳的荧光色（增强视觉感知）
        _GlowWidth ("Glow Width", Range(1,10)) = 5 // 发光宽度从3→5（扩大发光范围）
        _GlowIntensity ("Base Glow Intensity", Range(1,5)) = 3 // 基础强度从2→3（提高基准亮度）
        _BlinkSpeed ("Blink Speed", Range(0.5,10)) = 1.2 // 呼吸速度从2→1.2（变慢，更接近真实呼吸节奏）
        _BlinkRange ("Blink Range", Range(0.5,5)) = 3 // 强度波动从1.5→3（核心！波动范围翻倍，亮暗差更明显）
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float4 _GlowColor;
            float _GlowWidth;
            float _GlowIntensity;
            float _BlinkSpeed;
            float _BlinkRange;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 采样原图颜色
                fixed4 mainCol = tex2D(_MainTex, i.uv) * i.color;
                if (mainCol.a < 0.1) discard; // 剔除透明背景

                // 边缘检测
                float glow = 0;
                int step = (int)_GlowWidth;
                for (int x = -step; x <= step; x++)
                {
                    for (int y = -step; y <= step; y++)
                    {
                        float2 offset = float2(x, y) * _MainTex_TexelSize.xy;
                        float alpha = tex2D(_MainTex, i.uv + offset).a;
                        glow += 1 - alpha;
                    }
                }
                glow /= (step * 2 + 1) * (step * 2 + 1);

                // 时间驱动闪烁：用正弦函数实现周期性强度波动（0~1→1~_BlinkRange）
                float blink = sin(_Time.y * _BlinkSpeed) * 0.5 + 0.5; // 转换到0~1范围
                blink = 1 + (blink * (_BlinkRange - 1)); // 映射到1~_BlinkRange波动

                // 混合闪烁发光色
                fixed4 glowCol = _GlowColor * glow * _GlowIntensity * blink;
                fixed4 finalCol = mainCol + glowCol;
                finalCol.a = mainCol.a;
                return finalCol;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}