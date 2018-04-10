using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingThing : MonoBehaviour {

	public Transform[] trajectory;

	public Transform objectToMove;

	public float speed;

	int currentTarget;

	void OnDrawGizmos()
	{
		if (trajectory == null) return;
		if (trajectory.Length < 2) return;

		Gizmos.color = Color.yellow;
		for (int i = 0; i < trajectory.Length - 1; i++)
			Gizmos.DrawLine(trajectory[i].position, trajectory[i + 1].position);

		Gizmos.DrawLine(trajectory[0].position, trajectory[trajectory.Length - 1].position);

	}

	void Update ()
	{
		if (objectToMove == null) { enabled = false; return; }

		if (trajectory == null) { enabled = false; return; }
		if (trajectory.Length < 2) { enabled = false; return; }

		Vector3 moveDir = trajectory[currentTarget].position - objectToMove.position;
		objectToMove.Translate(speed * moveDir.normalized * Time.deltaTime);
		if (Vector3.Distance(objectToMove.position, trajectory[currentTarget].position) < 0.1f)
		{
			currentTarget++;
			if (currentTarget >= trajectory.Length)
				currentTarget = 0;
		}
	}
}
