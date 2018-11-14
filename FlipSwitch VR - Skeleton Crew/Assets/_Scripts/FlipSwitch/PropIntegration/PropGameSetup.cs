using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class PropGameSetup : MonoBehaviour {

	//public string ipAddress = "190.168.1.105";
	//public int message;

	// Use this for initialization
	[Button]
	void StartHighWind () {
		PropController.Instance.ActivateProp(Prop.WindHigh);
	}

	[Button]
	void StopWind() {
		PropController.Instance.ActivateProp( Prop.WindOff );
	}

	[Button]
	void StartLowWind() {
		PropController.Instance.ActivateProp( Prop.WindLow );
	}
	[Button]
	void StartMedWind() {
		PropController.Instance.ActivateProp( Prop.WindMed );
	}
	[Button]
	void FireCannonLeftOne() {
		PropController.Instance.ActivateProp( Prop.CannonLeftOne );
	}
	[Button]
	void FireCannonLeftTwo() {
		PropController.Instance.ActivateProp( Prop.CannonLeftTwo );
	}
	[Button]
	void FireCannonLeftThree() {
		PropController.Instance.ActivateProp( Prop.CannonLeftThree );
	}
	[Button]
	void FireCannonRightOne() {
		PropController.Instance.ActivateProp( Prop.CannonRightOne );
	}
	[Button]
	void FireCannonRightTwo() {
		PropController.Instance.ActivateProp( Prop.CannonRightTwo );
	}
	[Button]
	void FireCannonRightThree() {
		PropController.Instance.ActivateProp( Prop.CannonRightThree );
	}

}
