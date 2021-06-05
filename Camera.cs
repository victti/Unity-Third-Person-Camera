using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    protected Transform _XForm_Camera;
    protected Transform _XForm_Parent;
    protected Transform _XForm_Player;

    protected Vector3 _LocalRotation;
    protected float _CameraDistance = 10f;

    public bool InvertVerticalAxis = true;

    public float MouseSensitivity = 4f;
    public float ScrollSensitvity = 2f;
    public float OrbitDampening = 10f;
    public float ScrollDampening = 6f;

    public bool CameraDisabled = false;

    private Camera fakeCamera;

    void Start()
    {
        this._XForm_Camera = this.transform;
        this._XForm_Parent = this.transform.parent;
        this._XForm_Player = this._XForm_Parent.parent;

        GameObject CamerasHelperFolder = new GameObject("Cameras Helper Folder");
        fakeCamera = new GameObject().AddComponent<Camera>();
        fakeCamera.enabled = false;
        fakeCamera.transform.SetParent(CamerasHelperFolder.transform);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
            CameraDisabled = !CameraDisabled;

        if (!CameraDisabled)
        {
            //Rotation of the Camera based on Mouse Coordinates
            if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
            {
                _LocalRotation.x += Input.GetAxis("Mouse X") * MouseSensitivity;
                _LocalRotation.y += Input.GetAxis("Mouse Y") * MouseSensitivity * (InvertVerticalAxis ? -1 : 1);

                //Clamp the y Rotation to horizon and not flipping over at the top
                if (_LocalRotation.y < -90f)
                    _LocalRotation.y = -90f;
                else if (_LocalRotation.y > 90f)
                    _LocalRotation.y = 90f;
            }

            //Zooming Input from our Mouse Scroll Wheel
            if (Input.GetAxis("Mouse ScrollWheel") != 0f)
            {
                float ScrollAmount = Input.GetAxis("Mouse ScrollWheel") * ScrollSensitvity;

                ScrollAmount *= (this._CameraDistance * 0.3f);

                this._CameraDistance += ScrollAmount * -1f;

                this._CameraDistance = Mathf.Clamp(this._CameraDistance, 1.5f, 100f);
            }
        }

        //Actual Camera Rig Transformations
        Quaternion QT = Quaternion.Euler(_LocalRotation.y, _LocalRotation.x, 0);
        this._XForm_Parent.rotation = Quaternion.Lerp(this._XForm_Parent.rotation, QT, Time.deltaTime * OrbitDampening);

        // Collision Detection Stuff
        float finalDist = this._CameraDistance * -1f;

        // Not sure how costful this is. If you are not going to change the main camera settings any time, remove this, or make it copy only
        // when you make changes on the main camera
        fakeCamera.CopyFrom(Camera.main);
        fakeCamera.cullingMask = 0;

        fakeCamera.transform.position = getPosition(_XForm_Player.rotation, getPosition(_XForm_Parent.localRotation, new Vector3(0, 0, finalDist), _XForm_Parent.localPosition), _XForm_Player.position);
        
        Vector3[] viewPortPositions = getCameraViewPortPositions(fakeCamera);

        for (int i = 0; i < viewPortPositions.Length; i++)
        {
            if (Physics.Linecast(_XForm_Parent.parent.position, viewPortPositions[i]))
            {
                for (int j = Mathf.FloorToInt(finalDist); j < 5; j++)
                {
                    fakeCamera.transform.position = getPosition(_XForm_Player.rotation, getPosition(_XForm_Parent.localRotation, new Vector3(0, 0, j), _XForm_Parent.localPosition), _XForm_Player.position);
                    viewPortPositions = getCameraViewPortPositions(fakeCamera);
                    if (!Physics.Linecast(_XForm_Parent.parent.position, viewPortPositions[i]))
                    {
                        finalDist = j;
                        break;
                    }
                }
            }
        }

        float nextPos = Mathf.Lerp(this._XForm_Camera.localPosition.z, finalDist, Time.deltaTime * ScrollDampening);

        this._XForm_Camera.localPosition = new Vector3(0f, 0f, nextPos);
    }

    /// <summary>
    /// Gets the position an object would be within a parent-child situation
    /// </summary>
    /// <param name="rotation">parent rotation</param>
    /// <param name="offset">child local position</param>
    /// <param name="position">parent position</param>
    /// <returns></returns>
    private Vector3 getPosition(Quaternion rotation, Vector3 offset, Vector3 position)
    {
        return rotation * offset + position;
    }

    private Vector3[] getCameraViewPortPositions(Camera c)
    {
        return new Vector3[4]
        {
            c.ViewportPointToRay(new Vector3(0, 0, 0)).origin,
            c.ViewportPointToRay(new Vector3(1, 0, 0)).origin,
            c.ViewportPointToRay(new Vector3(1, 1, 0)).origin,
            c.ViewportPointToRay(new Vector3(0, 1, 0)).origin
        };
    }
}
