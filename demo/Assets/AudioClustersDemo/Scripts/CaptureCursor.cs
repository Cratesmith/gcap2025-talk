using System;
using UnityEditor;
using UnityEngine;

namespace AudioClustersDemo
{
    public class CaptureCursor : MonoBehaviour
    {
        void Awake()
        {
            Application.focusChanged += focused => SetCaptured(focused);
        }
        void OnEnable()
        {
            SetCaptured(true);
        }

        void OnDisable()
        {
            SetCaptured(false);
        }

        void SetCaptured(bool b)
        {
            Cursor.visible = !b;
            Cursor.lockState = b ? CursorLockMode.Confined : CursorLockMode.None;
        }
    }
}
