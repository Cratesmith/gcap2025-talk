using AK.Wwise;
using UnityEngine;
using UnityEngine.Serialization;

namespace AudioClustersDemo
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CharacterController))]
    class PlayerCharacter : MonoBehaviour
    {
        [SerializeField] float  m_GravityScale = 2.0f;
        [SerializeField] float  m_MoveSpeed    = 2.0f;
        [SerializeField] float  m_TurnBlend    = 5.0f;
        [SerializeField] float  m_JumpHeight   = 1.0f;
        [SerializeField] Switch m_LandSwitch;
        [SerializeField] Switch         m_WalkSwitch;
        [SerializeField] Switch         m_SprintSwitch;
        [SerializeField] Switch         m_JumpSwitch;
        [SerializeField] AK.Wwise.Event m_FootstepEvent;

        CharacterController m_CharacterController;
        Animator            m_Animator;
        Vector3             m_DesiredDirection;
        Vector3             m_Velocity;

        static readonly int AnimMoveSpeed = Animator.StringToHash("move_speed");
        static readonly int AnimJump      = Animator.StringToHash("jump");
        static readonly int AnimIsGrounded    = Animator.StringToHash("is_grounded");

        void Awake()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Animator = GetComponent<Animator>();
        }

        void Walk() => PostFootstep(m_WalkSwitch);

        void Sprint() => PostFootstep(m_SprintSwitch);

        void Land() => PostFootstep(m_LandSwitch);

        void Jump()
        {
            if (!m_CharacterController.isGrounded)
                return;
            
            m_Velocity.y = Mathf.Sqrt(m_JumpHeight * -2.0f * Physics.gravity.y * m_GravityScale);
            m_Animator.SetTrigger(AnimJump);

            PostFootstep(m_JumpSwitch);
        }
        
        void PostFootstep(in Switch akSwitch)
        {
            if (m_FootstepEvent.IsValid() && akSwitch.IsValid())
            {
                akSwitch.SetValue(gameObject);
                m_FootstepEvent.Post(gameObject);
            }
        }

        void FixedUpdate()
        {
            var camera = Camera.main;
            if (!camera)
                return;
        
            var input2d = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            var cameraFwd = camera.transform.forward;
            var flatRight = Vector3.Cross(Vector3.up, cameraFwd);
            var flatFwd = Vector3.Cross(flatRight, Vector3.up);
            var input3d = (flatRight * input2d.x + flatFwd * input2d.y).normalized * input2d.magnitude;

            
            if (m_CharacterController.isGrounded)
            {
                if (m_Velocity.y < 0)
                    m_Velocity.y = 0;

                if (Input.GetButton("Fire1"))
                {
                    Jump();
                }
            }
            m_Velocity += Vector3.up * (Physics.gravity.y*m_GravityScale * Time.deltaTime);
        
            var move =  (input3d * m_MoveSpeed + m_Velocity) * Time.deltaTime;
            m_CharacterController.Move(move);
        
            if (input3d.sqrMagnitude > Mathf.Epsilon)
                m_DesiredDirection = input3d.normalized;
        
            if(m_DesiredDirection.sqrMagnitude > Mathf.Epsilon)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(m_DesiredDirection), Time.deltaTime * m_TurnBlend);
            }
            m_Animator.SetFloat(AnimMoveSpeed, Vector3.Dot(new Vector3(input3d.x, 0, input3d.z), transform.forward));
            m_Animator.SetBool(AnimIsGrounded, m_CharacterController.isGrounded);
        }
    }
}
