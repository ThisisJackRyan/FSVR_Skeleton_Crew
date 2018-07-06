using UnityEngine;

[CreateAssetMenu]
public class ControllerSettings : ScriptableObject {

	public Color activeColor = Color.green, inactiveColor = Color.blue, pulseColor = Color.yellow;
	public float range = 2.5f;
	public float lineWeight = 0.2f;
	public float pulseRate = 1.0f;
	public Material beam;

}
