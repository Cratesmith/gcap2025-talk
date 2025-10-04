using Cinemachine;
using UnityEngine;

namespace AudioClustersDemo
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class CameraScrollwheelZoom : MonoBehaviour
    {
        [SerializeField] float m_ZoomSpeed = 0.05f;
        [SerializeField] float m_MinZoom   = 1.0f;
        [SerializeField] float m_MaxZoom   = 20.0f;

        CinemachineVirtualCamera     m_VirtualCamera;
        CinemachineFramingTransposer m_FramingTransposer;
        void Awake()
        {
            m_VirtualCamera = GetComponent<CinemachineVirtualCamera>();
            m_FramingTransposer = m_VirtualCamera 
                ? m_VirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>() 
                : null;
        }

        // Update is called once per frame
        void Update()
        {
            if (m_FramingTransposer)
            {
                m_FramingTransposer.m_CameraDistance = Mathf.Clamp(m_FramingTransposer.m_CameraDistance - Input.mouseScrollDelta.y * m_ZoomSpeed * Time.deltaTime, m_MinZoom, m_MaxZoom);
            }
        }
    }
}
