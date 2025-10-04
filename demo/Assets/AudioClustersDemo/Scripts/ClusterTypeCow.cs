using AK.Wwise;
using AudioClusters;
using UnityEngine;
using UnityEngine.Serialization;

namespace AudioClustersDemo
{
	[CreateAssetMenu(fileName ="WwiseClusterTypeCow", menuName = "AudioClusters/ClusterTypes/Cow (ClusterPointCow)")]
	public class ClusterTypeCow : WwiseClusterType<ClusterTypeCow, ClusterPointCow>
	{
		[FormerlySerializedAs("AggrivationRtpc")] public RTPC AgitationRtpc;
		public  RTPC SpeedRtpc;
	}
}
