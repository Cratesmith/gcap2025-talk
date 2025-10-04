using UnityEngine;

namespace AudioClusters
{
	public struct PointDefault : IPoint
	{
		public int Id { get; set; }
		public Vector3 WorldPosition { get; set; }
		public ulong GroupId { get; set;}
	}
}
