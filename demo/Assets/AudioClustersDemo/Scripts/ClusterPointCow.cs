using AudioClusters;
using UnityEngine;

namespace AudioClustersDemo
{
	public struct ClusterPointCow : IPoint
	{
		public int Id { get; set; }
		public Vector3 WorldPosition { get; set; }
		public float Agitation { get; set; }
		public float Speed { get; set; }
	}
}
