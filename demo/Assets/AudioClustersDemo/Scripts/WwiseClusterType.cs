using AK.Wwise;
using AudioClusters;
using UnityEditor;

namespace AudioClustersDemo
{
	public class WwiseClusterType<TSelf, TPoint> : ClusterType<TSelf, TPoint> 
		where TSelf:WwiseClusterType<TSelf, TPoint>
		where TPoint:IPoint
	{
		public Event ClusterAkEvent;
		public RTPC PointCountRtpc;
	}
}
