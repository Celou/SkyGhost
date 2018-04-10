using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollectBox : MonoBehaviour {

	public PlayerHealth healthScript;

	public void GetExtraLife()
	{
		healthScript.healthPoints++;
	}
}
