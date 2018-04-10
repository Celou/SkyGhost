using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour {

	public int healthPoints = 3;
	public float invincibilityTimeWhenHurt = 1f;

	private float invincibilityTimestamp;

	public SpriteRenderer playerSprite;

	void Start()
	{
		if (invincibilityTimeWhenHurt <= 0)
			invincibilityTimeWhenHurt = 1f;
	}

	// update transparency based on invincibility
	void Update()
	{
		playerSprite.color = new Color(playerSprite.color.r, playerSprite.color.g, playerSprite.color.b, (Time.time - invincibilityTimestamp)/invincibilityTimeWhenHurt);
	}

	public void OnTriggerEnter2D(Collider2D other)
	{
		if (Time.time - invincibilityTimestamp < invincibilityTimeWhenHurt)
			return;

		invincibilityTimestamp = Time.time;

		healthPoints--;

		if (healthPoints == 0)
			Destroy(transform.parent.gameObject);
	}
}
