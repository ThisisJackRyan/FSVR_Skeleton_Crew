using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: SpaceTester
/// </summary>
public class SpaceTester : MonoBehaviour {
	#region Fields

	public bool recording = true;
	public bool reset = false;
	//public OVRInput.Controller con = OVRInput.Controller.RTouch;
	public GameObject prefab;
	int count;

	#endregion

	void Start() {
		if (reset) {
			PlayerPrefs.DeleteAll();
		} else {
			if (PlayerPrefs.GetString("Recorded", "nope") != "nope") {
				recording = false;
			}

		using (System.IO.StreamWriter s = new System.IO.StreamWriter(Application.persistentDataPath + "/boundaryCoords")) {
			for (int i = 0; i < Mathf.Infinity; i++) {
				string point =
					PlayerPrefs.GetString("point_" + i, "notFound");

				if (point == "notFound") {
					print("point_" + i + point);
					break;
				} else {
					//print(point);
					Instantiate(prefab, Vector3FromString(point), Quaternion.identity);
					print( Application.persistentDataPath + "/boundaryCoords" );
						s.WriteLine( point );
					}
				}
			}
		}
	}

	void Update() {
		if (recording) {

			if ( Controller.RightController.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger) ) {
				print( "triggered" );
				PlayerPrefs.SetString( "point_" + count, transform.position.ToString() );
				print( "point_" + count );

				Instantiate( prefab, transform.position, Quaternion.identity );
				PlayerPrefs.SetString( "Recorded", "yup" );
				PlayerPrefs.Save();

				count++;
			}
		}
	}

	Vector3 Vector3FromString(string s) {
		s = s.Replace(')', ',');
		s = s.Replace('(', ',');

		//print(s);
		string[] pieces = s.Split(',');

		Vector3 toReturn = new Vector3(float.Parse(pieces[1]), float.Parse(pieces[2]), float.Parse(pieces[3]));
		return toReturn;
	}
}