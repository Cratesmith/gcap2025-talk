#define VERIFY_CLUSTER_LOGIC
#define USE_INCRENTAL_AVERAGE_POSITION
#define USE_INCRENTAL_POINTS_RADIUS
// #define DEBUGDRAW_CLUSTER_MOVDED

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace AudioClusters
{
	public abstract partial class ClusterType<TSelf, TPoint> 
	{
		public struct ClusterId : IEquatable<ClusterId>
		{
			readonly int     m_SpawnCount;
			readonly Cluster m_Cluster;

			public ClusterId(Cluster cluster)
			{
				m_Cluster = cluster;
				m_SpawnCount = cluster ? cluster.SpawnCount:-1;
			} 
			public Cluster Get() => IsValid() ? m_Cluster : null;
			public bool IsValid() => m_Cluster && m_SpawnCount == m_Cluster.SpawnCount;
			public static implicit operator ClusterId(Cluster cluster) => new ClusterId(cluster);
			public static implicit operator Cluster(ClusterId clusterId) => clusterId.Get();
			public static implicit operator bool(ClusterId clusterId) => clusterId.IsValid();
			public bool Equals(ClusterId other) => m_SpawnCount == other.m_SpawnCount && Equals(m_Cluster, other.m_Cluster);
			public override bool Equals(object obj) => obj is ClusterId other && Equals(other);
			public override int GetHashCode()
			{
				unchecked
				{
					return (m_SpawnCount * 397) ^ (m_Cluster ? m_Cluster.GetHashCode() : 0);
				}
			}
			public static bool operator ==(ClusterId left, ClusterId right) => left.Equals(right);
			public static bool operator !=(ClusterId left, ClusterId right) => !left.Equals(right);

			public override string ToString()
			{
				return IsValid() ? m_Cluster.ToString()
					: "Cluster[null]";
			}
		}
		
		/// <summary>
		/// A cluster emitter   
		/// </summary>
		public abstract class Cluster : MonoBehaviour
		{
			public TSelf Type { get; private set; }
			public int Id { get; private set; }
			public float CaptureRadius { get; private set; }
			public float TotalWeight { get; private set; }
			public Vector3 WeightedPosition { get; private set; }
			public Dictionary<PointData, Vector3> Points { get; } = new();
			public Dictionary<PointData, float> Weights { get; } = new();
			public int SpawnCount { get; private set; }
			public bool IsPlaying { get; protected set;}

			public event Action<Cluster> OnClusterChanged;
			
			UpdateParams m_UpdateParams;
			
			// distance to the furthest point, encoded to allow uncertainty 
			// if -ve, the real points radius would be <= Abs(m_PointsRadiusData)
			private float m_PointsRadiusData;
			
			public void Init(TSelf type, int id, UpdateParams updateParams)
			{
				Type = type;
				Id = id;
				m_UpdateParams = updateParams;
				SetPooled(false);
			}

			internal void BeginUpdate(UpdateParams updateParams)
			{
				m_UpdateParams = updateParams;
			}
			
			void MarkClusterAsChanged()
			{ 
				if (OnClusterChanged != null)
					OnClusterChanged.Invoke(this);
			}
			
			public bool UpdateCaptureRadius()
			{
				var clusterPosition = transform.position;
				var newRadius = GetClusterRadiusAtPosition(clusterPosition);
				if (Mathf.Approximately(newRadius, CaptureRadius))
					return false;
                
				// dont update the cluster radius unless it has changed sufficiently 
				// this prevents unessicary updates and also causes more distant clusters to (usually) refresh less frequently
				float toListener = m_UpdateParams.CalcMinDistanceTo(clusterPosition, Type.CaptureRadiusMinAttenuationDistance);
				if (Mathf.Abs(newRadius - CaptureRadius) < toListener * Type.CaptureRadiusMinChangeSinRatio) 
					return false;

				CaptureRadius = newRadius;
				
				// if radius has changed, the cluster needs to be refreshed
				// as well as any points that might be orphaned by this radius change
				MarkClusterAsChanged();
				if (PointsRadiusCompareTo(CaptureRadius) >= 0)
				{
					var captureRadiusSq = CaptureRadius*CaptureRadius;
					foreach (var pointPair in Points)
					{
						if ((pointPair.Value - clusterPosition).sqrMagnitude > captureRadiusSq)
							pointPair.Key.MarkPointAsChanged();
					}
				}
				return true;
			}
			
			public float GetClusterRadiusAtPosition(Vector3 position)
			{
				float minDistance = m_UpdateParams.CalcMinDistanceTo(position, Type.CaptureRadiusMinAttenuationDistance);
				return minDistance * Type.CaptureRadiusListenerSinRatio;
			}

			public float GetPointsRadiusInfo()
			{
				return m_PointsRadiusData;
			}
			
			public float GetPointsRadius()
			{
				if (m_PointsRadiusData >= 0 ) 
					return m_PointsRadiusData;
                
				var maxPointDistSq = 0.0f;
				foreach (var pair in Points)
				{
					var pointData = pair.Key;
					var pointPosition = pointData.Point.WorldPosition;
					var distSq = (pointPosition - transform.position).sqrMagnitude;
					if (distSq > maxPointDistSq)
					{
						maxPointDistSq = distSq;
					}
				}
				m_PointsRadiusData = Mathf.Min(Mathf.Sqrt(maxPointDistSq), CaptureRadius);
				return m_PointsRadiusData;
			}
			
			public int PointsRadiusSquaredCompareTo(float distanceSquared)
			{
				if (m_PointsRadiusData < 0 && (m_PointsRadiusData*m_PointsRadiusData) < distanceSquared)
				{
					return -1;
				}
				
				var pointsRadius = GetPointsRadius(); 
				return (pointsRadius*pointsRadius).CompareTo(distanceSquared);
			}
			
			public int PointsRadiusCompareTo(float distance)
			{
				if (m_PointsRadiusData < 0 && Mathf.Abs(m_PointsRadiusData) < distance)
				{
					return -1;
				}
				
				return GetPointsRadius().CompareTo(distance);
			}
			
			void HandleClusterMovedByPointChange(in Vector3 prevPosition, PointData causedByPoint, int pointCountChange)
			{
				// todo: only move cluster if radius or position have changed more than sin ratios from type settings
				UpdateCaptureRadius();
				UpdatePointsRadius(causedByPoint, pointCountChange);
#if DEBUGDRAW_CLUSTER_MOVDED
				var midPoint = Vector3.Lerp(prevPosition, transform.position, 0.5f);
				Debug.DrawLine(prevPosition, midPoint, Color.black, 1.0f);
				Debug.DrawLine(midPoint, transform.position, Color.white, 1.0f);
				var color = pointCountChange < 0 ? Color.red : pointCountChange == 0 ? Color.yellow : Color.cyan;
				color.a = 0.125f;
				Debug.DrawLine(causedByPoint.Point.WorldPosition+Vector3.up*0.1f, midPoint+Vector3.up*0.1f, color, 1.0f);
#endif
			}

			private void UpdatePointsRadius(PointData causedByPoint, int pointCountChange)
			{
#if USE_INCRENTAL_POINTS_RADIUS 
				// removing a point can only reduce radius
				if(pointCountChange < 0)
				{
					// when removing points, the radius can only reduce
					// the radius becomes unknown, it will be less or equaal to the previous value
					if(m_PointsRadiusData > 0)
						m_PointsRadiusData = -m_PointsRadiusData;
					return;
				}
				
				// adding/updating a point sets a new known radius if the value is higher
				var pointPosition = causedByPoint.Point.WorldPosition;
				var pointToClusterSq = (transform.position - pointPosition).sqrMagnitude;

				if (pointToClusterSq > m_PointsRadiusData * m_PointsRadiusData)
				{
					m_PointsRadiusData = Mathf.Sqrt(pointToClusterSq);
				} else if(pointCountChange == 0 && m_PointsRadiusData > 0)
				{
					// when upating, if a point shrinks
					// the radius becomes unknown, it will be less or equaal to the previous value
					m_PointsRadiusData = -m_PointsRadiusData;
				}
#else
				m_PointsRadiusData = -1;
				m_PointsRadiusData = GetPointsRadius();
#endif
			}

			public bool AddOrUpdatePoint(PointData pointData)
			{
				Assert.AreEqual(pointData.Cluster.Get(), this);
				Vector3 newClusterPosition;
				bool hasPoint = Points.TryGetValue(pointData, out var prevPointPosition);
				if (hasPoint && prevPointPosition == pointData.Point.WorldPosition)
				{
					return false;
				}
				
				int prevPointCount = Points.Count;
				Points[pointData] = pointData.Point.WorldPosition;

#if USE_INCRENTAL_AVERAGE_POSITION
				newClusterPosition = hasPoint 
					? IncrementalAverage.MoveAverageMove(transform.position, prevPointCount, prevPointPosition, pointData.Point.WorldPosition)
					: IncrementalAverage.MoveAverageAdd(transform.position, prevPointCount, pointData.Point.WorldPosition);
#else
				newClusterPosition = CalcAveragePointPosition();
#endif
				if (newClusterPosition != transform.position)
				{
#if VERIFY_CLUSTER_LOGIC
					var averagingError = (newClusterPosition - CalcAveragePointPosition()).magnitude;
					if (averagingError > 0.00001f)
					{
						Debug.LogWarning($"Cluster::AddOrUpdatePoint - position error average is high {averagingError}");
					}
#endif
						var prevPosition = transform.position;
						transform.position = newClusterPosition;
						HandleClusterMovedByPointChange(prevPosition, pointData, Points.Count - prevPointCount);
					}
				MarkClusterAsChanged();
				return true;
			}
			
			
			public bool RemovePoint(PointData pointData)
			{
				Assert.IsTrue(pointData!=null);
				Assert.AreNotEqual(pointData.Cluster.Get(), this);
				if (Points.TryGetValue(pointData, out var prevPointPosition))
				{
					var prevCount = Points.Count;
					bool removed = Points.Remove(pointData);
					Assert.IsTrue(removed);
					
#if USE_INCRENTAL_AVERAGE_POSITION
					Vector3 newClusterPosition = IncrementalAverage.MoveAverageRemove(transform.position, prevCount, prevPointPosition);
#else
					Vector3 newClusterPosition = CalcAveragePointPosition();
#endif
					if (newClusterPosition != transform.position)
					{
#if VERIFY_CLUSTER_LOGIC
						Assert.IsTrue((newClusterPosition-CalcAveragePointPosition()).magnitude < 0.01f);
#endif
						var prevPosition = transform.position;
						transform.position = newClusterPosition;
						HandleClusterMovedByPointChange(prevPosition, pointData, -1);
					}
					MarkClusterAsChanged();
					return true;
				}
				return false;
			}

			public bool AddOrUpdateWeight(PointData pointData, float weight)
			{
				Assert.IsTrue(pointData!=null);
				Assert.IsTrue(pointData.Weights.ContainsKey(this));

				if (Weights.TryGetValue(pointData, out var currentWeight))
				{
					if (Mathf.Approximately(currentWeight, weight))
						return false;
				}
				
				Weights[pointData] = weight;
				MarkClusterAsChanged();
				return true;
			}
			
			public bool RemoveWeight(PointData pointData)
			{
				if (Weights.Remove(pointData))
				{
					Assert.IsFalse(pointData.Weights.ContainsKey(this));
					MarkClusterAsChanged();
					return true;
				}
				return false;
			}
			public bool CanContainActivePoint(PointData pointData) => CanContainPointPosition(pointData.Point.WorldPosition, pointData.Cluster == this);

			public bool CanContainPointPosition(in Vector3 pointPosition, bool pointIsInCluster)
			{
				if (pointIsInCluster && Points.Count == 1)
					return true;
				
				// check if point is within capture distance
				var position = transform.position;
				if ((pointPosition-position).sqrMagnitude > CaptureRadius*CaptureRadius)
					return false;

				// if adding a new point, check that cluster position after add would not orphan any existing points 
				var pointsCount = Points.Count;
				if (!pointIsInCluster && pointsCount > 0)
				{
					var newPosition = IncrementalAverage.MoveAverageAdd(position, pointsCount, pointPosition);
					var newCaptureRadius = GetClusterRadiusAtPosition(newPosition);
					var newPositionDist = (newPosition - position).magnitude;
					if(PointsRadiusCompareTo(newCaptureRadius - newPositionDist) >= 0)
						return false;
				}

				return true;
			}
			public void SetPooled(bool value)
			{
				if (value)
				{
					gameObject.SetActive(false);
					Id = -1;
					CaptureRadius = 0;
					transform.position = Vector3.zero;
					m_PointsRadiusData = 0;
					m_UpdateParams = default;
					
					while (Points.Count > 0)
					{
						using var first = Points.GetEnumerator();
						first.MoveNext();
						var point = first.Current.Key;
						
						Assert.AreEqual(point.Cluster.Get(), this);
						point.SetCluster(null, true);
					}
				
					while (Weights.Count > 0)
					{
						using var first = Weights.GetEnumerator();
						first.MoveNext();
						var weight = first.Current.Key;
						Assert.IsTrue(weight != null);
						weight.Weights.Remove(this);
						RemoveWeight(weight);
					}
				} else
				{
					++SpawnCount;
					gameObject.SetActive(true);
				}
			}
			public virtual void Refresh(UpdateParams updateParams)
			{
				if (!IsPlaying)
					IsPlaying = StartPlaying();
				
				RefreshWeights();
			}
			
			virtual protected bool StartPlaying()
			{
				return true;
			}

			void RefreshWeights()
			{
				var newTotalWeight = 0.0f;
				foreach (var weightPair in Weights)
				{
					newTotalWeight += weightPair.Value;
				}
				TotalWeight = newTotalWeight;
				
				if (TotalWeight > 0)
				{
					var newWeightedPosition = Vector3.zero;
					foreach (var pair in Weights)
					{
						newWeightedPosition += pair.Key.Point.WorldPosition * pair.Value/newTotalWeight;
					}

					WeightedPosition = newWeightedPosition;
				} 
			}
			
			Vector3 CalcAveragePointPosition()
			{
				if (Points.Count == 0)
				{
					return transform.position;
				}
				
				Vector3 newPosition = Vector3.zero;
				foreach (var pair in Points)
				{
					newPosition += pair.Value;
				}
				newPosition /= Points.Count;
				return newPosition;
			}
			
			public override string ToString()
			{
				return $"Cluster[{Type.name}, {Id}, {SpawnCount}]";
			}
			
			public virtual string GetDebugString() => TotalWeight.ToString("0.#");
		}
		
#if UNITY_EDITOR
		public class ClusterEditor : Editor
		{
			[SerializeField] bool m_DebugWeights = true;
			[SerializeField] bool m_DebugPoints = true;
			public override void OnInspectorGUI()
			{
				DrawDefaultInspector();

				if (!EditorApplication.isPlaying)
					return;
				
				var cluster = target as Cluster;
				if (!cluster)
					return;
				
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Debug");
				using var debugBoxScope = new GUILayout.VerticalScope("Box");
				m_DebugPoints = EditorGUILayout.Foldout(m_DebugPoints, $"Points ({cluster.Points.Count})");
				if (m_DebugPoints)
				{
					using (new GUILayout.VerticalScope("Box"))
					{
						foreach (var point in cluster.Points)
						{
							EditorGUILayout.LabelField("point", $"{point.Key.Source}:{point.Key.Id}:{point.Key.Point.WorldPosition}");
							EditorGUILayout.Vector3Field("position", point.Value);
						}
					}
				}

				m_DebugWeights = EditorGUILayout.Foldout(m_DebugWeights, $"Weights ({cluster.Weights.Count})");
				if (m_DebugWeights)
				{
					using (new GUILayout.VerticalScope("Box"))
					{
						foreach (var point in cluster.Weights)
						{
							EditorGUILayout.LabelField("point", $"{point.Key.Source}:{point.Key.Id}:{point.Key.Point.WorldPosition}");
							EditorGUILayout.FloatField("weight", point.Value);
							if (point.Key.Weights.TryGetValue(cluster, out float weight))
							{
								if (!Mathf.Approximately(weight, point.Value))
								{
									EditorGUILayout.HelpBox("Point weight does not match weight on cluster", MessageType.Error, true);
								}
							}
							if (!point.Key.Weights.ContainsKey(cluster))
							{
								EditorGUILayout.HelpBox("Weight is not present on point", MessageType.Error, true);
							}
						}
					}
				}
			}
		}
#endif
	}
}