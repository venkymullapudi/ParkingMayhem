using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Touch Settings")]
    public float touchSensitivity = 1f;
    public float rotationSensitivity = 1f;
    public float pinchSensitivity = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2f;

    private Camera mainCamera;
    private Vector2 lastTouchPosition;
    private float lastPinchDistance;
    private float currentZoom = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Check if touch is over UI
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    lastTouchPosition = touch.position;
                    break;

                case TouchPhase.Moved:
                    if (Input.touchCount == 1)
                    {
                        HandleSingleTouch(touch);
                    }
                    else if (Input.touchCount == 2)
                    {
                        HandleMultiTouch();
                    }
                    break;
            }
        }
    }

    private VehicleController GetActiveVehicle()
    {
        return Object.FindAnyObjectByType<VehicleController>();
    }

    private void HandleSingleTouch(Touch touch)
    {
        Vector2 worldDelta = mainCamera.ScreenToWorldPoint(touch.position) -
                             mainCamera.ScreenToWorldPoint(lastTouchPosition);

        VehicleController vehicle = GetActiveVehicle();
        if (vehicle != null)
            vehicle.transform.Translate(worldDelta * touchSensitivity, Space.World);

        lastTouchPosition = touch.position;
    }

    private void HandleMultiTouch()
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);

        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            lastPinchDistance = currentPinchDistance;
        }
        else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
        {
            float pinchDelta = currentPinchDistance - lastPinchDistance;
            currentZoom = Mathf.Clamp(currentZoom + pinchDelta * pinchSensitivity, minZoom, maxZoom);
            mainCamera.orthographicSize = 5f / currentZoom;

            VehicleController vehicle = GetActiveVehicle();
            if (vehicle != null)
            {
                Vector2 touchDelta = touch1.position - touch0.position;
                Vector2 lastTouchDelta = (touch1.position - touch1.deltaPosition) -
                                         (touch0.position - touch0.deltaPosition);
                float angleDelta = Vector2.SignedAngle(lastTouchDelta, touchDelta);
                vehicle.transform.Rotate(Vector3.forward, angleDelta * rotationSensitivity);
            }

            lastPinchDistance = currentPinchDistance;
        }
    }

    public void ResetZoom()
    {
        currentZoom = 1f;
        mainCamera.orthographicSize = 5f;
    }
} 