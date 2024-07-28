using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NativeWebSocket;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;
[DefaultExecutionOrder(-1)]
public class WebSocketManager : MonoBehaviour
{
    public string host = "127.0.0.1"; // This machines host.
    public int port = 5050; // Must match the Python side.
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

    private Body body;
    WebSocket websocket;

    //  these virtual transforms are not actually provided by mediapipe pose, but are required for avatars.
    // so I just manually compute them
    private Transform virtualNeck;
    private Transform virtualHip;

    public Transform GetLandmark(int mark)
    {
        return body.instances[mark].transform;
    }
    public Transform GetVirtualNeck()
    {
        return virtualNeck;
    }
    public Transform GetVirtualHip()
    {
        return virtualHip;
    }
    async void Start()
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        body = new Body(bodyParent, landmarkPrefab, linePrefab, landmarkScale, enableHead ? headPrefab : null);
        virtualNeck = new GameObject("VirtualNeck").transform;
        virtualHip = new GameObject("VirtualHip").transform;

        websocket = new WebSocket("ws://localhost:5050");

        // Connect to the server
        await websocket.Connect();

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            var message = Encoding.UTF8.GetString(bytes);
            // Process landmarks data here
            ProcessLandmarksData(message);
        };

     
        //listen to the websocket server
        // await ConnectToWebSocketServer();
        

        //  Thread t = new Thread(new ThreadStart(Run));
        //  t.Start();
        
    }

    async void OnApplicationQuit()
    {
        await websocket.Close();
    }

    void Update()
     {
        if (body != null)
        {
            UpdateBody(body);
        }
        else
        {
            Debug.LogError("Body object is null in Update.");
        }
      //  UpdateBody(body);
    }

    private void UpdateBody(Body b)
    {
        for (int i = 0; i < LANDMARK_COUNT; ++i)
        {
            if (b.positionsBuffer[i].accumulatedValuesCount < samplesForPose)
                continue;

            b.localPositionTargets[i] = b.positionsBuffer[i].value / (float)b.positionsBuffer[i].accumulatedValuesCount * multiplier;
            b.positionsBuffer[i] = new AccumulatedBuffer(Vector3.zero, 0);
        }

        Vector3 offset = Vector3.zero;
        for (int i = 0; i < LANDMARK_COUNT; ++i)
        {
            if (b.instances[i] == null)
            {
                Debug.LogError($"Instance at index {i} is null.");
                continue;
            }
            Vector3 p = b.localPositionTargets[i] - offset;
            b.instances[i].transform.localPosition = Vector3.MoveTowards(b.instances[i].transform.localPosition, p, Time.deltaTime * maxSpeed);
        }

        virtualNeck.transform.position = (b.instances[(int)Landmark.RIGHT_SHOULDER].transform.position + b.instances[(int)Landmark.LEFT_SHOULDER].transform.position) / 2f;
        virtualHip.transform.position = (b.instances[(int)Landmark.RIGHT_HIP].transform.position + b.instances[(int)Landmark.LEFT_HIP].transform.position) / 2f;

        b.UpdateLines();
    }
     public void SetVisible(bool visible)
    {
        bodyParent.gameObject.SetActive(visible);
    }


    void ProcessLandmarksData(string message)
    {
        Debug.Log("Processing");
        Debug.Log("Raw JSON: " + message);

        // try
        // {
        //     var landmarksData = JsonUtility.FromJson<LandmarksData>(message);

        //     if (landmarksData != null)
        //     {
        //         foreach (var landmark in landmarksData.frame)
        //         {
        //             Debug.Log($"Index: {landmark.index}, X: {landmark.x}, Y: {landmark.y}, Z: {landmark.z}");
        //         }
        //     }
        //     else
        //     {
        //         Debug.LogError("Failed to parse JSON: Data is null");
        //     }
        // }
        // catch (Exception e)
        // {
        //     Debug.LogError($"Error processing landmarks data: {e.Message}");
        // }

        Body h = body;
        string jsonString = message;

        var serializer = new DataContractJsonSerializer(typeof(Root));
        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
        {
            Root root = (Root)serializer.ReadObject(stream);

            // Iterate through the frames and process their values
            foreach (var frame in root.Frames)
            {
                Debug.Log($"index: {frame.Index}, x: {frame.X}, y: {frame.Y}, z: {frame.Z}");

                int index = frame.Index;
                if (index < 0 || index >= LANDMARK_COUNT)
                {
                    Debug.LogError($"Invalid index {index} in landmarks data.");
                    continue;
                }

                Vector3 position = new Vector3(frame.X, frame.Y, frame.Z);
                body.positionsBuffer[index] = new AccumulatedBuffer(position, body.positionsBuffer[index].accumulatedValuesCount + 1);
            }
        }
    }
    //    void ProcessLandmarksData(string message)
    // {
    //    // Debug.Log("Raw JSON: " + message);

    //     try
    //     {
    //         // Parse JSON using JsonUtility
    //         var frameData = JsonUtility.FromJson<FrameData>(message);

    //         // Process the point data
    //         foreach (var point in frameData.frame)
    //         {
    //             Debug.Log($"Index: {point.index}, X: {point.x}, Y: {point.y}, Z: {point.z}");
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Debug.LogError($"Error processing landmarks data: {ex.Message}");
    //     }
    // }
    // }

    // Wrapper class to match the JSON structure

    [DataContract]
    public class Frame
    {
        [DataMember(Name = "index")]
        public int Index { get; set; }

        [DataMember(Name = "x")]
        public float X { get; set; }

        [DataMember(Name = "y")]
        public float Y { get; set; }

        [DataMember(Name = "z")]
        public float Z { get; set; }
    }


    [DataContract]
    public class Root
    {
        [DataMember(Name = "frame")]
        public List<Frame> Frames { get; set; }
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
                instances[i] = Instantiate(landmarkPrefab);
                instances[i].transform.localScale = Vector3.one * s;
                instances[i].transform.parent = parent;
                instances[i].name = ((Landmark)i).ToString();
                Debug.Log($"Created instance for landmark {(Landmark)i}");
            }
            for (int i = 0; i < lines.Length; ++i)
            {
                lines[i] = Instantiate(linePrefab).GetComponent<LineRenderer>();
                lines[i].transform.parent = parent;
                Debug.Log($"Created line renderer for line {i}");
            }

            if (headPrefab)
            {
                GameObject head = Instantiate(headPrefab);
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


                    //
                    //    lines[0].positionCount = 4;
                    // lines[0].SetPosition(0, Position((Landmark)32));
                    // lines[0].SetPosition(1, Position((Landmark)30));
                    // lines[0].SetPosition(2, Position((Landmark)28));
                    // lines[0].SetPosition(3, Position((Landmark)32));
                    // lines[1].positionCount = 4;
                    // lines[1].SetPosition(0, Position((Landmark)31));
                    // lines[1].SetPosition(1, Position((Landmark)29));
                    // lines[1].SetPosition(2, Position((Landmark)27));
                    // lines[1].SetPosition(3, Position((Landmark)31));

                    // lines[2].positionCount = 3;
                    // lines[2].SetPosition(0, Position((Landmark)28));
                    // lines[2].SetPosition(1, Position((Landmark)26));
                    // lines[2].SetPosition(2, Position((Landmark)24));
                    // lines[3].positionCount = 3;
                    // lines[3].SetPosition(0, Position((Landmark)27));
                    // lines[3].SetPosition(1, Position((Landmark)25));
                    // lines[3].SetPosition(2, Position((Landmark)23));

                    // lines[4].positionCount = 5;
                    // lines[4].SetPosition(0, Position((Landmark)24));
                    // lines[4].SetPosition(1, Position((Landmark)23));
                    // lines[4].SetPosition(2, Position((Landmark)11));
                    // lines[4].SetPosition(3, Position((Landmark)12));
                    // lines[4].SetPosition(4, Position((Landmark)24));

                    // lines[5].positionCount = 4;
                    // lines[5].SetPosition(0, Position((Landmark)12));
                    // lines[5].SetPosition(1, Position((Landmark)14));
                    // lines[5].SetPosition(2, Position((Landmark)16));
                    // lines[5].SetPosition(3, Position((Landmark)22));
                    // lines[6].positionCount = 4;
                    // lines[6].SetPosition(0, Position((Landmark)11));
                    // lines[6].SetPosition(1, Position((Landmark)13));
                    // lines[6].SetPosition(2, Position((Landmark)15));
                    // lines[6].SetPosition(3, Position((Landmark)21));

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