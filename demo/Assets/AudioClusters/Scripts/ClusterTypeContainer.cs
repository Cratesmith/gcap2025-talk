// #define LOG_POINT_REFRESH_POINTS_POINTS
// #define LOG_SPAWN_CLUSTER
// #define DEBUGDRAW_MERGE_CUSTE
// #define LOG_DESPAWN_CLUSTER
// #define DISABLE_CLUSTER_BLENDS
// #define DISABLE_DYNAMIC_CULLING_DISTANCE

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace AudioClusters
{
	public interface IClusterTypeContainer
	{
		void ApplyChanges(in UpdateParams updateParams);
		void OnDrawGizmos();
	}

	public interface IClusterTypeContainer<TPoint> : IClusterTypeContainer 
		where TPoint:IPoint
	{
		public void SetPoints<T>(in ISource<TPoint> source, in T points, bool removeImmediately = false)
			where T : IEnumerable<TPoint>;
		
		public void AddPoints<T>(in ISource<TPoint> source, in T points)
			where T : IEnumerable<TPoint>;

		public void RemovePoints<T>(in ISource<TPoint> source, in T points, bool removeImmediately = false)
			where T : IEnumerable<int>;

		public void RemoveAllPoints(in ISource<TPoint> source, bool removeImmediately = false);
	}

	/// <summary>
	/// Maages points, sources & clusters for a single cluster type.
	/// </summary>
	public abstract partial class ClusterType<TSelf, TPoint>
	{
		public class ClusterTypeContainer : IClusterTypeContainer<TPoint>
		{
			public TSelf Type { get; }

			// todo: pool unused clusters

			readonly HashSet<Cluster>   m_Clusters        = new(); // todo: move into ClusterGroupContainer
			readonly Queue<Cluster>     m_PooledClusters  = new();
			readonly HashSet<ClusterId> m_ChangedClusters = new(); // todo: move into ClusterGroupContainer

			readonly Dictionary<ISource<TPoint>, SourceData> m_SourceDatas   = new(); // todo: move into ClusterGroupContainer
			readonly HashSet<SourceData>                     m_ActiveSources = new();

			readonly Queue<PointData> m_RefreshSourcesPointsQueue = new();
			readonly Queue<PointData> m_RefreshPointsQueue        = new();
			readonly Queue<ClusterId> m_MergeClustersQueue        = new();
			private  Queue<PointData> m_RefreshWeightsQueue       = new();

			float                m_CullingDistance;
			private UpdateParams m_UpdateParams;
			int                  m_NextClusterId        = -1;
			bool                 m_FailedToSpawnCluster = false; 

			const float 		smallNumber = 1.0e-6f;
			
			
			internal ClusterTypeContainer(TSelf type)
			{
				Type = type;
				m_CullingDistance = Type.CullingDistance;
			}

			bool GetOrAddSourceData(in ISource<TPoint> source, out SourceData sourceData)
			{
				if (source == null)
				{
					sourceData = null;
					return false;
				}

				if (!m_SourceDatas.TryGetValue(source, out sourceData))
				{
					sourceData = new SourceData(source);
					sourceData.OnPointChanged += HandleSourcePointChanged;
					sourceData.OnPointClusterChanged += HandlePointClusterChanged; 
					m_SourceDatas.Add(source, sourceData);
				}

				return true;
			}

			private void HandlePointClusterChanged(PointData point)
			{
				if(point.SetPointFlag(PointDataFlags.Blending, true))
				{
					m_RefreshWeightsQueue.Enqueue(point);
				}
			}

			void HandleClusterChanged(Cluster cluster)
			{
				if (m_ChangedClusters.Add(cluster))
				{
					m_MergeClustersQueue.Enqueue(cluster);
				}
			}

			void HandleSourcePointChanged(PointData pointData)
			{
				m_RefreshSourcesPointsQueue.Enqueue(pointData);
			}

			public void SetPoints<T>(in ISource<TPoint> source, in T setPoints, bool removeImmediately = false)
				where T : IEnumerable<TPoint>
			{
				if (!GetOrAddSourceData(source, out var sourceData))
					return;

				sourceData.SetPoints(setPoints, removeImmediately);
			}

			public void AddPoints<T>(in ISource<TPoint> source, in T addPoints) where T : IEnumerable<TPoint>
			{
				if (!GetOrAddSourceData(source, out var sourceData))
					return;

				sourceData.AddPoints(addPoints);
			}

			public void RemoveAllPoints(in ISource<TPoint> source, bool removeImmediately = false)
			{
				if (m_SourceDatas.TryGetValue(source, out var sourceData))
				{
					sourceData.RemoveAllPoints(removeImmediately);
				}
			}

			public void RemovePoints<T>(in ISource<TPoint> source, in T pointIds, bool removeImmediately = false)
				where T : IEnumerable<int>
			{
				if (m_SourceDatas.TryGetValue(source, out var sourceData))
				{
					sourceData.RemovePoints(pointIds, removeImmediately);
				}
			}

			public void ApplyChanges(in UpdateParams updateParams)
			{
				var prevUpdateParams = m_UpdateParams;
				m_UpdateParams = updateParams;

				// cache off update parameters (listener / attenuation positions) in each cluster
				foreach (var cluster in m_Clusters)
				{
					cluster.BeginUpdate(m_UpdateParams);
				}

				// todo: add debug controls to step each of these update stages independently
				RefreshSources();
				RefreshPoints(prevUpdateParams); // todo: move into ClusterGroupContainer
				MergeClusters();                 // todo: move into ClusterGroupContainer
				RefreshWeights();                // todo: move into ClusterGroupContainer
				RefreshClusters();               // todo: move into ClusterGroupContainer
				RefreshCullingDistance();
				m_FailedToSpawnCluster = false;

				m_ChangedClusters.Clear();
			}
			
			void RefreshCullingDistance()
			{
#if !DISABLE_DYNAMIC_CULLING_DISTANCE
				if (m_FailedToSpawnCluster)
				{
					var maxClusterCullingDist = 0.0f;
					foreach (var cluster in m_Clusters)
					{
						var clusterDistance = m_UpdateParams.CalcMinDistanceTo(cluster.transform.position, Type.CaptureRadiusMinAttenuationDistance) - cluster.GetPointsRadius();
						
						// abort and don't apply culling distance reduction if there are clusters that are blending out  
						if (cluster.Points.Count == 0 && clusterDistance > m_CullingDistance)
						{
							maxClusterCullingDist = -1.0f;
							break;
						}

						// otherwise find the furthest cluster we can remove
						if(clusterDistance > maxClusterCullingDist && clusterDistance < m_CullingDistance)
						{
							maxClusterCullingDist = clusterDistance;
						}	
					}

					if (maxClusterCullingDist > 0.0f)
					{
						m_CullingDistance = maxClusterCullingDist;
					}	
				} else if (m_CullingDistance < Type.CullingDistance 
				           && Type.MaxClusters-m_Clusters.Count > Type.CullingDistanceSpareClusterCount)
				{
					var radiusIncrease = Type.CullingDistance * Type.CullingDistanceRestoreRateSinRatio * m_UpdateParams.RealDeltaTime;
					m_CullingDistance = Mathf.Min(Type.CullingDistance, m_CullingDistance + radiusIncrease);
				}
#endif
			}

			void RefreshSources()
			{
				// todo: only refresh sources that have changed by:
				// - contents changing
				// - or by listener/attenuation positions changing (beyond a sin threshold vs max distance)
				foreach (KeyValuePair<ISource<TPoint>, SourceData> sourcePair in m_SourceDatas)
				{
					var sourceData = sourcePair.Value;
					if (sourceData == null)
						continue;

					if (sourceData.ApplyCulling(m_CullingDistance, m_UpdateParams))
						m_ActiveSources.Add(sourceData);
					else
						m_ActiveSources.Remove(sourceData);
				}

				while (m_RefreshSourcesPointsQueue.Count > 0)
				{
					var pointData = m_RefreshSourcesPointsQueue.Dequeue();
					pointData.SetPointFlag(PointDataFlags.Changed, false);
					if (!pointData.IsValid)
						continue;

					if (!pointData.IsActive)
					{
						if (pointData.Cluster)
						{
							pointData.SetCluster(null, false);
						}

						continue;
					}

					if (pointData.Cluster)
					{
						// re-add if point has changed to incrementally update position 
						pointData.Cluster.Get().AddOrUpdatePoint(pointData);
					}

					if (!m_RefreshPointsQueue.Contains(pointData))
					{
						m_RefreshPointsQueue.Enqueue(pointData);
					}
				}
			}

			void RefreshPoints(UpdateParams prevUpdateParams, int maxSteps = -1)
			{
				// if the listener or attenuation position has changed, attempt to update the radius of each active cluster
				// this may orphan points from the cluster, automaticlaly markeing them as chnged.
				if ((m_UpdateParams.AttenuationPosition != prevUpdateParams.AttenuationPosition) ||
				    (m_UpdateParams.ListenerPosition != prevUpdateParams.ListenerPosition))
				{
					foreach (var cluster in m_Clusters)
					{
						cluster.UpdateCaptureRadius();
					}
				}

				maxSteps = Mathf.Max(maxSteps, m_RefreshPointsQueue.Count);
				for (int step = 0; m_RefreshPointsQueue.Count > 0 && maxSteps - step != 0; step++)
				{
					var pointData = m_RefreshPointsQueue.Dequeue();

					pointData.SetPointFlag(PointDataFlags.Changed, false);

					if (!pointData.IsActive)
						continue;

					if (pointData.IsCulled)
					{
						if (pointData.Cluster)
						{
#if LOG_POINT_REFRESH_POINTS
						Debug.Log($"Point {pointData} Is culled. Removing from cluster {pointData.Cluster}", pointData.Source.Source as Object);
#endif
							pointData.SetCluster(null, false);
						}
						continue;
					}
					else if (!pointData.Cluster || !pointData.Cluster.Get().CanContainActivePoint(pointData))
					{
						bool reassigned = false;
						// if we're out of clusters, don't remove the point
						foreach (var cluster in m_Clusters)
						{
							if (cluster != pointData.Cluster && cluster.CanContainActivePoint(pointData))
							{
								var prevCluster = pointData.Cluster;
								pointData.SetCluster(cluster, false);
#if LOG_POINT_REFRESH_POINTS
								Debug.Log($"Point {pointData} re-assigned from cluster {prevCluster} to cluster {pointData.Cluster}", pointData.Source.Source as Object);
#endif
								reassigned = true;
								break;
							}
						}

						if(!reassigned) 
						{
							// the point is not valid for any cluster.
							// Spawn a new cluster if we can, or schedule the point to update next tick if we can'ta
							if (SpawnCluster() is {} newCluster)
							{
								if (newCluster)
								{
									pointData.SetCluster(newCluster, !pointData.Cluster.IsValid() || !pointData.Cluster.Get().IsPlaying);
#if LOG_POINT_REFRESH_POINTS
									Debug.Log($"Point {pointData} assigned to new cluster {pointData.Cluster}", pointData.Source.Source as Object);
#endif
								}
							} 
							else
							{
								m_RefreshPointsQueue.Enqueue(pointData);
#if LOG_POINT_REFRESH_POINTS
								Debug.LogWarning($"Point {pointData} awaiting available cluster.", pointData.Source.Source as Object);
#endif
							}
						}
					}
				}
			}

			private void RefreshWeights(int maxSteps = -1)
			{
				maxSteps = Mathf.Max(maxSteps, m_RefreshWeightsQueue.Count);
				for (var step = 0; m_RefreshWeightsQueue.Count > 0 && maxSteps - step != 0; step++)
				{
					var pointData = m_RefreshWeightsQueue.Dequeue();

					// defer weight updates on clusters that aren't playing yet
					if (pointData.Cluster && !pointData.Cluster.Get().IsPlaying)
					{
						m_RefreshWeightsQueue.Enqueue(pointData);
						continue;
					}
					
					// total weight from this point (can be less than 1.0f if blending in or out)
					var totalWeight = 0.0f;
					foreach (var weight in pointData.Weights)
					{
						totalWeight += weight.Value;
					}
					
					// find the current value of the blend
					// either the weight for the point's current cluster,
					// or 1.0 - totalWeight if blending out
					var blendWeight = pointData.Cluster
						? Mathf.Clamp01(pointData.Weights[pointData.Cluster])
						: Mathf.Clamp01(1.0f - totalWeight);

					
					// linearly update the blend value
					// todo: add in blend speed multiplier for rapid-cull situation
#if DISABLE_CLUSTER_BLENDS
					var blendDuration = 0.0f;
#else
					var blendDuration = m_CullingDistance >= Type.CullingDistance
						? Type.PointBlendDuration
						: Type.PointBlendDuration * Type.PointBlendDurationOutOfClustersMultiplier;
#endif					
					var t = blendDuration <= smallNumber 
						? 1.0f 
						: Mathf.Clamp01(m_UpdateParams.RealDeltaTime / (1.0f - blendWeight) / blendDuration );
					var newBlendValue = Mathf.Lerp(blendWeight, 1.0f, t);
					Assert.IsTrue(newBlendValue >= 0 && newBlendValue <= 1.0f);

					// if blending to a cluster, set it's weight to the blend value
					if (pointData.Cluster)
					{
						pointData.Weights[pointData.Cluster] = newBlendValue;
						pointData.Cluster.Get().AddOrUpdateWeight(pointData, newBlendValue);
					}

					using var _ = ListPool<ClusterId>.Get(out var removeWeights);
					using var __ = ListPool<KeyValuePair<ClusterId, float>>.Get(out var setWeights);
					if (newBlendValue >= 1.0f)
					{
						foreach (var cluster in pointData.Weights.Keys)
						{
							if (cluster == pointData.Cluster)
								continue;
							
							removeWeights.Add(cluster);
						}
					} else
					{
						var newTotal = 0.0f;
						var ratio = totalWeight > smallNumber 
							? (1.0f - newBlendValue) / totalWeight
							: 0.0f;
						foreach (var cluster in pointData.Weights.Keys)
						{
							if (cluster == pointData.Cluster)
								continue;

							var newWeight = Mathf.Clamp01(pointData.Weights[cluster] * ratio);
							if (newWeight > smallNumber && cluster.IsValid() && cluster.Get().IsPlaying)
							{
								newTotal += newWeight;
								setWeights.Add(new KeyValuePair<ClusterId, float>(cluster, newWeight));
							} else
							{
								removeWeights.Add(cluster);
							}
						}	
						
						if (newTotal < 0 || newTotal > 1.0f)
						{
							Debug.LogWarning($"RefeshWeights: {pointData} had total weight out of range: {totalWeight}", pointData.Source.Source as Object);
						}
					}
					
					foreach (var pair in setWeights)
					{
						var cluster = pair.Key;
						pointData.Weights[cluster] = pair.Value;
						if (cluster)
						{
							cluster.Get().AddOrUpdateWeight(pointData, pair.Value);
						}
					}

					foreach (var cluster in removeWeights)
					{
						pointData.Weights.Remove(cluster);
						if (cluster)
						{
							cluster.Get().RemoveWeight(pointData);
						}
					}

					if (newBlendValue >= 1.0f)
					{
						Assert.IsTrue(pointData.Weights.Count <= 1);
						Assert.IsTrue(pointData.Cluster || pointData.Weights.Count == 0);
						pointData.SetPointFlag(PointDataFlags.Blending, false);
					}

					if (pointData.GetPointFlag(PointDataFlags.Blending))
					{
						m_RefreshWeightsQueue.Enqueue(pointData);
					}
				}
			}

			Cluster SpawnCluster()
			{
				if (m_Clusters.Count < Type.MaxClusters && Type.ClusterPrefab)
				{
					Cluster newCluster = m_PooledClusters.Count > 0 
						? m_PooledClusters.Dequeue() 
						: Instantiate(Type.ClusterPrefab);
					newCluster.Init(Type, ++m_NextClusterId, m_UpdateParams);
					newCluster.OnClusterChanged += HandleClusterChanged;
					m_Clusters.Add(newCluster);
#if LOG_SPAWN_CLUSTER
					Debug.Log($"Spawned cluster {newCluster}", newCluster);
#endif
					return newCluster;
				}
				m_FailedToSpawnCluster = true;
				return null;
			}

			void DespawnCluster(Cluster cluster)
			{
				Assert.IsTrue(cluster);
				Assert.AreEqual(cluster.Type, Type);
#if LOG_DESPAWN_CLUSTER
				Debug.Log($"Despawning cluster {cluster}", cluster);
#endif
				cluster.SetPooled(true);
				m_Clusters.Remove(cluster);
				m_PooledClusters.Enqueue(cluster);
			}

			void MergeClusters(int maxSteps = -1)
			{
				// todo: merge overlapping clusters
				for(int step = 0; step!=maxSteps && m_MergeClustersQueue.Count > 0; ++step)
				{
					var clusterA = m_MergeClustersQueue.Dequeue();
					
					bool couldStillMerge = true;
					while (couldStillMerge)
					{
						couldStillMerge = false;
						using var _ = ListPool<ClusterId>.Get(out var removeImmediateClusters);
						
						foreach (var clusterB in m_Clusters)
						{
							if (!clusterA.IsValid() || clusterA.Get().Points.Count == 0)
								break;

							if (clusterB.Points.Count == 0 || clusterB == clusterA)
								continue;

							(Cluster smallerCluster, Cluster largerCluster) = clusterB.Points.Count < clusterA.Get().Points.Count 
							                                                  || (clusterB.Points.Count == clusterA.Get().Points.Count && clusterB.CaptureRadius < clusterA.Get().CaptureRadius)
								? (new ClusterId(clusterB), clusterA)
								: (clusterA, new ClusterId(clusterB));

							var diff = smallerCluster.transform.position - largerCluster.transform.position;
							var dist = diff.magnitude;
							var dir = dist > Mathf.Epsilon ? diff / dist : Vector3.forward;
							var farEdgeDist = (dist + smallerCluster.GetPointsRadius() + smallNumber);
							var farEdge = largerCluster.transform.position + dir * farEdgeDist;
						
							if (!largerCluster.CanContainPointPosition(farEdge, false))
								continue;
							
#if DEBUGDRAW_MERGE_CUSTE
							Debug.DrawLine(largerCluster.transform.position, farEdge, Color.green, 0.5f);
#endif
						
							using (ListPool<PointData>.Get(out var mergePoints))
							{
								foreach (var pair in smallerCluster.Points)
								{
									mergePoints.Add(pair.Key);
								}
						
								bool isImmediate = !smallerCluster.IsPlaying && !largerCluster.IsPlaying;
								foreach (var pointData in mergePoints)
								{
									pointData.SetCluster(largerCluster, isImmediate);
								}
								
								if (isImmediate)
								{
									removeImmediateClusters.Add(smallerCluster);
									m_MergeClustersQueue.Enqueue(largerCluster);
								}
							}
						}

						if (removeImmediateClusters.Count > 0)
						{
							couldStillMerge = true;
							foreach (var clusterId in removeImmediateClusters)
							{
								if(clusterId.IsValid())
									clusterId.Get().SetPooled(true);
							}
						}
					}
				}
			}
			
			void RefreshClusters(int maxSteps = -1)
			{
				using (ListPool<Cluster>.Get(out var emptyClusters))
				{
					foreach (var cluster in m_ChangedClusters)
					{
						cluster.Get().Refresh(m_UpdateParams);
						if (cluster.Get().Points.Count == 0 && (cluster.Get().Weights.Count == 0 || !cluster.Get().IsPlaying))
						{
							emptyClusters.Add(cluster);
						} 
					}
					foreach (var cluster in emptyClusters)
					{
						DespawnCluster(cluster);
					}
				}
				
				// todo: only refresh modified clusters
				foreach (var cluster in m_Clusters)
				{
					cluster.Refresh(m_UpdateParams);
				}
				
				// todo: try to start playing cluster 
				// todo: destroy (pool) cluster if no points and zero total weight
				// todo: finalize position & radius for modified clusters
				// todo: update emitter position from weights
				// todo: notify cluster of refresh.
			}
			
			public void OnDrawGizmos()
			{
				// todo: draw text info

				Gizmos.color = Color.Lerp(Type.DebugColor, Color.black, 0.55f);
				Gizmos.DrawWireSphere(m_UpdateParams.AttenuationPosition, m_CullingDistance);

				// clusters
				foreach (var cluster in m_Clusters)
				{
					Gizmos.color = new Color(Type.DebugColor.r, Type.DebugColor.g, Type.DebugColor.b, 0.1f);
					var position = cluster.transform.position;
					Gizmos.DrawSphere(position, cluster.CaptureRadius);

					
					Gizmos.color = Type.DebugColor;
					Gizmos.DrawWireCube(cluster.WeightedPosition, cluster.CaptureRadius/4.0f*Vector3.one);
					foreach (var weightPair in cluster.Weights)
					{
						Gizmos.color = Color.white;
						var midPoint = Vector3.Lerp(weightPair.Key.Point.WorldPosition, cluster.WeightedPosition, weightPair.Value);
				// #if UNITY_EDITOR
				// 		Handles.Label(Vector3.Lerp(weightPair.Key.Point.WorldPosition, cluster.WeightedPosition, 0.5f), weightPair.Value.ToString(CultureInfo.InvariantCulture));
				// #endif
						Debug.DrawLine(weightPair.Key.Point.WorldPosition, midPoint, Type.DebugColor);
						if (weightPair.Value < 1.0f)
						{
							Debug.DrawLine(midPoint, cluster.WeightedPosition, Color.black);
						}
					}
#if UNITY_EDITOR
					// todo: make runtime circle draw
					float radiusValue = cluster.GetPointsRadiusInfo();
					Handles.color = radiusValue >=  0 ? Color.white : Color.magenta;  
					Handles.DrawWireDisc(position, Vector3.up, Mathf.Abs(radiusValue));
#endif
					
					// todo: draw points assigned to cluster as lines to each point
					// todo: draw per points weights/blending as inner lines to each point
				}

				// sources (draw bounds)
				Gizmos.color = Type.DebugColor;
				foreach (var sourcePair in m_SourceDatas)
				{
					var source = sourcePair.Value;
					var sourceColor = Gizmos.color = !m_ActiveSources.Contains(source) 
							? Type.DebugColor
							: Color.Lerp(Type.DebugColor, Color.black, 0.125f);
					sourceColor.a = 0.5f;
					Gizmos.color = sourceColor;
					Gizmos.DrawWireCube(source.Bounds.center, source.Bounds.extents*2);
				}

			#if UNITY_EDITOR
				Gizmos.color = Color.white;
				Handles.color = Color.white;
				var style = new GUIStyle("button");
				style.normal.textColor = Color.white;
				style.richText = true;
				
				foreach (var cluster in m_Clusters)
				{
					Handles.Label(cluster.WeightedPosition, cluster.GetDebugString(), style);
				}
			#endif
			}
		}
	}
}
