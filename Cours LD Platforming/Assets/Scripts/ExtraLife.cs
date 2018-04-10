using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtraLife : MonoBehaviour {

	public void OnTriggerEnter2D(Collider2D other)
	{
		other.GetComponent<PlayerCollectBox>().GetExtraLife();
		Destroy(gameObject);
	}
}
