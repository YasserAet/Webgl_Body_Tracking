const WebSocket = require('ws');

const wss = new WebSocket.Server({ port: 5050 });

wss.on('connection', (ws) => {
    console.log('Client connected');

    ws.on('message', (message) => {
         console.log(`Received message => ${message}`);
            // var data = JSON.parse(message);
            // console.log('index:', data.index);
            // console.log('x:', data.x);
            // console.log('y:', data.y);
            // console.log('z:', data.z);
            
            // const response = JSON.stringify({ message: 'Data received', status: 'success' });
            ws.send(message);
        // Broadcast the message to all clients
        wss.clients.forEach((client) => {
            if (client.readyState === WebSocket.OPEN) {
                client.send(message);
                console.log('data sent from server.js')
            }
        });
    });

    ws.on('close', () => {
        console.log('Client disconnected');
    });
});

console.log('WebSocket server is running on ws://localhost:5050');