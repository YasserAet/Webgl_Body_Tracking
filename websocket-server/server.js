const WebSocket = require('ws');

const wss = new WebSocket.Server({ port: 5050 });

wss.on('connection', (ws) => {
    console.log('Client connected');

    ws.on('message', (message) => {
        console.log(`Received message => ${message}`);
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



// const WebSocket = require('ws');

// const wss = new WebSocket.Server({ port: 5050 });

// wss.on('connection', (ws) => {
//     console.log('Client connected');

//     ws.on('message', (message) => {
//         // Assuming the message is JSON formatted, parse it
//         try {
//             // const data = JSON.parse(message);
//             console.log('Received message hada f server.js:', message);

//             // Broadcast the parsed data to all clients
//             wss.clients.forEach((client) => {
//                 if (client.readyState === WebSocket.OPEN) {
//                     // client.send(JSON.stringify(data));
//                     console.log('Data sent from server.js');
//                 }
//             });
//         } catch (error) {
//             console.error('Error parsing JSON:', error);
//         }
//     });

//     ws.on('close', () => {
//         console.log('Client disconnected');
//     });
// });

// console.log('WebSocket server is running on ws://localhost:5050');
