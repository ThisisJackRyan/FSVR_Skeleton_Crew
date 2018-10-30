using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hapticTester : MonoBehaviour {

	public float length = 1, gapLength = 0.25f;
	public int count = 2;
	[Range(500, 3999)]
	public ushort strength = 500;
	public bool allowTest = false;
	void Update() {
		if (allowTest) {

			if (Controller.LeftController.GetPressDown(Controller.Trigger)) {
				//StartHapticVibration(Controller.LeftController, length, strength);
				StartCoroutine(LongVibration(count, length, gapLength, strength));
			}

			if (Controller.LeftController.GetPressDown(Controller.Grip)) {
				StopHapticVibration(Controller.LeftController);
			}
		}
	}
	////////////////////////////////////////
	//
	// Haptic Functions

	protected Dictionary<SteamVR_Controller.Device, Coroutine> activeHapticCoroutines = new Dictionary<SteamVR_Controller.Device, Coroutine>();

	public void StartHapticVibration(SteamVR_Controller.Device device, float length, float strength) {

		if (activeHapticCoroutines.ContainsKey(device)) {
			Debug.Log("This device is already vibrating");
			return;
		}

		Coroutine coroutine = StartCoroutine(StartHapticVibrationCoroutine(device, length, strength));
		activeHapticCoroutines.Add(device, coroutine);

	}

	public void StopHapticVibration(SteamVR_Controller.Device device) {

		if (!activeHapticCoroutines.ContainsKey(device)) {
			Debug.Log("Could not find this device");
			return;
		}
		StopCoroutine(activeHapticCoroutines[device]);
		activeHapticCoroutines.Remove(device);
	}

	protected IEnumerator StartHapticVibrationCoroutine(SteamVR_Controller.Device device, float length, float strength) {

		for (float i = 0; i < length; i += Time.deltaTime) {
			device.TriggerHapticPulse((ushort)Mathf.Lerp(0, 3999, strength));
			yield return null;
		}

		activeHapticCoroutines.Remove(device);
	}

	IEnumerator LongVibration(float length, float strength) {
		for (float i = 0; i < length; i += Time.deltaTime) {

			Controller.LeftController.TriggerHapticPulse((ushort)Mathf.Lerp(0, 3999, strength));
			yield return null;
		}
	}

	//vibrationCount is how many vibrations
	//vibrationLength is how long each vibration should go for
	//gapLength is how long to wait between vibrations
	//strength is vibration strength from 0-1
	IEnumerator LongVibration(int vibrationCount, float vibrationLength, float gapLength, float strength) {
		strength = Mathf.Clamp01(strength);
		for (int i = 0; i < vibrationCount; i++) {
			if (i != 0) yield return new WaitForSeconds(gapLength);
			yield return StartCoroutine(LongVibration(vibrationLength, strength));
		}
	}

}
