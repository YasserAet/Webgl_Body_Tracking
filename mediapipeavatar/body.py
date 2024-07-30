# # MediaPipe Body
# import mediapipe as mp
# from mediapipe.tasks import python
# from mediapipe.tasks.python import vision
# from clientUDP import ClientUDP
# import websocket


# import cv2
# import threading
# import time
# import global_vars
# import struct
# import json


# # def send_large_data(ws, data):
# #     max_chunk_size = 1024  # Adjust as necessary
# #     start = 0
# #     end = max_chunk_size
# #     data_length = len(data)
# #     while start < data_length:
# #         ws.send(data[start:end])
# #         start = end
# #         end += max_chunk_size

# ws = websocket.WebSocket()
# ws.connect("ws://localhost:5050")

# # the capture thread captures images from the WebCam on a separate thread (for performance)
# class CaptureThread(threading.Thread):
#     cap = None
#     ret = None
#     frame = None
#     isRunning = False
#     counter = 0
#     timer = 0.0
#     def run(self):
#         self.cap = cv2.VideoCapture(global_vars.CAM_INDEX) # sometimes it can take a while for certain video captures
#         if global_vars.USE_CUSTOM_CAM_SETTINGS:
#             self.cap.set(cv2.CAP_PROP_FPS, global_vars.FPS)
#             self.cap.set(cv2.CAP_PROP_FRAME_WIDTH,global_vars.WIDTH)
#             self.cap.set(cv2.CAP_PROP_FRAME_HEIGHT,global_vars.HEIGHT)

#         time.sleep(1)
        
#         print("Opened Capture @ %s fps"%str(self.cap.get(cv2.CAP_PROP_FPS)))
#         while not global_vars.KILL_THREADS:
#             self.ret, self.frame = self.cap.read()
#             self.isRunning = True
#             if global_vars.DEBUG:
#                 self.counter = self.counter+1
#                 if time.time()-self.timer>=3:
#                     print("Capture FPS: ",self.counter/(time.time()-self.timer))
#                     self.counter = 0
#                     self.timer = time.time()

# # the body thread actually does the 
# # processing of the captured images, and communication with unity
# class BodyThread(threading.Thread):
#     data = ""
#     dirty = True
#     pipe = None
#     timeSinceCheckedConnection = 0
#     timeSincePostStatistics = 0

#     def run(self):
#         mp_drawing = mp.solutions.drawing_utils
#         mp_pose = mp.solutions.pose

#         self.setup_comms()
        
#         capture = CaptureThread()
#         capture.start()

#         with mp_pose.Pose(min_detection_confidence=0.80, min_tracking_confidence=0.5, model_complexity = global_vars.MODEL_COMPLEXITY,static_image_mode = False,enable_segmentation = True) as pose: 
            
#             while not global_vars.KILL_THREADS and capture.isRunning==False:
#                 print("Waiting for camera and capture thread.")
#                 time.sleep(0.5)
#             print("Beginning capture")
                
#             while not global_vars.KILL_THREADS and capture.cap.isOpened():
#                 ti = time.time()

#                 # Fetch stuff from the capture thread
#                 ret = capture.ret
#                 image = capture.frame
                                
#                 # Image transformations and stuff
#                 image = cv2.flip(image, 1)
#                 image.flags.writeable = global_vars.DEBUG
                
#                 # Detections
#                 results = pose.process(image)
#                 tf = time.time()
                
#                 # Rendering results
#                 if global_vars.DEBUG:
#                     if time.time()-self.timeSincePostStatistics>=1:
#                         print("Theoretical Maximum FPS: %f"%(1/(tf-ti)))
#                         self.timeSincePostStatistics = time.time()
                        
#                     if results.pose_landmarks:
#                         mp_drawing.draw_landmarks(image, results.pose_landmarks, mp_pose.POSE_CONNECTIONS, 
#                                                 mp_drawing.DrawingSpec(color=(255, 100, 0), thickness=2, circle_radius=4),
#                                                 mp_drawing.DrawingSpec(color=(255, 255, 255), thickness=2, circle_radius=2),
#                                                 )
#                     cv2.imshow('Body Tracking', image)
#                     cv2.waitKey(1000)

#                 # Set up data for relay
#                 # self.data = "{\"frame\" : ["
#                 # i = 0
#                 # if results.pose_world_landmarks:
#                 #     pose_landmarks = results.pose_world_landmarks
#                 #     for i in range(0,23):
#                 #         landmarks = {
#                 #             'index': i,
#                 #             'x': pose_landmarks.landmark[i].x,
#                 #             'y': pose_landmarks.landmark[i].y,
#                 #             'z': pose_landmarks.landmark[i].z,
#                 #             }
                        
#                 #         #contatinating the larndmarks {} and sending them as a single string
#                 #         if (i == 32):
#                 #             self.data += json.dumps(landmarks)
#                 #         else:
#                 #             self.data += json.dumps(landmarks) + ","

                    
#                 #     self.data += "]}"
#                 # ws.send(self.data)
#                 # self.send_data(self.data)

#                 if results.pose_world_landmarks:
#                    pose_landmarks = results.pose_world_landmarks
#                    landmarks_list = []
#                    for i in range(23):

#                        landmarks = f"{i},{pose_landmarks.landmark[i].x},{pose_landmarks.landmark[i].y},{pose_landmarks.landmark[i].z}"
#                        landmarks_list.append(landmarks)

#                    self.data += "|".join(landmarks_list)+"|"
                       
                   
#                    ws.send(self.data)
#                    ws.send("-----------------------------end-----------------------------")
#                 #    self.send_data(self.data)
#                    # Send data in chunks
#                 # send_large_data(ws, self.data)
                   
#                 # if results.pose_landmarks:
#                 #     # Convert landmarks to JSON
#                 #     print('landmarks converted to JSON')
#                 #     landmarks = [
#                 #         {
#                 #             'i': i,
#                 #             'x': landmark.x,
#                 #             'y': landmark.y,
#                 #             'z': landmark.z,
                
#                 #         }
#                 #         for landmark in results.pose_landmarks.landmark
#                 #     ]
#                     # landmarks_json = json.dumps({'landmarks': landmarks})
#                 # print(self.data)

#                 # landmarks_json = json.dumps({'landmarks': self.data})
#                 # print(self.data)
#                 # ws.send(self.data)

                    
#                     # Send landmarks data to WebSocket server
#                 # if results.pose_landmarks:
#                 #     # Serialize pose landmarks
#                 #     landmarks_data = [{'x': landmark.x, 'y': landmark.y, 'z': landmark.z} 
#                 #                     for landmark in results.pose_landmarks.landmark]
                    
#                 #     # Include drawing specifications
#                 #     # drawing_specs = {
#                 #     #     'lines': [
#                 #     #         {'color': (255, 100, 0), 'thickness': 2, 'circle_radius': 4},
#                 #     #         {'color': (255, 255, 255), 'thickness': 2, 'circle_radius': 2}
#                 #     #     ]
#                 #     # }
                    
#                 #     # Combine data
#                 #     # data_to_send = {
#                 #     #     'landmarks': landmarks_data,
#                 #     #     'drawing_specs': drawing_specs,
#                 #     #     'fps': cv2.CAP_PROP_FPS
#                 #     # }
                    
#                 #     # Serialize to JSON and send
#                 #     ws.send(json.dumps(landmarks_data))

#                 print("Sending landmarks data to WebSocket server")

#                 # Display the image
#                 # cv2.imshow('MediaPipe Pose', image)
#                 # if cv2.waitKey(5) & 0xFF == 27:
#                 #     break



                    
#         self.pipe.close()
#         capture.cap.release()
#         cv2.destroyAllWindows()
#         ws.close()
#         pass

#     def setup_comms(self):
#         if not global_vars.USE_LEGACY_PIPES:
#             self.client = ClientUDP(global_vars.HOST,global_vars.PORT)
#             self.client.start()
#         else:
#             print("Using Pipes for interprocess communication (not supported on OSX or Linux).")
#         pass      

#     def send_data(self,message):
#         if not global_vars.USE_LEGACY_PIPES:
#             self.client.sendMessage(message)
#             pass
#         else:
#             # Maintain pipe connection.
#             if self.pipe==None and time.time()-self.timeSinceCheckedConnection>=1:
#                 try:
#                     self.pipe = open(r'\\.\pipe\UnityMediaPipeBody1', 'r+b', 0)
#                 except FileNotFoundError:
#                     print("Waiting for Unity project to run...")
#                     self.pipe = None
#                 self.timeSinceCheckedConnection = time.time()

#             if self.pipe != None:
#                 try:     
#                     s = self.data.encode('utf-8') 
#                     self.pipe.write(struct.pack('I', len(s)) + s)   
#                     self.pipe.seek(0)    
#                 except Exception as ex:  
#                     print("Failed to write to pipe. Is the unity project open?")
#                     self.pipe= None
#         pass
                        


# from mediapipe.tasks import python
# from mediapipe.tasks.python import vision
# from clientUDP import ClientUDP
# import websocket
# import threading
# import time
# import global_vars
# import struct
# import mediapipe as mp
# import cv2

# ws = websocket.WebSocket()
# ws.connect("ws://localhost:5050")


# # Initialize MediaPipe Pose
# mp_pose = mp.solutions.pose
# pose = mp_pose.Pose(min_detection_confidence=0.80, min_tracking_confidence=0.5)

# # Initialize MediaPipe Drawing Utilities
# mp_drawing = mp.solutions.drawing_utils

# # Capture video from webcam
# cap = cv2.VideoCapture(0)

# while cap.isOpened():
#     ret, frame = cap.read()
#     if not ret:
#         break

#     # Flip the frame horizontally for a later selfie-view mirror effect
#     frame = cv2.flip(frame, 1)

#     # Process the frame and get pose landmarks
#     results = pose.process(frame)

#     if results.pose_landmarks:
#         landmarks = results.pose_landmarks.landmark

#         # Define indices for upper body landmarks
#         upper_body_indices = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,11,12,13,14,15,16,17,18,19,20,21,22,23,24]

#         # Draw the upper body landmarks
#         for i in upper_body_indices:
#             landmark = landmarks[i]
#             x, y = int(landmark.x * frame.shape[1]), int(landmark.y * frame.shape[0])
#             cv2.circle(frame, (x, y), 2, (0, 255, 0), -1)
#             cv2.putText(frame, str(i), (x, y), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)

#     # Display the frame
#     cv2.imshow('Upper Body Detection', frame)

#     # Send the landmarks data to the WebSocket server
#             landmarks_list = []
#                 for i in range(23):
#                     landmarks = f"{i},{pose_landmarks.landmark[i].x},{pose_landmarks.landmark[i].y},{pose_landmarks.landmark[i].z}"
#                     landmarks_list.append(landmarks)
#                 self.data += "|".join(landmarks_list)+"|"
                    
                
#                 ws.send(self.data)
#                 ws.send("-----------------------------end-----------------------------")
#     if cv2.waitKey(1) & 0xFF == 27:  # Press 'Esc' to exit
#         break


# cap.release()
# self.pipe.close()
# cv2.destroyAllWindows()
# ws.close()
# pass

#     def setup_comms(self):
#         if not global_vars.USE_LEGACY_PIPES:
#             self.client = ClientUDP(global_vars.HOST,global_vars.PORT)
#             self.client.start()
#         else:
#             print("Using Pipes for interprocess communication (not supported on OSX or Linux).")
#         pass      

#     def send_data(self,message):
#         if not global_vars.USE_LEGACY_PIPES:
#             self.client.sendMessage(message)
#             pass
#         else:
#             # Maintain pipe connection.
#             if self.pipe==None and time.time()-self.timeSinceCheckedConnection>=1:
#                 try:
#                     self.pipe = open(r'\\.\pipe\UnityMediaPipeBody1', 'r+b', 0)
#                 except FileNotFoundError:
#                     print("Waiting for Unity project to run...")
#                     self.pipe = None
#                 self.timeSinceCheckedConnection = time.time()

#             if self.pipe != None:
#                 try:     
#                     s = self.data.encode('utf-8') 
#                     self.pipe.write(struct.pack('I', len(s)) + s)   
#                     self.pipe.seek(0)    
#                 except Exception as ex:  
#                     print("Failed to write to pipe. Is the unity project open?")
#                     self.pipe= None
#         pass


from mediapipe.tasks import python
from mediapipe.tasks.python import vision
from clientUDP import ClientUDP
import websocket
import threading
import time
import global_vars
import struct
import mediapipe as mp
import cv2

ws = websocket.WebSocket()
ws.connect("ws://localhost:5050")

# Initialize MediaPipe Pose
mp_pose = mp.solutions.pose
pose = mp_pose.Pose(min_detection_confidence=0.80, min_tracking_confidence=0.5)

# Initialize MediaPipe Drawing Utilities
mp_drawing = mp.solutions.drawing_utils

# Capture video from webcam
cap = cv2.VideoCapture(0)

# Define indices for upper body landmarks
upper_body_indices = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24]

# Define the connections for upper body landmarks
connections = [
    (0, 1), (0, 4), (4, 5), (5, 6), (1, 2), (2, 3), (3, 7), (6, 8),
    (10, 9), (11, 12), (12, 14), (14, 16), (16, 22), (16, 18), (18, 20), (20, 16),
    (11, 13), (13, 15), (15, 21), (15, 17), (17, 19), (19, 15),
    (12, 24), (11, 23),(23, 24)
]

while cap.isOpened():
    ret, frame = cap.read()
    if not ret:
        break

    # Flip the frame horizontally for a later selfie-view mirror effect
    frame = cv2.flip(frame, 1)

    # Process the frame and get pose landmarks
    results = pose.process(frame)

    if results.pose_landmarks:
        landmarks = results.pose_landmarks.landmark

        # Draw the landmarks and lines between them
        for i in upper_body_indices:
            if i < len(landmarks):
                landmark = landmarks[i]
                x, y = int(landmark.x * frame.shape[1]), int(landmark.y * frame.shape[0])
                cv2.circle(frame, (x, y), 2, (0, 255, 0), -1)
                cv2.putText(frame, str(i), (x, y), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)

        # Draw lines according to the provided connections
        for start_idx, end_idx in connections:
            if start_idx in upper_body_indices and end_idx in upper_body_indices:
                if start_idx < len(landmarks) and end_idx < len(landmarks):
                    start_landmark = landmarks[start_idx]
                    end_landmark = landmarks[end_idx]
                    start_point = (int(start_landmark.x * frame.shape[1]), int(start_landmark.y * frame.shape[0]))
                    end_point = (int(end_landmark.x * frame.shape[1]), int(end_landmark.y * frame.shape[0]))
                    cv2.line(frame, start_point, end_point, (0, 255, 0), 2)

        # Send the landmarks data to the WebSocket server
        landmarks_list = []
        for i in upper_body_indices:
            if i < len(landmarks):  # Ensure it doesn't exceed the number of landmarks available
                landmarks_data = f"{i},{results.pose_landmarks.landmark[i].x},{results.pose_landmarks.landmark[i].y},{results.pose_landmarks.landmark[i].z}"
                landmarks_list.append(landmarks_data)
        data = "|".join(landmarks_list) + "|"

        ws.send(data)
        ws.send("-----------------------------end-----------------------------")

    # Display the frame
    cv2.imshow('Upper Body Detection', frame)

    if cv2.waitKey(1) & 0xFF == 27:  # Press 'Esc' to exit
        break

cap.release()
cv2.destroyAllWindows()
ws.close()
