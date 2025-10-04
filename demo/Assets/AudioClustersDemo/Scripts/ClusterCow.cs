using AudioClusters;

namespace AudioClustersDemo
{
	public class ClusterCow : ClusterTypeCow.Cluster
	{ 
		void Awake()
		{
			m_AkGameObj = GetComponent<AkGameObj>();
		}

		AkGameObj m_AkGameObj;
		float     m_Agitation;
		float     m_Speed;

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
				if(!m_AkGameObj.GameObjIsRegistered())
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
			
			m_Agitation = 0.0f;
			m_Speed = 0.0f;
			if (Weights.Count > 0)
			{
				foreach (var pair in Weights)
				{
					m_Agitation += pair.Key.Point.Agitation * pair.Value;
					m_Speed += pair.Key.Point.Speed * pair.Value;
				}
				m_Agitation /= Weights.Count;
				m_Speed /= Weights.Count;
			}

			if (Type.AgitationRtpc.IsValid())	
				Type.AgitationRtpc.SetValue(gameObject, m_Agitation * 100.0f);

			if (Type.SpeedRtpc.IsValid())
				Type.SpeedRtpc.SetValue(gameObject, m_Speed * 100.0f);
		}

		public override string GetDebugString()
		{
			return $"{base.GetDebugString()}" +
			       $"\nSpeed: {m_Speed:0.#}" + 
			       $"\nAgitation: {m_Agitation:0.#}";

		}
	}
}
