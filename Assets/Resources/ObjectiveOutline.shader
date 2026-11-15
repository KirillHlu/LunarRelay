Shader "LunarRelay/ObjectiveOutline" {
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
  Pass {
   Cull Front ZWrite Off ZTest LEqual
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct Varyings { float4 positionCS:SV_POSITION; };
   Varyings vert(Attributes input) {
    Varyings output;
    float3 p=TransformObjectToWorld(input.positionOS.xyz);
    float width=clamp(distance(p,_WorldSpaceCameraPos)*.0045,.004,.12);
    output.positionCS=TransformWorldToHClip(p+TransformObjectToWorldNormal(input.normalOS)*width);
    return output;
   }
   half4 frag(Varyings input):SV_Target { return half4(.94,.72,.18,1); }
   ENDHLSL
  }
 }
}
