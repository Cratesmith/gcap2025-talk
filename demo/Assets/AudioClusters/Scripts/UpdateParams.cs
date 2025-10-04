using UnityEngine;

namespace AudioClusters
{
	public struct UpdateParams
	{
		public float RealDeltaTime { get; }
		public Vector3 ListenerPosition { get; }
		public Quaternion ListenerRotation { get; }
		public Vector3 AttenuationPosition { get; }
	
		public UpdateParams(float realDeltaTime, Vector3 listenerPosition, Quaternion listenerRotation, Vector3 attenuationPosition)
		{
			RealDeltaTime = realDeltaTime;
			ListenerPosition = listenerPosition;
			ListenerRotation = listenerRotation;
			AttenuationPosition = attenuationPosition;
		}
        
		public readonly bool ListenerEqual(in UpdateParams other)
		{
			return AttenuationPosition.Equals(other.AttenuationPosition) 
			       && ListenerPosition.Equals(other.ListenerPosition)
			       && ListenerRotation.Equals(other.ListenerRotation);
		}

		
		public readonly float GetCullingDistanceSq(in Bounds bounds)
		{
			return bounds.SqrDistance(AttenuationPosition);
		}
		public readonly float GetCullingDistanceSq(in Vector3 position)
		{
			return (AttenuationPosition - position).sqrMagnitude;
		}

		public readonly float CalcMinDistanceTo(in Vector3 position, float minAttenuationDistance = 0)
		{
			var listenerDistSq = (position - ListenerPosition).sqrMagnitude;
			float attenuationDistSq = Mathf.Max((position - AttenuationPosition).sqrMagnitude, minAttenuationDistance*minAttenuationDistance);
			float minDistance = Mathf.Sqrt(Mathf.Min(listenerDistSq, attenuationDistSq));
			return minDistance;
		}
	}
}
