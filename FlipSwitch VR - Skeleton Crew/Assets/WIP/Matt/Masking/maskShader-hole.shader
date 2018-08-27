Shader "Masked/Diffuse Masked" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
		SubShader{
		// This shader does the same thing as the Diffuse shader, but after masks
		// and before transparent things
		Tags{ "Queue" = "Geometry+20" }
		UsePass "Diffuse/BASE"
		UsePass "Diffuse/PPL"
	}
		FallBack "Diffuse", 1
}