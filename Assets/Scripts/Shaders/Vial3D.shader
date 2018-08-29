﻿Shader "Unlit/Vial3D"
{
	Properties {
	    _Tint ("Tint", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_FillAmount ("Fill Amount", Range(-5, 5)) = 0.0
		[HideInInspector] _WobbleX ("WobbleX", Range(-1, 1)) = 0.0
		[HideInInspector] _WobbleZ ("WobbleZ", Range(-1, 1)) = 0.0
		_TopColor ("Top Color", Color) = (1,1,1,1)
		_FoamColor ("Foam Line Color", Color) = (1,1,1,1)
		_Rim ("Foam Line Width", Range(0,0.1)) = 0.0
		_RimColor ("Rim Color", Color) = (1,1,1,1)
		_RimPower ("Rim Power", Range(0,10)) = 0.0
		
		_UpDirection ("Up Direction", Vector) = (0,1,0,1)
	}
	
	SubShader {
		Tags { "Queue"="Geometry" "DisableBatching"="False" }

		Pass
		{
		    ZWrite On
		    Cull Off
		    AlphaToMask On
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 viewDir : COLOR;
				float3 normal : COLOR2;
				float fillEdge : TEXCOORD2;
			};

			sampler2D _MainTex;
			half _FillAmount, _WobbleX, _WobbleZ;
			float4 _TopColor, _RimColor, _FoamColor, _Tint;
			fixed _Rim, _RimPower;
			
			fixed4 _UpDirection;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				
				/*
				// world pos of vertex.
				// rotate around xy.
				float3 worldPosX = RotateAroundYInDegrees(float4(worldPos,0), 360);
				// rotate around xz.
				float3 worldPosZ = float3(worldPosX.y, worldPosX.z, worldPosX.x);
				// combine rotations with worldPos, based on sine wave from script.
				float3 worldPosAdjusted = worldPos + (worldPosX * _WobbleX) + (worldPosZ * _WobbleZ);
				// how high up the liquid is.
				*/
				//o.fillEdge = worldPosAdjusted.y + _FillAmount;
				
				// find vector origin in world space.
				fixed3 worldPos = mul(unity_ObjectToWorld, v.vertex);
				fixed3 worldOriginPos = mul(unity_ObjectToWorld, fixed4(0,0,0,1));
				fixed3 realWorld = worldPos - worldOriginPos;
				/*
				fixed3 worldPos;
				 worldPos.x = v.vertex.y;
				 worldPos.y = v.vertex.x;
				 worldPos.z = v.vertex.z;
				 */
				 
				fixed3 normalizedUp = normalize(_UpDirection.xyz);
				//normalizedUp.z = 0;
				//worldPos.z = 0;
				
				fixed worldPosX = worldPos.x * _WobbleX;
				fixed worldPosZ = worldPos.z * _WobbleZ;
				
				fixed likeness = dot(normalizedUp, worldPos + worldPosX + worldPosZ);
				o.fillEdge = likeness - _FillAmount;
				
				o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
				o.normal = v.normal;
				
				return o;
			}
			
			fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target {
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				
				/*
				if (i.fillEdge < _FillAmount) {
				    col = _FoamColor;
				}
				
				return col;
				*/
				
				// rim light
				/*
                float dotProduct = 1 - pow(dot(i.normal, i.viewDir), _RimPower);
                float4 RimResult = smoothstep(0.5, 1.0, dotProduct);
                RimResult *= _RimColor;
                */
 
                // foam edge
                fixed4 foam = (step(i.fillEdge, 0.5) - step(i.fillEdge, (0.5 - _Rim)))  ;
                fixed4 foamColored = foam * _FoamColor;
                // rest of the liquid
                fixed4 result = step(i.fillEdge, 0.5) - foam;
                fixed4 resultColored = result * col * _Tint;
                // both together, with the texture
                fixed4 finalResult = resultColored + foamColored;               
                //finalResult.rgb += RimResult;
 
                // color of backfaces/ top
                fixed4 topColor = _TopColor * (foam + result);
                
                //VFACE returns positive for front facing, negative for backfacing
                return facing > 0 ? finalResult : topColor;
			}
			
			ENDCG
		}
	}
}