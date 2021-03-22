using UnityEngine;

namespace Fragsurf.Movement
{
    /// <summary>
    /// Easily add a surfable character to the scene
    /// </summary>
    [AddComponentMenu("Fragsurf/Surf Character")]
    public class SurfCharacter : MonoBehaviour, ISurfControllable
    {

        public enum ColliderType
        {
            Capsule,
            Box
        }

        ///// Fields /////

        [Header("Physics Settings")]
        public int TickRate = 128;
        public Vector3 ColliderSize = new Vector3(1, 2, 1);
        public ColliderType CollisionType;

        [Header("View Settings")]
        public Transform viewTransform;
        public Vector3 ViewOffset = new Vector3(0, 0.61f, 0);

        [Header("Input Settings")]
        public float XSens = 15;
        public float YSens = 15;
        public KeyCode JumpButton = KeyCode.LeftShift;
        public KeyCode MoveLeft = KeyCode.A;
        public KeyCode MoveRight = KeyCode.D;
        public KeyCode MoveForward = KeyCode.W;
        public KeyCode MoveBack = KeyCode.S;

        [Header("Movement Config")]
        [SerializeField]
        public MovementConfig moveConfig;

        private GameObject _groundObject;
        private Vector3 _baseVelocity;
        private Collider _collider;
        private Vector3 _angles;
        private Vector3 _startPosition;
		private float _wishJumpScroll;

        private MoveData _moveData = new MoveData();
        private SurfController _controller = new SurfController();

        ///// Properties /////

        public MoveType MoveType
        {
            get { return MoveType.Walk; }
        }

        public MovementConfig MoveConfig
        {
            get { return moveConfig; }
        }

        public MoveData MoveData
        {
            get { return _moveData; }
        }

        public Collider Collider
        {
            get { return _collider; }
        }

        public GameObject GroundObject
        {
            get { return _groundObject; }
            set { _groundObject = value; }
        }

        public Vector3 BaseVelocity
        {
            get { return _baseVelocity; }
        }

        public Vector3 Forward
        {
            get { return viewTransform.forward; }
        }

        public Vector3 Right
        {
            get { return viewTransform.right; }
        }

        public Vector3 Up
        {
            get { return viewTransform.up; }
        }

        ///// Methods /////

        private void Awake()
        {
            Application.targetFrameRate = 144;
            QualitySettings.vSyncCount = 1;

            Time.fixedDeltaTime = 1f / TickRate;
        }

        private void Start()
        {
            if(viewTransform == null)
                viewTransform = Camera.main.transform;
            viewTransform.SetParent(transform, false);
            viewTransform.localPosition = ViewOffset;
            viewTransform.localRotation = transform.rotation;

            _collider = gameObject.GetComponent<Collider>();

            if (_collider != null)
                GameObject.Destroy(_collider);

            // rigidbody is required to collide with triggers
            var rbody = gameObject.GetComponent<Rigidbody>();
            if (rbody == null)
                rbody = gameObject.AddComponent<Rigidbody>();
            rbody.isKinematic = true;

            switch(CollisionType)
            {
                case ColliderType.Box:
                    _collider = gameObject.AddComponent<BoxCollider>();
                    var boxc = (BoxCollider)_collider;
                    boxc.size = ColliderSize;
                    break;

                case ColliderType.Capsule:
                    _collider = gameObject.AddComponent<CapsuleCollider>();
                    var capc = (CapsuleCollider)_collider;
                    capc.height = ColliderSize.y;
                    capc.radius = ColliderSize.x / 2f;
                    break;
            }

            _collider.isTrigger = true;
            _moveData.Origin = transform.position;
            _startPosition = transform.position;
        }

        private void Update()
        {
            UpdateTestBinds();
            UpdateRotation();
            UpdateMoveData();
        }

        private void UpdateTestBinds()
        {
            if(Input.GetKeyDown(KeyCode.R))
            {
                MoveData.Velocity = Vector3.zero;
                MoveData.Origin = _startPosition;
            }
        }

        private void LateUpdate()
        {
            viewTransform.rotation = Quaternion.Euler(_angles);
        }

        private void FixedUpdate()
        {
            _controller.ProcessMovement(this, moveConfig, Time.fixedDeltaTime);
            transform.position = MoveData.Origin;
        }

        private void UpdateMoveData()
        {
            var moveLeft = Input.GetKey(MoveLeft);
            var moveRight = Input.GetKey(MoveRight);
            var moveFwd = Input.GetKey(MoveForward);
            var moveBack = Input.GetKey(MoveBack);

			float mouseWheelAxis = Mathf.Abs(Input.GetAxisRaw("Mouse ScrollWheel"));
            _wishJumpScroll = Mathf.Clamp(Mathf.Lerp(0, _wishJumpScroll+mouseWheelAxis*20f, 0.5f), 0f, 2f);
            var jump = Input.GetKey(JumpButton) || _wishJumpScroll >= 0.04f || mouseWheelAxis >= 0.05f;

            if (!moveLeft && !moveRight)
                _moveData.SideMove = 0;
            else if (moveLeft)
                _moveData.SideMove = -MoveConfig.Accel;
            else if (moveRight)
                _moveData.SideMove = MoveConfig.Accel;

            if (!moveFwd && !moveBack)
                _moveData.ForwardMove = 0;
            else if (moveFwd)
                _moveData.ForwardMove = MoveConfig.Accel;
            else if (moveBack)
                _moveData.ForwardMove = -MoveConfig.Accel;

			if(_wishJumpScroll >= 0f)
                _wishJumpScroll -= Time.deltaTime*0.5f;
            if (jump)
                _moveData.Buttons = _moveData.Buttons.AddFlag((int)InputButtons.Jump);
            else
                _moveData.Buttons = _moveData.Buttons.RemoveFlag((int)InputButtons.Jump);

            _moveData.OldButtons = _moveData.Buttons;
            _moveData.ViewAngles = _angles;
        }

        private void UpdateRotation()
        {
            float mx = (Input.GetAxis("Mouse X") * XSens * .02f);
            float my = Input.GetAxis("Mouse Y") * YSens * .02f;
            var rot = _angles + new Vector3(-my, mx, 0f);
            rot.x = ClampAngle(rot.x, -85f, 85f);
            _angles = rot;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static float ClampAngle(float angle, float from, float to)
        {
            if (angle < 0f) angle = 360 + angle;
            if (angle > 180f) return Mathf.Max(angle, 360 + from);
            return Mathf.Min(angle, to);
        }

    }
}

