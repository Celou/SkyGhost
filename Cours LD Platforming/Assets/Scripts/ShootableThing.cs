using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootableThing : MonoBehaviour {

	public int healthPoints = 1;
	public float invincibilityTimeWhenHurt = 0.1f;
	public GameObject objectToDestroyWhenDead;

	private float invincibilityTimestamp;

	void Start()
	{
		if (invincibilityTimeWhenHurt <= 0)
			invincibilityTimeWhenHurt = 1f;
	}

	public void OnTriggerEnter2D(Collider2D other)
	{
		if (Time.time - invincibilityTimestamp < invincibilityTimeWhenHurt)
			return;

		Destroy(other.gameObject);

		invincibilityTimestamp = Time.time;

		healthPoints--;

		if (healthPoints == 0)
			Destroy(objectToDestroyWhenDead);
	}
}
