using UnityEngine;

namespace AudioClusters
{
	public interface ISource<TPoint> where TPoint:IPoint
	{
		IClusterType<TPoint> GetClusterType();

		Bounds GetCullingBounds();
	}
}