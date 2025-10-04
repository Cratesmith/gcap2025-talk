using System;
using AudioClusters;
using UnityEngine;

[RequireComponent(typeof(AkAudioListener))]
[RequireComponent(typeof(AkListenerDistanceProbe))]
public class WwiseClusterListener : MonoBehaviour
{
	AkAudioListener         m_AkListener;
	AkListenerDistanceProbe m_AkDistanceProbe;
	void Awake()
	{
		m_AkListener = GetComponent<AkAudioListener>();
		m_AkDistanceProbe = GetComponent<AkListenerDistanceProbe>();
	}

	void LateUpdate()
	{
		if (m_AkListener && m_AkListener.isDefaultListener)
		{
			var clusterManager = ClusterManager.Get();
			var listenerTransform = transform;
			clusterManager.ListenerPosition = listenerTransform.position;
			clusterManager.ListenerRotation = listenerTransform.rotation;
			clusterManager.AttenuationPosition = m_AkDistanceProbe.distanceProbe
				? m_AkDistanceProbe.distanceProbe.transform.position
				: listenerTransform.position;
		}
		
	}
}
