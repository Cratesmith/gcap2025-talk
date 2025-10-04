using UnityEngine;

namespace AudioClusters
{
	public interface IClusterType
	{
	}
	
	public interface IClusterType<TPoint> : IClusterType 
		where TPoint:IPoint
	{
		IClusterTypeContainer<TPoint> CreateContainer();
	}

	public abstract partial class ClusterType<TSelf, TPoint> : ScriptableObject, IClusterType<TPoint>, ISerializationCallbackReceiver
		where TSelf:ClusterType<TSelf,TPoint>
		where TPoint:IPoint
	{
		[SerializeField]               Cluster m_ClusterPrefab;
		public Cluster ClusterPrefab => m_ClusterPrefab;

		[field: SerializeField] public float CaptureRadiusListenerAngle { get; private set; } = 30;
		public float CaptureRadiusListenerSinRatio { get; private set; }
		
		[field: SerializeField] public float CaptureRadiusMinAttenuationDistance { get; private set; } = 0;
        
		[field: SerializeField] public float CaptureRadiusMinChangeAngle { get; private set; } = 1;
		public float CaptureRadiusMinChangeSinRatio { get; private set; }
		
		[field: SerializeField] public int MaxClusters { get; private set; } = 10;
		
		[field: SerializeField] public int CullingDistance { get; private set; } = 100;

		[field: SerializeField] public int CullingDistanceSpareClusterCount { get; private set; } = 2;
		
		[field: SerializeField] public float CullingDistanceRestoreRateAngle { get; private set; } = 20f;
		
		public float CullingDistanceRestoreRateSinRatio { get; private set; }

		[field: SerializeField] public Color DebugColor { get; private set; } = Color.green;

		[field: SerializeField] public float PointBlendDuration { get; private set; } = 1.0f;
		[field: SerializeField] public float PointBlendDurationOutOfClustersMultiplier { get; private set; } = 0.5f;

		public IClusterTypeContainer<TPoint> CreateContainer() => new ClusterTypeContainer((TSelf)this);
		
		public void OnBeforeSerialize() {}
		public void OnAfterDeserialize()
		{
			CaptureRadiusListenerSinRatio = Mathf.Sin(CaptureRadiusListenerAngle * Mathf.Deg2Rad);
			CaptureRadiusMinChangeSinRatio = Mathf.Sin(CaptureRadiusMinChangeAngle * Mathf.Deg2Rad);
			CullingDistanceRestoreRateSinRatio = Mathf.Sin(CullingDistanceRestoreRateAngle * Mathf.Deg2Rad);
		}

		public override string ToString()
		{
			return name;
		}
	}
}