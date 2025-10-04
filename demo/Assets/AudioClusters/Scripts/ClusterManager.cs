using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AudioClusters
{
	/**
	 * Cluster manager
	 * Provides external API and manages ClusterTypeContainers for each kind of cluster in use.
	 */
	[DefaultExecutionOrder(100)]
	public class ClusterManager : MonoBehaviour
	{
		// todo: add support for multiple listereners
		// (all logic should performed based on closest listener, so doing this probably requires spatial storage of listeners and/or points & clusters)
		public Vector3 ListenerPosition { get; set; }
		public Quaternion ListenerRotation { get; set; }
		public Vector3 AttenuationPosition { get; set; }

		Dictionary<IClusterType, IClusterTypeContainer> m_Containers = new();
        
		private static ClusterManager s_Instance;

		public static ClusterManager Get()
		{
			if (!s_Instance && Application.isPlaying)
			{
				var go = new GameObject(nameof(ClusterManager));
				DontDestroyOnLoad(go);
				s_Instance = go.AddComponent<ClusterManager>();
			}
		
			return s_Instance;
		}

		private void LateUpdate()
		{
			UpdateContainers();
		}
		
		void UpdateContainers()
		{
			foreach (var pair in m_Containers)
			{
				pair.Value.ApplyChanges(new (Time.unscaledDeltaTime,
					ListenerPosition,
					ListenerRotation,
					AttenuationPosition));
			}
		}

		public void SetPoints<TPoint, T>(in ISource<TPoint> source, in T points, bool removeImmediately=false) 
			where T : IEnumerable<TPoint>
			where TPoint: IPoint
		{
			if (source == null)
			{
				return;
			}
			
			var clusterType = source.GetClusterType();
			if (clusterType == null)
			{
				return;
			}
			
			if (!m_Containers.TryGetValue(clusterType, out var container))
			{
				container = clusterType.CreateContainer();
				m_Containers.Add(clusterType, container);
			}
			
			((IClusterTypeContainer<TPoint>)container).SetPoints(source, points, removeImmediately);
		}
		
		public void AddPoints<TPoint, T>(in ISource<TPoint> source, in T points) 
			where T : IEnumerable<TPoint>
			where TPoint: IPoint
		{
			if (source == null)
			{
				return;
			}
			
			var clusterType = source.GetClusterType();
			if (clusterType == null)
			{
				return;
			}
			
			if (!m_Containers.TryGetValue(clusterType, out var container))
			{
				container = clusterType.CreateContainer();
				m_Containers.Add(clusterType, container);
			}
			
			((IClusterTypeContainer<TPoint>)container).AddPoints(source, points);
		}

		public void RemoveAllPoints<TPoint>(in ISource<TPoint> source, bool removeImmediately=false)
			where TPoint: IPoint
		{
			if (source == null)
			{
				return;
			}
			
			var clusterType = source.GetClusterType();
			if (clusterType == null)
			{
				return;
			}

			if (m_Containers.TryGetValue(clusterType, out var container))
			{
				((IClusterTypeContainer<TPoint>)container).RemoveAllPoints(source, removeImmediately);
			}
		}
		
		public void RemovePoints<TPointData, T>(ISource<TPointData> source, T pointIds) 
			where T : IEnumerable<int>
			where TPointData: IPoint
		{
			if (source == null)
			{
				return;
			}
			
			var clusterType = source.GetClusterType();
			if (clusterType == null)
			{
				return;
			}
			
			if (m_Containers.TryGetValue(clusterType, out var container))
			{
				((IClusterTypeContainer<TPointData>)container).RemovePoints(source, pointIds);
			}
		}

		void OnDrawGizmos()
		{
			foreach (var container in m_Containers)
			{
				container.Value.OnDrawGizmos();
			}
		}
	}
}
