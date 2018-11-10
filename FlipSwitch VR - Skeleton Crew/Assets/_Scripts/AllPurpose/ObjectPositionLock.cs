using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class ObjectPositionLock : MonoBehaviour
{
    public GameObject posPoint;
	public Vector3 posOffset;
	public Quaternion rotOffset;
	public bool positionOnly = false;

	void OnEnable() {
#if UNITY_2017_1_OR_NEWER
		Application.onBeforeRender += OnBeforeRender;
#endif
	}


	void OnDisable() {
#if UNITY_2017_1_OR_NEWER
		Application.onBeforeRender -= OnBeforeRender;
#endif
	}

	void OnBeforeRender() {
		if (posPoint) {
			//if (!positionOnly) {
				transform.rotation = posPoint.transform.rotation * rotOffset;
			//}
            
            transform.position = posPoint.transform.position + (Vector3)(transform.localToWorldMatrix * posOffset);
			
            //transform.GetChild( 0 ).localPosition = Vector3.zero;
			//transform.GetChild( 0 ).localRotation = Quaternion.identity;
		}



		//print( transform.GetChild( 0 ).name );
	}
}
