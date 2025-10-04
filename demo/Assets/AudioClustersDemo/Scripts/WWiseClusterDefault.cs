using System;
using System.Transactions;
using AudioClusters;
using UnityEngine;

namespace AudioClustersDemo
{
	[RequireComponent(typeof(AkGameObj))]
	public class WWiseClusterDefault : WwiseClusterTypeDefault.Cluster
	{
		void Awake()
		{
			m_AkGameObj = GetComponent<AkGameObj>();
		}

		AkGameObj m_AkGameObj;

		void OnDisable()
		{
			if (Type && Type.ClusterAkEvent.IsValid())
			{
				Type.ClusterAkEvent.Stop(gameObject);
				m_AkGameObj.Unregister();
				IsPlaying = false;
			}
		}

		protected override bool StartPlaying()
		{
			if (Type && Type.ClusterAkEvent.IsValid())
			{
				m_AkGameObj.Register();
				return Type.ClusterAkEvent.Post(gameObject) != AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
			}
			return false;
		}

		public override void Refresh(UpdateParams updateParams)
		{
			base.Refresh(updateParams);
			if (IsPlaying && Type && Type.PointCountRtpc.IsValid())
			{
				Type.PointCountRtpc.SetValue(gameObject, TotalWeight);
			}
		}
	}
}
