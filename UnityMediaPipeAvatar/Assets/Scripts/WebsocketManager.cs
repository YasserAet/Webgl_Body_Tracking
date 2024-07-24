using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NativeWebSocket;

public class WebSocketManager : MonoBehaviour
{
    WebSocket websocket;

    async void Start()
    {
        websocket = new WebSocket("ws://localhost:5050");

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
            Debug.Log("OnMessage! " + message);
            // Process landmarks data here
            ProcessLandmarksData(message);
        };

        // Connect to the server
        await websocket.Connect();
    }

    async void OnApplicationQuit()
    {
        await websocket.Close();
    }

    public async void SendMessage(string message)
    {
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(message);
        }
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
    }

    void ProcessLandmarksData(string message)
    {
        // Parse JSON and handle landmarks data
        var landmarksData = JsonUtility.FromJson<LandmarksData>(message);
        // Update Unity object using pipeserver.cs script
        print(landmarksData);
    }
}

[Serializable]
public class LandmarksData
{
    public List<Landmark> landmarks;
}