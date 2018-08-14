using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingRock : MonoBehaviour {

	public Transform rock;
	public GameObject spellPattern;
	public float minTimeToFall = 1.5f, maxTimeToFall = 5f, raiseFallSpeed = 2f;
	float timeToFall;
	float currentLerpTime;
	float moveDistance = 10f;

	Vector3 startPos;
	Vector3 endPos;

	// Use this for initialization
	void Start() {
		timeToFall = Random.Range(minTimeToFall, maxTimeToFall);
	}

	private void OnTriggerStay(Collider other) {
		print(other);
		if (other.transform.root.tag == "Player") {
			timeToFall -= Time.deltaTime;
		}

		if (timeToFall <= 0) {
			StopAllCoroutines();
			StartCoroutine("MoveRockDown");

			//show pattern here
			spellPattern.SetActive(true);
		}
	}

	public void RaiseRock() {
		StopAllCoroutines();
		StartCoroutine("MoveRockUp");
	}
	

	IEnumerator MoveRockUp() {
		startPos = rock.transform.position;
		endPos = transform.position;
		currentLerpTime = 0f;

		while (currentLerpTime < raiseFallSpeed) {
			//increment timer once per frame
			currentLerpTime += Time.deltaTime;
			if (currentLerpTime > raiseFallSpeed) {
				currentLerpTime = raiseFallSpeed;
			}

			//lerp!
			float perc = currentLerpTime / raiseFallSpeed;
			rock.transform.position = Vector3.Lerp(startPos, endPos, perc);
			yield return new WaitForFixedUpdate();
		}
	}

	IEnumerator MoveRockDown() {
		startPos = rock.transform.position;
		endPos = transform.position + transform.up * -moveDistance;
		currentLerpTime = 0f;

		while ( currentLerpTime < raiseFallSpeed ) {
			//increment timer once per frame
			currentLerpTime += Time.deltaTime;
			if ( currentLerpTime > raiseFallSpeed ) {
				currentLerpTime = raiseFallSpeed;
			}

			//lerp!
			float perc = currentLerpTime / raiseFallSpeed;
			rock.transform.position = Vector3.Lerp( startPos, endPos, perc );
			yield return new WaitForFixedUpdate();
		}
	}
}