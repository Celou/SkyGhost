using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBullet : MonoBehaviour {

	public float speed = 15;
	public float lifespan = 3;

	private float timeleft;
	Transform self;

	void Start ()
	{
		timeleft = lifespan;
		self = transform;
	}
	
	void Update ()
	{
		self.Translate(speed * Vector3.right * Time.deltaTime);

		timeleft -= Time.deltaTime;

		if (timeleft < 0) Destroy(gameObject);
	}

	void OnCollisionEnter2D(Collision2D coll)
	{
		Destroy(gameObject);
	}
}
