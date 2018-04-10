using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playParticules : MonoBehaviour {

	public ParticleSystem myParticuleSystem;

	// Use this for initialization
	void Start () {

		myParticuleSystem.Play ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
