using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticController : MonoBehaviour {


	public static HapticController instance;

	public void Start() {
		if (instance == null) {
			////print("is server, setting as instance");
			instance = this;
			DontDestroyOnLoad(gameObject);
		} else {
			////print("is server with instance, destroying");

			Destroy(this);
		}
	}

	protected Dictionary<SteamVR_Controller.Device, Coroutine> activeHapticCoroutines = new Dictionary<SteamVR_Controller.Device, Coroutine>();

	public void StartHapticVibration(SteamVR_Controller.Device device, float length, float strength) {

		if (activeHapticCoroutines.ContainsKey(device)) {
			Debug.Log("This device is already vibrating");
			return;
		}

		Coroutine coroutine = StartCoroutine(StartHapticVibrationCoroutine(device, length, strength));
		activeHapticCoroutines.Add(device, coroutine);

	}

	public void StartHapticVibrationPulse(SteamVR_Controller.Device device, int vibrationCount, float vibrationLength, float gapLength, float strength) {

		if (activeHapticCoroutines.ContainsKey(device)) {
			Debug.Log("This device is already vibrating");
			return;
		}

		Coroutine coroutine = StartCoroutine(LongVibrationPulse(device, vibrationCount, vibrationLength, gapLength, strength));
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
			//device.TriggerHapticPulse((ushort)strength);

			yield return null;
		}

		activeHapticCoroutines.Remove(device);
	}

	//vibrationCount is how many vibrations
	//vibrationLength is how long each vibration should go for
	//gapLength is how long to wait between vibrations
	//strength is vibration strength from 0-1
	IEnumerator LongVibrationPulse(SteamVR_Controller.Device device,int vibrationCount, float vibrationLength, float gapLength, float strength) {
		strength = Mathf.Clamp01(strength);
		for (int i = 0; i < vibrationCount; i++) {
			if (i != 0) yield return new WaitForSeconds(gapLength);
			yield return StartCoroutine(LongVibration(device, vibrationLength, strength));
		}

		activeHapticCoroutines.Remove(device);
	}

	IEnumerator LongVibration(SteamVR_Controller.Device device, float length, float strength) {
		for (float i = 0; i < length; i += Time.deltaTime) {

			device.TriggerHapticPulse((ushort)Mathf.Lerp(0, 3999, strength));
			yield return null;
		}
	}

	public static HapticEvent BurstHaptics = new HapticEvent {
		eventType = HapticEvent.HapticEventType.Standard,
		vibrationCount = 1,
		vibrationLength = 0.5f,
		gapLength = 0,
		strength = 0.5f
	};
}

[System.Serializable]
public struct HapticEvent{
	public enum HapticEventType {
		Standard, Pulse
	}

	public HapticEventType eventType;
	public int vibrationCount;
	public float vibrationLength;
	public float gapLength;
	[Range(0, 1)]

	public float strength;
}