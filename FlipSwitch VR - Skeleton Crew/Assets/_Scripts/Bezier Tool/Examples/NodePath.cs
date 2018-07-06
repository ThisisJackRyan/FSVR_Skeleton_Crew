using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LivingValkyrie.Util;
using Sirenix.OdinInspector;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: NodePath
/// </summary>
[RequireComponent(typeof(PathCreator))]
public class NodePath : MonoBehaviour {
	#region Fields

	public float spacing = .1f;
	public float resolution = 1;
	public GameObject toSpawn;

	public Transform[] Nodes {
		get { return nodes ?? (nodes = new Transform[0]);
		}
	}

	[HideInInspector,SerializeField]
	Transform[] nodes;

	#endregion

	[Button("Spawn Nodes")]
	void Spawn() {
		
		Vector2[] points = GetComponent<PathCreator>().path.CalculateEvenlySpacedPoints(spacing, resolution);
		nodes = new Transform[points.Length];
		int i = 0;
		foreach (var vector2 in points) {
			GameObject g = Instantiate( toSpawn , vector2, Quaternion.identity);
			//g.transform.position = vector2;
			//g.transform.localScale = Vector3.one * spacing * .5f;
			g.transform.parent = transform;
			g.name = "Node " + i;
			nodes[i] = g.transform;
			i++;

		}
	}

	[Button("Despawn nodes")]
	void Despawn() {
		for (int i = transform.childCount-1; i < transform.childCount && i >= 0; i--) {
			DestroyImmediate(transform.GetChild(i).gameObject);
		}
		nodes = null;
	}

	[Button("print array")]
	public void PrintStuff(){
		print(Nodes.Length);
	}
	
}