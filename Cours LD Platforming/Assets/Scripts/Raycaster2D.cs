using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * The Raycaster must be part of the CharacterController.
 * It's the component that draws rays to return data from any BoxCollider2D hit while trying to move.
 */

// This script is part of the SlopeZones2D package.
// Author : Simon Albou <albou.simon@gmail.com>

// This struct stores data from the raycasts, just as a regular RaycastHit2D, with extra infos added.
public struct RaycastHitPlus
{
	public RaycastHit2D hit;
	public Vector2 ray;
	public Collider2D box;
}

public enum RayDirection { Left, Right, Up, Down }

[RequireComponent(typeof(BoxCollider2D))]
[AddComponentMenu("SlopeZones/Raycaster 2D")]
public class Raycaster2D : MonoBehaviour
{
	#region properties

	private struct RaycastOrigins
	{
		public Vector2 topRight;
		public Vector2 topLeft;
		public Vector2 bottomRight;
		public Vector2 bottomLeft;
	}

	// Number of rays
	public int horizontalRays = 10;
	public int verticalRays = 10;
	public bool drawRaysInEditor;

	// Calculated from number of rays
	private float verticalDistBetweenRays;
	private float horizontalDistBetweenRays;

	// Stores the result of our raycasts
	private RaycastHitPlus _outcome;

	// The object that stores our raycasts' origins.
	private RaycastOrigins _origins;

	// Which layers are actually collidable ?
	public LayerMask collidableLayers = 1;

	// Which layers contain platforms that only block you from above ? (always useful in platforming)
	public LayerMask oneWayLayers = 0;

	// internal references
	public BoxCollider2D box;
	public Transform self;

	// Distance between collider limit and the actual raycast starting point
	public float skinWidth = 0.02f;

	// Empty layer used for not be blocked by self during a move
	[Tooltip("The index of any layer that isn't used by your project.")]
	public int emptyLayerIndex = 31;
	
	#endregion

	#region preparative functions

	void Awake() { RecalculateDistanceBetweenRays(); }
	
	void RecalculateDistanceBetweenRays()
	{
		// figure out the distance between our rays in both directions

		// horizontal
		float realHeight = box.size.y * Mathf.Abs(self.localScale.y) - (2f * skinWidth);
		verticalDistBetweenRays = realHeight / (horizontalRays - 1);

		// vertical
		float realWidth = box.size.x * Mathf.Abs(self.localScale.x) - (2f * skinWidth);
		horizontalDistBetweenRays = realWidth / (verticalRays - 1);
		
	}

	#endregion

	private void RefreshRayOrigins()
	{
		Vector2 scaledColliderSize = new Vector2(box.size.x * Mathf.Abs(self.localScale.x), box.size.y * Mathf.Abs(self.localScale.y)) * 0.5f;
		Vector2 scaledCenter = new Vector2(box.offset.x * self.localScale.x, box.offset.y * self.localScale.y);

		Vector2 positionAsVector2 = new Vector2(self.position.x, self.position.y);

		_origins.topRight = positionAsVector2 + new Vector2(scaledCenter.x + scaledColliderSize.x, scaledCenter.y + scaledColliderSize.y);
		_origins.topRight.x -= skinWidth;
		_origins.topRight.y -= skinWidth;

		_origins.topLeft = positionAsVector2 + new Vector2(scaledCenter.x - scaledColliderSize.x, scaledCenter.y + scaledColliderSize.y);
		_origins.topLeft.x += skinWidth;
		_origins.topLeft.y -= skinWidth;

		_origins.bottomRight = positionAsVector2 + new Vector2(scaledCenter.x + scaledColliderSize.x, scaledCenter.y - scaledColliderSize.y);
		_origins.bottomRight.x -= skinWidth;
		_origins.bottomRight.y += skinWidth;

		_origins.bottomLeft = positionAsVector2 + new Vector2(scaledCenter.x - scaledColliderSize.x, scaledCenter.y - scaledColliderSize.y);
		_origins.bottomLeft.x += skinWidth;
		_origins.bottomLeft.y += skinWidth;

		RecalculateDistanceBetweenRays();
	}

	// Could be used for scale and rotation : cast rays in 4 directions simultaneously
	public RaycastHitPlus CastRaysEverywhere(float length)
	{
		RefreshRayOrigins();

		RaycastHitPlus result = CastRays(RayDirection.Left, length, true);
		if (result.box != null) return result;

		result = CastRays(RayDirection.Right, length, true);
		if (result.box != null) return result;

		result = CastRays(RayDirection.Up, length, true);
		if (result.box != null) return result;

		result = CastRays(RayDirection.Down, length, true);
		return result;
	}

	public RaycastHitPlus CastRays(RayDirection dir, float length, bool dontrefreshRayOrigins = false)
	{
		_outcome.box = null;

		if (!dontrefreshRayOrigins) RefreshRayOrigins();

		float rayDistance = Mathf.Abs(length) + skinWidth;
		Vector2 rayDirection = Vector2.zero;
		Vector2 initialRayOrigin = Vector2.zero;

		if(dir == RayDirection.Down)
		{
			rayDirection = Vector2.down;
			initialRayOrigin = _origins.bottomLeft;
		}
		if (dir == RayDirection.Up)
		{
			rayDirection = Vector2.up;
			initialRayOrigin = _origins.topLeft;
		}
		if (dir == RayDirection.Left)
		{
			rayDirection = Vector2.left;
			initialRayOrigin = _origins.bottomLeft;
		}
		if (dir == RayDirection.Right)
		{
			rayDirection = Vector2.right;
			initialRayOrigin = _origins.bottomRight;
		}

		// To sum it up : if direction is UP or DOWN, we cast rays from left to right.
		// If direction is LEFT or RIGHT, we cast rays from bottom to top.

		int totalRays = horizontalRays;
		bool isVerticalRay = (dir == RayDirection.Down || dir == RayDirection.Up);
		float horizontalDelta = isVerticalRay ? horizontalDistBetweenRays : 0;
		float verticalDelta = isVerticalRay ? 0 : verticalDistBetweenRays;

		for (var i = 0; i < totalRays; i++)
		{
			_outcome.ray = new Vector2(initialRayOrigin.x + i * horizontalDelta, initialRayOrigin.y + i * verticalDelta);

#if UNITY_EDITOR
			if (drawRaysInEditor) Debug.DrawRay(_outcome.ray, rayDirection, Color.red);
#endif

			string formerLayerName = LayerMask.LayerToName(gameObject.layer);
			gameObject.layer = emptyLayerIndex;
			_outcome.hit = Physics2D.Raycast(_outcome.ray, rayDirection, rayDistance, collidableLayers);
			gameObject.layer = LayerMask.NameToLayer(formerLayerName);

			if (_outcome.hit.collider != null)
			{
				// We don't want to hit triggers.
				if (_outcome.hit.collider.isTrigger) continue;

				// One-way platforms are negated whenever the controller isn't falling on them (i.e. going down)
				if (dir != RayDirection.Down)
					if ((1 << _outcome.hit.collider.gameObject.layer & oneWayLayers) > 0)
						continue;

				// If we passed all those checks, we're good.
				_outcome.box = _outcome.hit.collider;
				return _outcome;
			}
		}

		return _outcome;
	}
}
