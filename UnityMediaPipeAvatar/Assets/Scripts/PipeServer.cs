using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;
using Object = UnityEngine.Object;

public class PipeServer : MonoBehaviour
{
    public string websocketUrl = "ws://localhost:5050"; // WebSocket server URL
    public Transform bodyParent;
    public GameObject landmarkPrefab;
    public GameObject linePrefab;
    public GameObject headPrefab;
    public bool enableHead = false;
    public float multiplier = 10f;
    public float landmarkScale = 1f;
    public float maxSpeed = 50f;
    public float debug_samplespersecond;
    public int samplesForPose = 1;
    public bool active;


    public GameObject[] points;
    private WebSocket webSocket;
    private Body body;

    // These virtual transforms are not actually provided by mediapipe pose, but are required for avatars.
    // So I just manually compute them
    private Transform virtualNeck;
    private Transform virtualHip;

    public Transform GetLandmark(Landmark mark)
    {
        return body.instances[(int)mark].transform;
    }
    public Transform GetVirtualNeck()
    {
        return virtualNeck;
    }
    public Transform GetVirtualHip()
    {
        return virtualHip;
    }

    private async void Start()
    {
        InitializePositions();
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        //body = new Body(bodyParent, landmarkPrefab, linePrefab, landmarkScale, enableHead ? headPrefab : null);
        virtualNeck = new GameObject("VirtualNeck").transform;
        virtualHip = new GameObject("VirtualHip").transform;

        webSocket = new WebSocket(websocketUrl);

        webSocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };

        webSocket.OnError += (e) =>
        {
            Debug.LogError("WebSocket Error: " + e);
        };

        webSocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };

        webSocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            HandleMessage(message);
            // Debug.Log(message);
        };

        await webSocket.Connect();
    }

    private void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        webSocket.DispatchMessageQueue();
        #endif
        //UpdateBody(body);
        
    }
    public void SetVisible(bool visible)
    {
        bodyParent.gameObject.SetActive(visible);
    }

    private void InitializePositions()
    {
        // Example initial positions (these should be adjusted to your specific needs)
        Vector3 basePosition = new Vector3(0, -5.23f, 3.48f);
        Vector3[] initialPositions = new Vector3[25] {
            new Vector3(0, 2.0f, 0) + basePosition,  // Head
            new Vector3(0, 1.7f, 0) + basePosition,  // Neck
            new Vector3(0.4f, 1.7f, 0) + basePosition,  // Left Shoulder
            new Vector3(-0.4f, 1.7f, 0) + basePosition,   // Right Shoulder
            new Vector3(0.6f, 1.3f, 0) + basePosition,  // Left Elbow
            new Vector3(-0.6f, 1.3f, 0) + basePosition,   // Right Elbow
            new Vector3(0.8f, 0.9f, 0) + basePosition,  // Left Wrist
            new Vector3(-0.8f, 0.9f, 0) + basePosition,   // Right Wrist
            new Vector3(0, 1.2f, 0) + basePosition,  // Torso
            new Vector3(0.3f, 1.0f, 0) + basePosition,  // Left Hip
            new Vector3(-0.3f, 1.0f, 0) + basePosition,   // Right Hip
            new Vector3(0.3f, 0.6f, 0) + basePosition,  // Left Knee
            new Vector3(-0.3f, 0.6f, 0) + basePosition,   // Right Knee
            new Vector3(0.3f, 0.2f, 0) + basePosition,  // Left Ankle
            new Vector3(-0.3f, 0.2f, 0) + basePosition,   // Right Ankle
            new Vector3(0.4f, 1.5f, 0) + basePosition,  // Left Upper Arm
            new Vector3(-0.4f, 1.5f, 0) + basePosition,   // Right Upper Arm
            new Vector3(0.3f, 0.9f, 0) + basePosition,  // Left Thigh
            new Vector3(-0.3f, 0.9f, 0) + basePosition,   // Right Thigh
            new Vector3(0, 1.4f, 0) + basePosition,  // Chest
            new Vector3(0.5f, 1.2f, 0) + basePosition,  // Left Side Torso
            new Vector3(-0.5f, 1.2f, 0) + basePosition,   // Right Side Torso
            new Vector3(0, 0.4f, 0) + basePosition,   // Base Spine
            new Vector3(0.5f, 0.7f, 0) + basePosition,  // Left Upper Thigh
            new Vector3(-0.5f, 0.7f, 0) + basePosition,   // Right Upper Thigh
        };

        for (int i = 0; i < points.Length; i++)
        {
            points[i].transform.localPosition = initialPositions[i];
        }
    }
    private void HandleMessage(string message)
{

    string[] lines = message.Split('|');

    for (int i = 0; i < lines.Length; i++)
    {
        if (string.IsNullOrWhiteSpace(lines[i]))
            continue;

        string[] s = lines[i].Split(',');
        if (s.Length < 4) continue;

            int index = int.Parse(s[0]);
            float x = float.Parse(s[1]);
            float y = float.Parse(s[2]);
            float z = float.Parse(s[3]);

            // // Ensure index is within bounds
            // if (index < 0 || index >= points.Length)
            // {
            //     Debug.LogWarning($"Index {index} is out of bounds.");
            //     continue;
            // }

            // Transform the position of each landmark
            points[index].transform.localPosition = new Vector3(x, -y, z);

            // Debug log each value
            Debug.Log($"Index: {index}, X: {x}, Y: {y}, Z: {z}");
        
    }
}



    private async void OnApplicationQuit()
    {
        await webSocket.Close();
    }

    const int LANDMARK_COUNT = 33;
    const int LINES_COUNT = 11;

    public struct AccumulatedBuffer
    {
        public Vector3 value;
        public int accumulatedValuesCount;
        public AccumulatedBuffer(Vector3 v, int ac)
        {
            value = v;
            accumulatedValuesCount = ac;
        }
    }

    public class Body
    {
        public Transform parent;
        public AccumulatedBuffer[] positionsBuffer = new AccumulatedBuffer[LANDMARK_COUNT];
        public Vector3[] localPositionTargets = new Vector3[LANDMARK_COUNT];
        public GameObject[] instances = new GameObject[LANDMARK_COUNT];
        public LineRenderer[] lines = new LineRenderer[LINES_COUNT];

        public bool active;

        public Body(Transform parent, GameObject landmarkPrefab, GameObject linePrefab, float s, GameObject headPrefab)
        {
            this.parent = parent;
            for (int i = 0; i < instances.Length; ++i)
            {
                instances[i] = Object.Instantiate(landmarkPrefab);
                instances[i].transform.localScale = Vector3.one * s;
                instances[i].transform.parent = parent;
                instances[i].name = ((Landmark)i).ToString();
            }
            for (int i = 0; i < lines.Length; ++i)
            {
                lines[i] = Object.Instantiate(linePrefab).GetComponent<LineRenderer>();
                lines[i].transform.parent = parent;
            }

            if (headPrefab)
            {
                GameObject head = Object.Instantiate(headPrefab);
                head.transform.parent = instances[(int)Landmark.NOSE].transform;
                head.transform.localPosition = headPrefab.transform.position;
                head.transform.localRotation = headPrefab.transform.localRotation;
                head.transform.localScale = headPrefab.transform.localScale;
            }
        }
        public void UpdateLines()
        {
            lines[0].positionCount = 4;
            lines[0].SetPosition(0, Position((Landmark)32));
            lines[0].SetPosition(1, Position((Landmark)30));
            lines[0].SetPosition(2, Position((Landmark)28));
            lines[0].SetPosition(3, Position((Landmark)32));
            lines[1].positionCount = 4;
            lines[1].SetPosition(0, Position((Landmark)31));
            lines[1].SetPosition(1, Position((Landmark)29));
            lines[1].SetPosition(2, Position((Landmark)27));
            lines[1].SetPosition(3, Position((Landmark)31));

            lines[2].positionCount = 3;
            lines[2].SetPosition(0, Position((Landmark)28));
            lines[2].SetPosition(1, Position((Landmark)26));
            lines[2].SetPosition(2, Position((Landmark)24));
            lines[3].positionCount = 3;
            lines[3].SetPosition(0, Position((Landmark)27));
            lines[3].SetPosition(1, Position((Landmark)25));
            lines[3].SetPosition(2, Position((Landmark)23));

            lines[4].positionCount = 5;
            lines[4].SetPosition(0, Position((Landmark)24));
            lines[4].SetPosition(1, Position((Landmark)23));
            lines[4].SetPosition(2, Position((Landmark)11));
            lines[4].SetPosition(3, Position((Landmark)12));
            lines[4].SetPosition(4, Position((Landmark)24));

            lines[5].positionCount = 4;
            lines[5].SetPosition(0, Position((Landmark)12));
            lines[5].SetPosition(1, Position((Landmark)14));
            lines[5].SetPosition(2, Position((Landmark)16));
            lines[5].SetPosition(3, Position((Landmark)22));
            lines[6].positionCount = 4;
            lines[6].SetPosition(0, Position((Landmark)11));
            lines[6].SetPosition(1, Position((Landmark)13));
            lines[6].SetPosition(2, Position((Landmark)15));
            lines[6].SetPosition(3, Position((Landmark)21));

            lines[7].positionCount = 4;
            lines[7].SetPosition(0, Position((Landmark)16));
            lines[7].SetPosition(1, Position((Landmark)18));
            lines[7].SetPosition(2, Position((Landmark)20));
            lines[7].SetPosition(3, Position((Landmark)16));
            lines[8].positionCount = 4;
            lines[8].SetPosition(0, Position((Landmark)15));
            lines[8].SetPosition(1, Position((Landmark)17));
            lines[8].SetPosition(2, Position((Landmark)19));
            lines[8].SetPosition(3, Position((Landmark)15));

            lines[9].positionCount = 2;
            lines[9].SetPosition(0, Position((Landmark)10));
            lines[9].SetPosition(1, Position((Landmark)9));


            lines[10].positionCount = 5;
            lines[10].SetPosition(0, Position((Landmark)8));
            lines[10].SetPosition(1, Position((Landmark)5));
            lines[10].SetPosition(2, Position((Landmark)0));
            lines[10].SetPosition(3, Position((Landmark)2));
            lines[10].SetPosition(4, Position((Landmark)7));
        }
        public Vector3 Direction(Landmark from, Landmark to)
        {
            return (instances[(int)to].transform.position - instances[(int)from].transform.position).normalized;
        }
        public float Distance(Landmark from, Landmark to)
        {
            return (instances[(int)from].transform.position - instances[(int)to].transform.position).magnitude;
        }
        public Vector3 LocalPosition(Landmark Mark)
        {
            return instances[(int)Mark].transform.localPosition;
        }
        public Vector3 Position(Landmark Mark)
        {
            return instances[(int)Mark].transform.position;
        }
    }
}