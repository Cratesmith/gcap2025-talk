using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace AudioClusters
{
	[Flags]
	public enum PointDataFlags : ushort
	{
		None    = 0,
		Changed = 1<<0,
		Culled  = 1<<1,
		Removed = 1<<2,
		Blending = 1<<3,
	}
	
	public abstract partial class ClusterType<TSelf, TPoint>
	{
		/// <summary>
		/// Information about a single point
		/// </summary>
		public class PointData
		{
			/// <summary>
			/// The Id used by the source to recognize this point.
			/// These Ids are only unique within each source.
			/// </summary>
			public int Id => Point.Id;

			/// <summary>
			/// The source that owns this point
			/// </summary>
			public SourceData Source { get; }

			/// <summary>
			/// Point data provided by the source
			/// </summary>
			public TPoint Point;
			object                                m_WorldPosition;
			PointDataFlags                        m_Flags = PointDataFlags.None;

			/// <summary>
			///     what cluster does this point belong to?
			///     this point will contribute to the cluster's position. (note that emitter location is controlled by Weights)
			/// </summary>
			public ClusterId Cluster { get; private set; }

			/// <summary>
			///     how much has this point blended to being part of a cluster?
			///     if the point has a cluster it will blend the weight for this cluster to 1.0, blending out all others in the
			///     process.
			/// </summary>
			public Dictionary<ClusterId, float> Weights { get; } = new();

			/// <summary>
			/// is this point culled?
			/// culled points unbind from their cluster and blend out weights as if being removed, but aren't removed once blended out.
			/// </summary>
			public bool IsCulled { get => GetPointFlag(PointDataFlags.Culled); }
		
			/// <summary>
			/// is this point changed?
			/// </summary>
			public bool IsChanged { get => GetPointFlag(PointDataFlags.Changed); }
			
			/// <summary>
			/// is this point being removed
			/// </summary>
			public bool Removing { get => GetPointFlag(PointDataFlags.Removed); }

			public bool IsValid => Source != null && Source.Source != null;
			public bool IsActive => IsValid && (!IsCulled || !IsBlendedOut);
			public bool IsBlendedOut => !Cluster && Weights.Count == 0;
			// public bool IsBlending => IsValid && GetPointFlag(PointDataFlags.Blending);
			
			public PointData(SourceData source, TPoint point)
			{
				Source = source;
				Point = point;
			}
			public bool SetCluster(Cluster value, bool immediate)
			{
				if (value == Cluster)
					return false;

				var prevCluster = Cluster;
				Cluster = value;
				
				if (prevCluster)
					prevCluster.Get().RemovePoint(this);

				if (Cluster)
				{
					var initialWeight = 0.0f;
					if (immediate)
					{
						initialWeight = 1.0f;
					}
					Weights[Cluster] = initialWeight;
					Cluster.Get().AddOrUpdatePoint(this);
					MarkPointAsChanged();
				}
				
				Assert.IsTrue(Source!=null);
				if (Source.OnPointClusterChanged != null)
				{
					Source.OnPointClusterChanged(this);
				}
				return true;
			}

			public bool GetPointFlag(PointDataFlags flag) => (m_Flags & flag) != 0;
			internal bool SetPointFlag(PointDataFlags flag, bool value)
			{
				if (GetPointFlag(flag) == value)
					return false;
				
				if (value)
					m_Flags |= flag;
				else
					m_Flags &= ~flag;
				return true;
			}

			public void MarkPointAsChanged()
			{
				Source.MarkPointAsChanged(this);
			}

			public override string ToString()
			{
				return $"Point[{Source}, {Id}]";

			}
		}
		
		/// <summary>
		/// Information about a source object that provides points
		/// Owns PointData for each point provided by the source
		/// </summary>
		public class SourceData
		{
			/// <summary>
			/// Refernce to the source object
			/// </summary>
			public ISource<TPoint> Source { get; }
			
			/// <summary>
			/// Table of points, indexed by the Ids that are only unique from other points from the same source
			/// </summary>
			public Dictionary<int, PointData> PointsById { get; } = new();
			
			internal event Action<PointData> OnPointChanged; 
			internal Action<PointData> OnPointClusterChanged; 

			public Bounds Bounds { get; private set; } 
			
			public SourceData(ISource<TPoint> source)
			{
				Source = source;
			}

			public override string ToString()
			{
				if (Source is Object obj)
				{
					return obj.name;
				}
				return base.ToString();
			}

			bool TryAddPointInternal(int pointId, in TPoint point, out PointData pointData)
			{
				if (!PointsById.TryGetValue(pointId, out pointData))
				{
					pointData = new PointData(this, point);
					PointsById.Add(pointId, pointData);
					MarkPointAsChanged(pointData);
					return true;
				}

				// RemovingPoints.Remove(pointData);
				if (EqualityComparer<TPoint>.Default.Equals(pointData.Point, point))
					return false;
				
				MarkPointAsChanged(pointData);
				pointData.Point = point;
				return true;
			}
			
			bool MarkPointAsRemoving(PointData pointData, bool value)
			{
				if (pointData.SetPointFlag(PointDataFlags.Removed, value))
				{
					MarkPointAsChanged(pointData);
					return true;
				}
				return false;
			}

			public bool MarkPointAsChanged(PointData pointData)
			{
				if (pointData.SetPointFlag(PointDataFlags.Changed, true))
				{
					if (OnPointChanged != null)
						OnPointChanged(pointData);
					return true;
				}
				return false;
			}

			bool AddPointsInternal<T>(in T addPoints) where T : IEnumerable<TPoint>
			{
				bool modified = false;
				foreach (var point in addPoints)
				{
					if (TryAddPointInternal(point.Id, point, out var pointData))
					{
						modified = true;
					} 
				}
				return modified;
			}
			
			public bool AddPoints<T>(in T addPoints) where T:IEnumerable<TPoint>
			{
				if (AddPointsInternal(addPoints))
				{
					UpdateBounds();
					return true;
				}
				return false;
			}
			
			public bool RemovePoints<T>(in T removePoints, bool immediate) where T:IEnumerable<int>
			{
				if (RemovePointsInternal(removePoints, immediate))
				{
					UpdateBounds();
					return true;
				}
				return false;
			}

			public bool SetPointsInternal<T>(in T setPoints, bool immediate) where T:IEnumerable<TPoint>
			{
				bool modified = false;
				using (HashSetPool<int>.Get(out var removePoints))
				{
					foreach (var pair in PointsById)
					{
						removePoints.Add(pair.Key);
					}
					
					foreach (var pair in setPoints)
					{
						removePoints.Remove(pair.Id);
					}

					modified |= RemovePointsInternal(removePoints, immediate);
					modified |= AddPointsInternal(setPoints);
				}
				return modified;
			}

			public bool SetPoints<T>(in T setPoints, bool removeImmediately) where T : IEnumerable<TPoint>
			{
				if (SetPointsInternal(setPoints, removeImmediately))
				{
					UpdateBounds();
					return true;
				}
				return false;
			}

			bool TryRemovePointInternal(int pointId, bool immediate)
			{
				bool modified = false;
				if (PointsById.TryGetValue(pointId, out var pointData))
				{
					if (!immediate)
					{
						return MarkPointAsRemoving(pointData, true);
					}
					PointsById.Remove(pointId);

					if (pointData.Cluster)
					{
						modified = true;
						pointData.Cluster.Get().RemovePoint(pointData);
					}
					
					foreach (var pair in pointData.Weights)
					{
						if (pair.Key)
						{
							modified = true;
							pair.Key.Get().RemoveWeight(pointData);
						}
					}
					pointData.Weights.Clear();
				}

				if (modified)
				{
					MarkPointAsChanged(pointData);
					return true;
				}
				return false;
			}
			
			public bool RemovePointsInternal<T>(in T pointIds, bool immediate) where T : IEnumerable<int>
			{
				bool modified = false;
				foreach (var pointId in pointIds)
				{
					modified |= TryRemovePointInternal(pointId, immediate);
				}
				return modified;
			}
			
			public bool RemoveAllPoints(bool immediate)
			{
				if (PointsById.Count == 0)
				{
					return false;
				}
				
				bool modified = false;
				foreach (var pointPair in PointsById)
				{
					if (!immediate)
					{
						modified |= MarkPointAsRemoving(pointPair.Value, true);
						continue;
					}
					
					var pointData = pointPair.Value;
					if (pointData.Cluster)
					{
						pointData.Cluster.Get().RemovePoint(pointData);
					}
					
					foreach (var pair in pointData.Weights)
					{
						if (pair.Key)
						{
							pair.Key.Get().RemoveWeight(pointData);
						}
					}
				}
				PointsById.Clear();

				if (modified)
				{
					UpdateBounds();
					return true;
				}
				return false;
			}

			void UpdateBounds()
			{
				if (Source != null)
				{
					Bounds = Source.GetCullingBounds();
				}
			}
			
			internal bool ApplyCulling(float cullingDistance, in UpdateParams updateParams)
			{
				bool bAnyActive = false;
				var cullingDistSq = cullingDistance * cullingDistance;
				
				Vector3 boundsNearest = Bounds.ClosestPoint(updateParams.AttenuationPosition);
				if ((boundsNearest-updateParams.AttenuationPosition).sqrMagnitude >= cullingDistSq)
				{
					// bounds fully outside culling dist. Mark all points as culled
					// todo: skip this if there are no non-culled points
					foreach (var pointPair in PointsById)
					{
						SetPointCulledInternal(pointPair.Value, true);
					}
				}
				else if ((Bounds.center-updateParams.AttenuationPosition).magnitude - Bounds.extents.magnitude > cullingDistSq)
				{
					// whole bounds inside culling dist. Mark all points non-culled
					// todo: skip this if there are no culled points
					bAnyActive = true;
					foreach (var pointPair in PointsById)
					{
						SetPointCulledInternal(pointPair.Value, false);
					}
				}
				else
				{
					// bounds partially overlapping culling dist. Cull each point by distance individually
					foreach (var pointPair in PointsById)
					{
						var isCulled = (pointPair.Value.Point.WorldPosition - updateParams.AttenuationPosition).sqrMagnitude >= cullingDistSq;
						SetPointCulledInternal(pointPair.Value, isCulled);
						if (!isCulled)
						{
							bAnyActive = true;
						}
					}
				}
			
				return bAnyActive;
			}
			
			bool SetPointCulledInternal(PointData point, bool value)
			{
				if (!point.SetPointFlag(PointDataFlags.Culled, value))
					return false;
				
				MarkPointAsChanged(point);
				return true;
			}
		}
	}
}
