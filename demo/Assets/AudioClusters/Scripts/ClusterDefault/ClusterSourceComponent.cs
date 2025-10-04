using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace AudioClusters
{
	public class ClusterSourceComponent<TClusterType, TPoint> : MonoBehaviour, ISource<TPoint>
		where TPoint : struct, IPoint
		where TClusterType : IClusterType<TPoint>
	{
		[SerializeField] TClusterType  m_ClusterType;
		[SerializeField] List<Vector3> m_Points;
		Bounds                         m_Bounds;
		
		void Update()
		{
			UpdatePoints();
		}

		void OnDestroy()
		{
			ClusterManager.Get().RemoveAllPoints(this);
		}

		void UpdatePoints()
		{
			m_Bounds = default;
			if (m_Points.Count > 0)
			{
				m_Bounds.center = transform.TransformPoint(m_Points[0]);
			}
			using (ListPool<TPoint>.Get(out var list)) 
			{
				for (int i = 0; i < m_Points.Count; i++)
				{
					var worldPosition = transform.TransformPoint(m_Points[i]);
					list.Add(new TPoint {Id = i, WorldPosition = worldPosition});
					m_Bounds.Encapsulate(worldPosition);
				}
				ClusterManager.Get().SetPoints(this, list);
			}
		}
		
		public Bounds GetCullingBounds() => m_Bounds;
		public IClusterType<TPoint> GetClusterType() => m_ClusterType;
	}
}
