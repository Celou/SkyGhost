using UnityEngine;
using System;
using System.Collections.Generic;

/**
 * The Charactercontroller2D script allows a GameObject to move in a 2D space.
 * It works exclusively with a BoxCollider2D, supports scale, slopes, and even stepping on another CharacterController2D which is moving.
 * Just call Charactercontroller2D.Move(Vector2) in your own script, and it'll be functional.
 * You also have access to three collision events : one for any collision, one for horizontal collisions, one for vertical collisions.
 */

// This script is part of the SlopeZones2D package.
// Author : Simon Albou <albou.simon@gmail.com>

[RequireComponent(typeof(Raycaster2D))]
[AddComponentMenu("SlopeZones/Character Controller 2D")]
public class CharacterController2D : MonoBehaviour
{
	// Collision Flags
	public class CollisionFlags2D
	{
		public bool right;
		public bool left;
		public bool up;
		public bool down;
		
		public void ResetFlags() { right = left = up = down = false; }
	}

	#region properties

	public Transform self, graphics;
	public BoxCollider2D box;
	public Raycaster2D raycaster;

	[System.NonSerialized]
	public CollisionFlags2D collisionFlags = new CollisionFlags2D();
	public bool isGrounded { get { return collisionFlags.down; } }

	private float skinWidth;

	public AnimationCurve slopeSpeedMultiplier = AnimationCurve.EaseInOut(0, 1, 1, 0);

	// If we land on something that moves, we must be able to move along : (unless stated otherwise by setting canBeCarried to false)
	public bool canBeCarried;
	[System.NonSerialized]
	public CharacterController2D parent;
	[System.NonSerialized]
	public List<CharacterController2D> children;
	
	/**
	// Everything SlopeZone-related goes here.
	[System.NonSerialized]
	public SlopeZone ground, ceiling;
	[System.NonSerialized]
	public List<SlopeZone> groundTriggers, ceilingTriggers;
	[System.NonSerialized]
	public bool switchCeiling, switchGround;
	[System.NonSerialized]
	public SlopeZone upGround, downGround, upCeiling, downCeiling;

	public bool disableVerticalPhysics;
	public bool ignoreSlopeZones;
	/**/

	// A smaller step size leads to more accurate movement on SlopeZones. Private, because not meant to be edited unless for reaaaaally small scale platforming games. Set to 0.1f in Awake().
	//private float stepSize;

	//private float angleWithGround;
	
	public event Action<RaycastHitPlus> onCollisionEnter, onHorizontalCollisionEnter, onVerticalCollisionEnter;

	#endregion

	#region toolbox

	public float bottom { get { return self.position.y + (box.offset.y - box.size.y * 0.5f) * self.localScale.y; } }
	public float top { get { return self.position.y + (box.offset.y + box.size.y * 0.5f) * self.localScale.y; } }

	public float bottomDelta { get { return self.position.y - bottom; } }
	public float topDelta { get { return self.position.y - top; } }

	/**
	// Useful just before teleporting the controller, for example.
	public void ResetSlopes()
	{
		switchCeiling = switchGround = false;
		ground = upGround = downGround = null;
		ceiling = upCeiling = downCeiling = null;
		groundTriggers.Clear();
		groundTriggers.TrimExcess();
		ceilingTriggers.Clear();
		ceilingTriggers.TrimExcess();
	}
	/**/

	#endregion

	#region Monobehaviour

	void Awake()
	{
		collisionFlags = new CollisionFlags2D();

		if(!self) self = GetComponent<Transform>();
		if(!box) box = GetComponent<BoxCollider2D>();

		//stepSize = 0.1f;

		//groundTriggers = new List<SlopeZone>();
		//ceilingTriggers = new List<SlopeZone>();
	}

	// Copy the raycaster's skinWidth into this component
	void Start()
	{
		skinWidth = raycaster.skinWidth;
		children = new List<CharacterController2D>();
	}

	/**
	// Find out our Y-position on the slope we're currently using
	float CalculateAltitude(float progress)
	{
		return ground.self.position.y + ground.verticalOffset * ground.self.localScale.y + ground.yScale * ground.slopeCurve.Evaluate(progress) * ground.self.localScale.y;
	}
	
	// Called at each grounded movement
	bool UpdateSlope()
	{
		float progress = (self.position.x - ground.xMin) / (ground.xMax - ground.xMin);
		progress = Mathf.Clamp01(progress);
		float yNeeded = CalculateAltitude(progress);
		
		// Manually set our Y-coordinate if we're grounded
		if(bottom < yNeeded)
		{
			self.position = new Vector3(self.position.x, yNeeded + bottomDelta, self.position.z);
			
			if(!collisionFlags.down)
			{
				if (onCollisionEnter != null) onCollisionEnter(new RaycastHitPlus());
				if (onVerticalCollisionEnter != null) onVerticalCollisionEnter(new RaycastHitPlus());
			}
			
			collisionFlags.down = true;
		}
		if(self.position.y == yNeeded + bottomDelta) collisionFlags.down = true;

		return bottom <= yNeeded;
	}
	
	// Get the angle of the current slope based on the altitude "just behind us" and "just in front of us".
	float FindAngle()
	{
		if(!ground || ignoreSlopeZones) return 0;
	
		float result = 0;

		float prevProgress = (self.position.x - stepSize - ground.xMin) / (ground.xMax - ground.xMin);
		prevProgress = Mathf.Clamp01(prevProgress);
		float yPrev = CalculateAltitude(prevProgress);

		float nextProgress = (self.position.x + stepSize - ground.xMin) / (ground.xMax - ground.xMin);
		nextProgress = Mathf.Clamp01(nextProgress);
		float yNext = CalculateAltitude(nextProgress);

		Vector2 before = new Vector2(self.position.x - stepSize, yPrev);
		Vector2 after = new Vector2(self.position.x + stepSize, yNext);
		Vector2 curSlope = after-before;
		
		// If we're descending, let's say the angle equals zero (unless we want a slight acceleration, which is rarely the case)
		if (Mathf.Sign(curSlope.y) != Mathf.Sign(graphics.localScale.x)) result = 0;
		// When ascending, we must get the angle anyway.
		else result = Vector2.Angle(Vector2.right, curSlope);
		
		return result;
	}
	
	// Called when we might hit a ceiling. Same logic as UpdateSlope().
	bool UpdateCeiling()
	{
		float progress = (self.position.x - ceiling.xMin) / (ceiling.xMax - ceiling.xMin);
		progress = Mathf.Clamp01(progress);

		float yNeeded = (ceiling.self.position.y + ceiling.verticalOffset - ceiling.zoneHeight) + ceiling.yScale * ceiling.ceilingCurve.Evaluate(progress);

		if (top > yNeeded)
		{
			self.position = new Vector3(self.position.x, yNeeded + topDelta, self.position.z);

			if(!collisionFlags.up)
			{
				if (onCollisionEnter != null) onCollisionEnter(new RaycastHitPlus());
				if (onVerticalCollisionEnter != null) onVerticalCollisionEnter(new RaycastHitPlus());
			}

			collisionFlags.up = true;
		}
		if (self.position.y == yNeeded + topDelta) collisionFlags.up = true;

		return top >= yNeeded;
	}
	/**/
	#endregion

	#region Movement

	private void MoveHorizontally(ref Vector3 deltaMovement)
	{
		float length = Mathf.Abs(deltaMovement.x) + skinWidth;
		bool isGoingRight = deltaMovement.x > 0;

		RayDirection dir = isGoingRight ? RayDirection.Right : RayDirection.Left;
		RaycastHitPlus result = raycaster.CastRays(dir, length);

		if (result.box != null)
		{
			// stop the horizontal movement as we bumped into something
			deltaMovement.x = 0;
			
			// flip the collision flags
			if (isGoingRight) collisionFlags.right = true;
			else collisionFlags.left = true;

			// And finally, call events
			if (onCollisionEnter != null) onCollisionEnter(result);
			if (onHorizontalCollisionEnter != null)	onHorizontalCollisionEnter (result);
		}
	}

	private void MoveVertically(ref Vector3 deltaMovement)
	{
		//if (disableVerticalPhysics) return;

		bool isGoingUp = deltaMovement.y > 0;
		float length = Mathf.Abs(deltaMovement.y) + skinWidth;

		RayDirection dir = isGoingUp ? RayDirection.Up : RayDirection.Down;
		RaycastHitPlus result = raycaster.CastRays(dir, length, false);

		if (result.box)
		{
			// set our new deltaMovement and recalculate the rayDistance taking it into account
			deltaMovement.y = result.hit.point.y - result.ray.y;

			// remember to remove the skinWidth from our deltaMovement
			if (isGoingUp)
			{
				deltaMovement.y -= skinWidth;
				collisionFlags.up = true;
			}
			else
			{
				deltaMovement.y += skinWidth;
				collisionFlags.down = true;	
			}
			
			// Watch out, the global event could be called twice (one for horizontal movement, one for vertical movement).
			if (onCollisionEnter != null)	onCollisionEnter(result);
			if (onVerticalCollisionEnter != null) onVerticalCollisionEnter (result);
		}
	}

	public void Move(Vector3 deltaMovement)
	{
		// Induce movement in the controllers this one is currently carrying, if any
		if (children.Count != 0)
			foreach(CharacterController2D cc in children)
				cc.Move(deltaMovement);

		// Reset the collision info
		//bool wasGroundedBeforeMoving = collisionFlags.down;
		collisionFlags.ResetFlags();

		// First we check movement in the horizontal dir
		if (deltaMovement.x != 0)
			MoveHorizontally(ref deltaMovement);
		
		// Next, check movement in the vertical dir
		if (deltaMovement.y != 0)
			MoveVertically(ref deltaMovement);

		// Before actually moving, we might need to apply a last modifier based on the slope angle
		/**
		if (wasGroundedBeforeMoving)
		{
			angleWithGround = FindAngle();
			deltaMovement.x *= slopeSpeedMultiplier.Evaluate(angleWithGround*0.0111111111f); // means *1/90, without having a division slow down our perfs
		}
		/**/

		// So, we move as planned, as if there wasn't any obstacle...
		self.Translate(deltaMovement, Space.World);

		/**
		// ...And afterwards we adjust the y-coordinate if needed.
		if (ceiling && !ignoreSlopeZones) UpdateCeiling();
		if (ground && !ignoreSlopeZones)
			if(deltaMovement.y <= 0 || !ground.optimizeJumpsFromBelow)
				UpdateSlope();
		/**/

		// IMPORTANT : 
		// If we don't have enough room between ground and ceiling, the ceiling won't be taken into account.
		// If so, using an extra box collider to block the controller is just fine.
	}

	#endregion

	#region Moving ground handling (carrying objects)

	void LateUpdate()
	{
		if (!canBeCarried) return;

		CharacterController2D possibleGround = FindGround();

		if (possibleGround == parent) return;

		StopFollowing();

		if (possibleGround != null)
		{
			parent = possibleGround;
			possibleGround.children.Add(this);
		}
	}

	CharacterController2D FindGround()
	{
		RaycastHitPlus hit = raycaster.CastRays(RayDirection.Down, 5 * skinWidth, true);

		if(hit.box == null) return null;

		return hit.box.GetComponent<CharacterController2D>();		
	}

	void StopFollowing()
	{
		if (!parent) return;
		parent.children.Remove(this);
		parent = null;
	}

	#endregion
}
