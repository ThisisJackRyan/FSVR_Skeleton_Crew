using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LivingValkyrie.Util {

	/// <summary>
	/// Author: Matt Gipson
	/// Contact: Deadwynn@gmail.com
	/// Domain: www.livingvalkyrie.net
	/// 
	/// Description: PathCreator
	/// </summary>
	public class PathCreator : MonoBehaviour {
		#region Fields

		[HideInInspector]
		public Path path;

		public Color anchorColor = Color.blue;
		public Color controlColor = Color.red;
		public Color segmentColor = Color.green;
		public Color selectedSegmentColor = Color.red;
		public Color controlLineColor = Color.black;

		public float anchorDiameter = 0.1f;
		public float controlDiameter = 0.1f;
		public float controlLineWidth = 2;

		public bool displayControlPoints = true;

		#endregion

		public void CreatePath() {
			path = new Path(transform.position);
		}

		void Reset() {
			CreatePath();
		}
	}
}