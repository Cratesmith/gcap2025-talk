using System;
using UnityEngine;

namespace AudioClustersDemo
{
	public class FlockingEffector : MonoBehaviour
	{
		[SerializeField] AnimationCurve Attraction;
		[SerializeField] AnimationCurve Cohesion;
		[SerializeField] float          TeleportIfMovedDistance = 3.0f;
		Vector3                         m_PrevPosition;
		Vector3                         m_Velocity;

		void FixedUpdate()
		{
			var positionDiff = (transform.position - m_PrevPosition);
			m_Velocity = Time.deltaTime > Mathf.Epsilon && positionDiff.sqrMagnitude < (TeleportIfMovedDistance*TeleportIfMovedDistance) 
				? positionDiff/Time.deltaTime 
				: Vector3.zero;
			m_PrevPosition = transform.position;
		}

		public Vector3 GetFlockingForce(GameObject forObject)
		{
			var actorTransform = forObject.gameObject.transform;
			var diff = transform.position - actorTransform.position;
			var dist = diff.magnitude;
			var dir = dist > Mathf.Epsilon ? diff/dist : actorTransform.forward;

			
			
			var force = Vector3.zero; 
			force += Attraction.Evaluate(dist) * dir;
			force += Cohesion.Evaluate(dist) * m_Velocity;
			return force;
		}
	}
}
