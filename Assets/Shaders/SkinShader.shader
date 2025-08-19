Shader "Custom/Clothing" {
    Properties{
        _Color("Skin Tone", Color) = (1,1,1,1)
        _MainTex("Clothing Texture", 2D) = "black" {}
    }

        SubShader{
            Tags { "RenderType" = "Opaque" }
            LOD 250

        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTex;
        sampler2D _DecalTex;
        fixed4 _Color;

        struct Input {
            float2 uv_MainTex;
            float2 uv_DecalTex;
        };

        void surf(Input IN, inout SurfaceOutput o) {
            fixed3 skinTone = _Color;
            fixed4 skin = tex2D(_MainTex, IN.uv_MainTex);

            skin.rgb = lerp(skinTone, skin.rgb, skin.a);

            o.Albedo = skin.rgb;
            o.Alpha = skin.a;
        }
        ENDCG
    }

        Fallback "Legacy Shaders/Diffuse"
}