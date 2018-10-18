using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateLineTrajectory : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
/*
void UpdateTrajectory(Vector3 initialPosition, Vector3 initialVelocity, Vector3 gravity)
{
    int numSteps = 20; // for example
    float timeDelta = 1.0f / initialVelocity.magnitude; // for example

    LineRenderer lineRenderer = GetComponent<LineRenderer>();
    lineRenderer.SetVertexCount(numSteps);

    Vector3 position = initialPosition;
    Vector3 velocity = initialVelocity;
    for (int i = 0; i < numSteps; ++i)
    {
        lineRenderer.SetPosition(i, position);

        position += velocity * timeDelta + 0.5f * gravity * timeDelta * timeDelta;
        velocity += gravity * timeDelta;
    }
}*/