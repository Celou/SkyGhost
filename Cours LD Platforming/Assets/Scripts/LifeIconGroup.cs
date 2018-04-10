using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LifeIconGroup : MonoBehaviour {

	public Image[] lifeIcons;
	public PlayerHealth healthScript;

	void Update ()
	{
		for (int i = 0; i < lifeIcons.Length; i++)
			lifeIcons[i].enabled = i < healthScript.healthPoints;
	}
}
