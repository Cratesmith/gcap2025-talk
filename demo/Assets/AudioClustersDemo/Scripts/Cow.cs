using System.Collections.Generic;
using AudioClusters;
using UnityEngine;
using UnityEngine.Pool;

namespace AudioClustersDemo
{
	public class Cow : MonoBehaviour, ISource<ClusterPointCow>
	{
		[SerializeField] float          m_GravityScale          = 2.0f;
		[SerializeField] float          m_ForceToMoveSpeed      = 1.0f;
		[SerializeField] float          m_MaxMoveSpeed          = 2.0f;
		[SerializeField] float          m_TurnBlend             = 5.0f;
		[SerializeField] float          m_AgitationIncreaseRate = 1.0f;
		[SerializeField] float          m_AgitationDecreaseRate = 1.0f;
		[SerializeField] ClusterTypeCow m_CowClusterType;
		[SerializeField] float          m_MoveStartForce = 0.5f;
		[SerializeField] float          m_MoveStopForce = 0.1f;

		CharacterController       m_CharacterController;
		Animator                  m_Animator;
		HashSet<FlockingEffector> m_Effectors = new();
		Vector3                   m_DesiredDirection;
		Vector3                   m_Velocity;

		static readonly int AnimMoveSpeed = Animator.StringToHash("move_speed");
		float               m_Agitation   = 0.0f;
		bool                m_IsMoving;

		void Awake()
		{
			m_CharacterController = GetComponent<CharacterController>();
			m_Animator = GetComponent<Animator>();
		}

		void OnTriggerEnter(Collider other)
		{
			if (other.gameObject != gameObject && other.GetComponent<FlockingEffector>() is { } effector)
			{
				m_Effectors.Add(effector);
			}
		}

		void OnTriggerExit(Collider other)
		{
			if (other.gameObject != gameObject && other.GetComponent<FlockingEffector>() is { } effector)
			{
				m_Effectors.Add(effector);
			}
		}

		void FixedUpdate()
		{
			m_Effectors.RemoveWhere(x => x == null);
			Vector3 force = Vector3.zero;
			foreach (var effector in m_Effectors)
			{
				force += effector.GetFlockingForce(gameObject);
			}
			
			if (m_CharacterController.isGrounded)
			{
				if (m_Velocity.y < 0)
					m_Velocity.y = 0;
			}
			m_Velocity += Vector3.up * (Physics.gravity.y * m_GravityScale * Time.deltaTime);
			
			var forceScalar = force.magnitude;
			m_IsMoving = m_IsMoving ? forceScalar > m_MoveStopForce : forceScalar >= m_MoveStartForce;
			
			var desiredMove = m_IsMoving 
				? Vector3.ClampMagnitude(new Vector3(force.x, 0, force.z) * m_ForceToMoveSpeed, m_MaxMoveSpeed)
				: Vector3.zero;
			
			var move =  (desiredMove + m_Velocity) * Time.deltaTime;

			m_CharacterController.Move(move);
        
			if (desiredMove.sqrMagnitude > Mathf.Epsilon)
				m_DesiredDirection = desiredMove.normalized;
        
			if(m_DesiredDirection.sqrMagnitude > Mathf.Epsilon)
			{
				transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(m_DesiredDirection), Time.deltaTime * m_TurnBlend);
			}

			m_Animator.SetFloat(AnimMoveSpeed, Vector3.Dot(new Vector3(desiredMove.x, 0, desiredMove.z), transform.forward));

			var desiredAgitation = Mathf.Clamp01(forceScalar); 
			m_Agitation = Mathf.Lerp(m_Agitation, desiredAgitation, Time.deltaTime * (desiredAgitation > m_Agitation ? m_AgitationIncreaseRate:m_AgitationDecreaseRate));
			if (m_CowClusterType)
			{
				using var _ = ListPool<ClusterPointCow>.Get(out var list);
				list.Add(new ClusterPointCow
				{
					Id = 0,
					Agitation = m_Agitation, 
					Speed = Mathf.Clamp01(m_MaxMoveSpeed > Mathf.Epsilon ? desiredMove.magnitude/m_MaxMoveSpeed : 0.0f), 
					WorldPosition = transform.position
				});
				ClusterManager.Get().SetPoints(this, list);
			}
		}
		IClusterType<ClusterPointCow> ISource<ClusterPointCow>.GetClusterType() => m_CowClusterType;
		Bounds ISource<ClusterPointCow>.GetCullingBounds() => new(transform.position, Vector3.zero);
	}
}
