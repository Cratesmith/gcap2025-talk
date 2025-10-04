using UnityEngine;

namespace AudioClusters
{
	public interface IPoint
	{
		public int Id { get; set; }
		public Vector3 WorldPosition { get; set;  }
	}
}
