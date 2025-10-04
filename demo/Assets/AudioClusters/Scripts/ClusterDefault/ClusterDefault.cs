using UnityEditor;

namespace AudioClusters
{
	public class ClusterDefault : ClusterTypeDefault.Cluster
	{
	}
	
	
	#if UNITY_EDITOR
	[CustomEditor(typeof(ClusterDefault))]
	public class ClusterDefaultEditor : ClusterType<ClusterTypeDefault, PointDefault>.ClusterEditor
	{
	}
	#endif
}
