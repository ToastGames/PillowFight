using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XboxCtrlrInput;

public class PlayerController : MonoBehaviour {

	public XboxController controller;
	public GameObject playerGuide;		//used to keep the player upright (sort of)
	public float moveSpeed = 0.1f;
	public float minRotationThreshold = 0.1f;
	public float minMovementThreshold = 0.1f;
	public float triggerThreshold = 0.1f;
	//public Vector3 attackBoost;
	public float attackBoost;
	public float maxAngularVelocity;
	public float attackTimer;
	public float playerStandUpTime = 1.0f;
	public float standUpSpeed = 2.0f;
	public float groundingSpeed = 2.0f;
	public float attackForceMultiplier;

	//private SphereCollider faceZone;
	private Rigidbody rb;
	private Vector3 previousRotationDirection = Vector3.forward;
	private Vector3 resetPos;
	private Quaternion resetRot;
	private float timeSinceAttack;
	private float standingUpTimer = 0.0f;
	private bool attackStarted = false;
	private bool standingStarted = false;
	private bool canAttackAgain = true;


	void Start ()
	{
		rb = GetComponent<Rigidbody> ();
		rb.maxAngularVelocity = maxAngularVelocity;
		resetPos = transform.position;
		resetRot = transform.rotation;
	}



	void Update () {

		// only rotate the player if the right analogue stick is touched
		if ((XCI.GetAxis (XboxAxis.RightStickX, controller) > minRotationThreshold) || (XCI.GetAxis (XboxAxis.RightStickY, controller) > minRotationThreshold))
			RotatePlayer ();
		else
			standingStarted = true;
		
		MovePlayer ();

		if ((XCI.GetAxis (XboxAxis.RightTrigger, controller) > triggerThreshold) && (attackStarted == false) && (canAttackAgain == true))
			SpinAttack (1);
		else if ((XCI.GetAxis (XboxAxis.RightTrigger, controller) < triggerThreshold) && (canAttackAgain == false))
			canAttackAgain = true;
		
//		if ((XCI.GetAxis (XboxAxis.LeftTrigger, controller) > triggerThreshold) && (attackStarted == false) && (canAttackAgain == true))
//			SpinAttack (-1);
//		else if ((XCI.GetAxis (XboxAxis.LeftTrigger, controller) < triggerThreshold) && (canAttackAgain == false))
//			canAttackAgain = true;

		AttackCooldown ();
		UpdatePlayerGuide ();

		//CheckHit ();

		// reset player if back pressed
		if (XCI.GetButton(XboxButton.Back , controller) == true)
			ResetPlayer();
	}



	private void RotatePlayer()
	{
		// assign X and Y axiis from right stick on controller
		// to temporary variables rotateAxisX and rotateAxisZ
		// to assemble into rotation direction
		float rotateAxisX = XCI.GetAxis (XboxAxis.RightStickX, controller);
		float rotateAxisZ = XCI.GetAxis (XboxAxis.RightStickY, controller);

		// build rotation vector out of temp variables
		Vector3 directionVector = new Vector3 (rotateAxisX, 0, rotateAxisZ);

				// Debug draw rotation direction
				Debug.DrawLine (transform.position, transform.position + directionVector, Color.blue);

		// don't change direction if direction is less than some threshold value
		if (directionVector.magnitude < minRotationThreshold)
			directionVector = previousRotationDirection;

				// Debug draw rotation direction
				Debug.DrawLine (transform.position, transform.position + directionVector, Color.red);

		// normalise and apply rotation direction to transform of this object
		directionVector = directionVector.normalized;
		previousRotationDirection = directionVector;
		transform.rotation = Quaternion.LookRotation (directionVector);
	}



	private void MovePlayer()
	{
		// assign X and Y axiis from left stick on controller
		// to temporary variables axisX and axisZ
		// to assemble into move direction
		float axisX = XCI.GetAxis (XboxAxis.LeftStickX, controller);
		float axisZ = XCI.GetAxis (XboxAxis.LeftStickY, controller);

		// build movement vector out of temp variables
		Vector3 movement = new Vector3 (axisX, 0, axisZ);

		// don't move unless amount is greater than some threshold value
		// if movement ok, normalise and apply movement vector
		if (movement.magnitude > minMovementThreshold) {
			movement = movement.normalized;
			transform.position += movement * moveSpeed;
		} else {
			rb.velocity = Vector3.zero;

			if (attackStarted == false)
				rb.angularVelocity = Vector3.zero;

			// snap to Y = 0
			Vector3 newPos = new Vector3 (transform.position.x, resetPos.y, transform.position.z);
			// lerp to Y = 0
			transform.position = Vector3.Lerp(transform.position, newPos, 1/Time.deltaTime * groundingSpeed);
		}
	}



	private void SpinAttack(int attackDirection)
	{
		// spin the player around by adding a large boost to the rigid body's angular momentum
		rb.angularVelocity = transform.up * attackBoost * attackDirection;
		attackStarted = true;
		canAttackAgain = false;
	}



	private void AttackCooldown()
	{
		// so the player can't spam the attack button
		if (attackStarted == true)
			timeSinceAttack += Time.deltaTime;

		if (timeSinceAttack > attackTimer) {
			timeSinceAttack = 0;
			attackStarted = false;
			rb.angularVelocity = Vector3.zero;
		}
	}



	private void UpdatePlayerGuide()
	{
		playerGuide.transform.position = transform.position;

		if (standingStarted == true) {
			standingUpTimer += Time.deltaTime;

			//do some standing up!!!!!!!!!!!!!!!!!
			transform.localRotation = Quaternion.Slerp (transform.localRotation, playerGuide.transform.localRotation, standingUpTimer * standUpSpeed);
		}

		if (standingUpTimer > playerStandUpTime) {
			standingUpTimer = 0;
			standingStarted = false;
		}
		Debug.DrawLine (transform.position, transform.position + playerGuide.transform.up * 4.0f, Color.cyan);
		Debug.DrawLine (transform.position, transform.position + transform.up * 4.0f, Color.yellow);
	}



	private void ResetPlayer()
	{
		transform.position = resetPos;
		transform.rotation = resetRot;
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
	}



	//	This function and pretty much ALL calculations associated with it are fucked beyond belief... this code needs cleaning up like crazy... maybe even a revert

	void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "Pillow") {
			if (other.transform.parent.parent.gameObject != gameObject) {
				Vector3 tempVec = (transform.position - other.transform.parent.parent.transform.position);
				rb.AddForce (tempVec * attackForceMultiplier, ForceMode.Impulse);
			}
		}
	}
}
