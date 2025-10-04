using UnityEngine;

namespace AudioClusters
{
	public static class IncrementalAverage
	{
		public static Vector3 MoveAverageMove(Vector3 average, int count, in Vector3 oldPosition, in Vector3 newPosition)
		{
			switch (count)
			{
				case 0: return average;
				case 1: return newPosition;
				default:
					average = MoveAverageRemove(average, count, oldPosition);
					return MoveAverageAdd(average, count-1, newPosition);
			}
		}

		public static Vector3 MoveAverageAdd(in Vector3 average, int count, in Vector3 position)
		{
			switch (count)
			{
				case 0:  return position;
				default: return (average*count + position) / (count + 1);
			}
		}
        
		public static Vector3 MoveAverageRemove(Vector3 average, int count, in Vector3 position)
		{
			return count <= 1 
				? average 
				: (average * count - position) / (count - 1);
		}
	}
}
