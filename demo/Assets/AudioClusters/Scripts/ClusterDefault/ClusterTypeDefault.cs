using UnityEditor;
using UnityEngine;

namespace AudioClusters
{
	[CreateAssetMenu(fileName ="ClusterTypeDefault", menuName = "AudioClusters/ClusterTypes/Default")]
	public class ClusterTypeDefault : ClusterType<ClusterTypeDefault, PointDefault>
	{
	}
}
