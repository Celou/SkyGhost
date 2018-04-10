using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * This is not really required by the SlopeZones2D engine.
 * This script serves as a simple example of an input handler that could work with the CharacterController2D.
 * Feel free to edit it and add animation support, as it depends on your game's graphic needs !
 */

// This script is part of the SlopeZones2D package.
// Author : Simon Albou <albou.simon@gmail.com>


public static class KeyBinding
{
	public static KeyCode[] shootKeys = new KeyCode[] { KeyCode.LeftShift, KeyCode.JoystickButton1 } ;
	public static KeyCode[] jumpKeys = new KeyCode[] { KeyCode.Space, KeyCode.Z, KeyCode.JoystickButton0, KeyCode.UpArrow } ;
	public static KeyCode[] crouchKeys = new KeyCode[] { KeyCode.S, KeyCode.DownArrow };
	public static KeyCode[] leftKeys = new KeyCode[] { KeyCode.Q, KeyCode.A, KeyCode.LeftArrow };
	public static KeyCode[] rightKeys = new KeyCode[] { KeyCode.D, KeyCode.RightArrow };
	public static KeyCode[] upperKeys = new KeyCode[] { KeyCode.W, KeyCode.Z, KeyCode.UpArrow };
}

[RequireComponent(typeof(CharacterController2D))]
[AddComponentMenu("SlopeZones/Input Handler")]
public class InputHandler : MonoBehaviour
{
	#region References
	private CharacterController2D cc;
	[Header("References")]
	public Transform self;
	public Transform graphics;
	public GameObject bulletPrefab;
	public ParticleSystem myParticulesSysteme;
	//public Animator anim; // not used here because it's a simple example script. But it could be !
	#endregion

	#region Physic data
	[Header("Basics")]
	public float baseGravity;
	public float baseSpeed;
	private float currentGravity, currentSpeed;
	private Vector2 moveDir;

	[System.NonSerialized]
	public float accelerationStartTimeStamp, speedCurveModifier; // modifier due to slow acceleration or deceleration
	[Header("Acceleration")]
	public AnimationCurve accelerationCurve = AnimationCurve.Linear(0,1,1,1);
	public float accelerationTime;
			
	[Header("Jumping")]
	public int allowedJumps = 1;
	[Range(0, 5)]
	public float jumpShorteningWhenKeyReleased = 1;
	private int curAllowedJumps;
	public AnimationCurve gravityMultiplierDueToJump = AnimationCurve.EaseInOut(0, -1, 1, 1);
	private float timeSinceJump;
	private bool isJumping;

	[Header("Shooting")]
	public float shootCooldown = 0.3f;
	float curCooldown = 0;
	public float bulletSpeed = 15f;
	#endregion

	#region Animation-related
	[Header("Graphics")]
	public bool defaultLooksLeft;
	private bool facesRight; // we won't do anything here but flip the character to make it face left or right.
	// Add anything related to an Animator component here.
	#endregion

	#region blocking movement
	[System.NonSerialized]
	public bool inputInhibitedByPause = false; // Blocks input if some manager pauses the game
	[System.NonSerialized]
	public bool cantMoveDueToEvent = false; // Blocks input if some ingame event freezes the controls
	#endregion

	#region Inputs
	private KeyCode[] jumpKeys, crouchKeys, leftKeys, rightKeys, upperKeys, shootKeys;
	[System.NonSerialized] // public, so we can get them easily
	public bool inputJump, inputCrouch, inputLeft, inputRight, inputUp, inputUpDown, inputJumpDown, inputShoot;
	private bool xWasNotStrictlyPositive, xWasNotStrictlyNegative; // last frame data for computing horizontal acceleration

	// five unused vars so far, but could be :
	private bool goesLeft, goesRight; // these ones are used for animations. They're shortcuts for (inputLeft || Input.GetAxis("Horizontal")<0).
	private float horizontalAxis, verticalAxis;
	private bool verticalInputWithoutJump;
	#endregion

	#region Start

	void Start ()
	{
		LoadInput();

		facesRight = true;
		isJumping = false;
		currentSpeed = baseSpeed * Mathf.Abs(self.localScale.x);
		cc = GetComponent<CharacterController2D>();

		curAllowedJumps = allowedJumps;
		timeSinceJump = 0;

		speedCurveModifier = 1;
	}

	// Initializes the lists of available keys, so we don't use the static vars all the time.
	void LoadInput()
	{
		jumpKeys = KeyBinding.jumpKeys;
		crouchKeys = KeyBinding.crouchKeys;
		leftKeys = KeyBinding.leftKeys;
		rightKeys = KeyBinding.rightKeys;
		upperKeys = KeyBinding.upperKeys;
		shootKeys = KeyBinding.shootKeys;
	}

	#endregion
	
	#region Update
	
	void Update()
	{
		UpdateInput();

		UpdateShoot();
		UpdateMovement();
		UpdateAnimation();
	}

	// Resets all the inputs from last frame then check, our lists of available keys.
	void UpdateInput()
	{
		inputCrouch = false;
		inputLeft = false;
		inputRight = false;
		inputUp = false;
		inputUpDown = false;
		inputJump = false;
		inputJumpDown = false;
		inputShoot = false;

		horizontalAxis = 0f;
		verticalAxis = 0f;
		
		verticalInputWithoutJump = false;
		goesLeft = goesRight = false;
	
		if(inputInhibitedByPause) return;
	
		foreach(KeyCode key in crouchKeys)
		{
			if(Input.GetKey (key)) inputCrouch = true;
			else if(Input.GetAxis ("Vertical") < -0.5f) inputCrouch = true;
		}

		foreach(KeyCode key in leftKeys)
		{
			if(Input.GetKey (key)) inputLeft = true;
		}

		foreach(KeyCode key in rightKeys)
		{
			if(Input.GetKey (key)) inputRight = true;
		}

		foreach (KeyCode key in shootKeys)
		{
			if (Input.GetKey(key)) inputShoot = true;
		}

		foreach (KeyCode key in upperKeys)
		{
			if (Input.GetKeyDown(key)) inputUpDown = true;
			if (Input.GetKey(key)) inputUp = true;
		}

		foreach (KeyCode key in jumpKeys)
		{
			if (Input.GetKey(key))
			{
				inputJump = true;
				break;
			}
		}
		
		foreach (KeyCode key in jumpKeys)
		{
			if (Input.GetKeyDown(key))
			{
				inputJumpDown = true;
				break;
			}
		}
		
		if(inputLeft && !inputRight) horizontalAxis = -1f;
		else if (!inputLeft && inputRight) horizontalAxis = 1f;
		else horizontalAxis = Input.GetAxis("Horizontal");

		if(inputCrouch && !inputUp && !inputJump) verticalAxis = -1f;
		else if (!inputCrouch && (inputJump || inputUp)) verticalAxis = 1f;
		else verticalAxis = Input.GetAxis("Vertical");

		verticalInputWithoutJump = !inputJump && (inputCrouch || inputUp || Input.GetAxis("Vertical")!=0);

		goesLeft = inputLeft || Input.GetAxis("Horizontal")<0;
		goesRight = inputRight || Input.GetAxis("Horizontal")>0;
	}

	// Convert input into shooting
	void UpdateShoot()
	{
		if (inputInhibitedByPause) return;

		if (!inputShoot) return;

		curCooldown -= Time.deltaTime;
		if (curCooldown > 0) return;

		curCooldown = shootCooldown;
		GameObject go = GameObject.Instantiate(bulletPrefab, self.position, Quaternion.identity) as GameObject;
		PlayerBullet pb = go.GetComponent<PlayerBullet>();
		pb.speed = facesRight ? bulletSpeed : -bulletSpeed;
	}

	// Convert input into movement
	void UpdateMovement()
	{
		if(inputInhibitedByPause) return;

		// Y-movement :

		if(inputJumpDown) StartJumping();

		// If we release the key, we accelerate the fall, thus making a shorter jump.
		timeSinceJump += Time.deltaTime * (1+(inputJump ? 0 : jumpShorteningWhenKeyReleased));

		if(isJumping)
		{
			currentGravity = baseGravity * gravityMultiplierDueToJump.Evaluate(timeSinceJump) * -1.0f;

			// The jump stops if : either we touched the ground soon after the beginning of the jump...
			bool cond1 = cc.isGrounded && (timeSinceJump > gravityMultiplierDueToJump.keys[1].time);
			// ... Or the curve has reached its end.
			bool cond2 = timeSinceJump > gravityMultiplierDueToJump.keys[gravityMultiplierDueToJump.keys.Length-1].time;
			
			if(cond1 || cond2) isJumping = false;
		}
		else currentGravity = Mathf.Abs(baseGravity) * -1.0f;

		currentGravity *= Mathf.Abs(self.localScale.x); // this works provided localScale.x and .y are the same
		
		// X-movement :
		
		if(cantMoveDueToEvent) currentSpeed = 0;
		else
		{
			currentSpeed = baseSpeed * Mathf.Abs(self.localScale.x);

			if (xWasNotStrictlyPositive && horizontalAxis > 0) accelerationStartTimeStamp = Time.time;
			if (xWasNotStrictlyNegative && horizontalAxis < 0) accelerationStartTimeStamp = Time.time;

			if (accelerationTime > 0)
				speedCurveModifier = accelerationCurve.Evaluate(Mathf.Clamp01((Time.time - accelerationStartTimeStamp) / accelerationTime));
			else
				speedCurveModifier = accelerationCurve.Evaluate(0);

			// When not touching the ground : still smooth, but faster
			if (!cc.isGrounded)
			{
				speedCurveModifier += 1;
				speedCurveModifier *= 0.5f;
			}
		}

		// Final vector :

		moveDir.y = currentGravity;
		moveDir.x = currentSpeed * horizontalAxis * speedCurveModifier;
		
		// Applying final movement :
		cc.Move (moveDir * Time.deltaTime);

		// Final facing :
		if (moveDir.x > 0) facesRight = true;
		if (moveDir.x < 0) facesRight = false;
	}

	// Save some data for the next frame here
	void LateUpdate()
	{
		xWasNotStrictlyNegative = horizontalAxis >= 0;
		xWasNotStrictlyPositive = horizontalAxis <= 0;
	}

	// Everything related to graphics goes here.
	// Since we want this script to be a simple controller example, we won't do anything but flip the character.
	void UpdateAnimation()
	{
		if (inputInhibitedByPause) return;

		// A simple switch between idle and walking state.
		//anim.SetFloat("HorizontalAxis", horizontalAxis);

		if (horizontalAxis != 0)
		{
			float sign = Mathf.Sign(horizontalAxis) * (defaultLooksLeft ? -1 : 1);
			graphics.localScale = new Vector3(Mathf.Abs(graphics.localScale.x) * sign, graphics.localScale.y, graphics.localScale.z);
		}
	}
	
	#endregion
	
	#region Rest of the behaviour
	
	void StartJumping()
	{
		if(cc.isGrounded) curAllowedJumps = allowedJumps;

		
		//if(isJumping) return;
		//if(!cc.isGrounded) return;
		if(curAllowedJumps < 1) return;

		myParticulesSysteme.Play ();

		isJumping = true;
		curAllowedJumps--;
		timeSinceJump = 0;
	}

	public void FreezePlayer() { cantMoveDueToEvent = true; }

	public void UnfreezePlayer() { cantMoveDueToEvent = false; }

	#endregion

	#region toolbox

	public bool FacesRight() { return facesRight; }
	public bool GoesLeft() { return goesLeft; }
	public bool GoesRight() { return goesRight; }
	public bool VerticalInputWithoutJump() { return verticalInputWithoutJump; }
	public float GetHorizontalAxis() { return horizontalAxis; }
	public float GetVerticalAxis() { return verticalAxis; }

	#endregion

}
