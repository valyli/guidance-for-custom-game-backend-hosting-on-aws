'use strict';

const AWS = require('aws-sdk');

// X-ray for distributed tracing
var AWSXRay = require('aws-xray-sdk');
AWSXRay.config([AWSXRay.plugins.ECSPlugin]);

// This sentence broke code tips. changed by valyli
// // AWS SDK with tracing
// const AWS = AWSXRay.captureAWS(require('aws-sdk'));

// AWS-provided JWT verifier
const { JwtRsaVerifier } = require("aws-jwt-verify");
const verifier = JwtRsaVerifier.create({
  issuer: process.env.ISSUER_ENDPOINT, // Get our custom issuer url from environment
  audience: "gamebackend", // set this to the expected "aud" claim to gamebackend
  jwksUri: process.env.ISSUER_ENDPOINT + "/.well-known/jwks.json", // Set the JWKS file path at issuer
  scope: ["guest", "authenticated"], // We accept guest and authenticated scope
});

// Redis manager
const RedisManager = require('./RedisManager');
const redisManager = new RedisManager(process.env.REDIS_ENDPOINT);

// Callback for pub/sub chat message handling
const listener = (message, channel) => {
  console.log("Received message from pub/sub channel");
  console.log(message, channel);

  // Send the message to all websockets subscribed to this channel
  const channelSubscribers = redisManager.channelSubscriptions.get(channel);
  if (channelSubscribers) {
    channelSubscribers.forEach((ws) => {
      try {
        // Get the message and user name from the JSON message
        const messageParsed = JSON.parse(message);
        const username = messageParsed.username;
        const messageText = messageParsed.message;
        ws.send(JSON.stringify({ type: "chat_message_received", payload: { username: username, message: messageText, channel: channel } }));
      } catch (err) {
        console.error("Error sending chat message over websocket: " + err);
      }
    });
  }
};

// WEBSOCKET SERVER on 80
const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 80 });
const url = require('url');

wss.on('connection', async (ws, req) => {
  console.log("New websocket connection");

  const params = url.parse(req.url, true);
  // If no token is found, return an error
  if (!params.query.auth_token) {
    // Reject the connection
    ws.send(JSON.stringify({ error: "No authentication token provided" }));
    ws.close();
  }

  try {
    console.log("Test upgrade ====== 1");
    var payload = await verifier.verify(params.query.auth_token);
    console.log("Token is valid");
    // Add the user to the websocket map
    redisManager.addWebsocket(ws, payload.sub);

  } catch (err) {
    console.log(err);
    ws.send(JSON.stringify({ error: "Invalid token" }));
    ws.close();
    return;
  }

  ws.send(JSON.stringify({ message: 'Successfully connected!' })); // send a message

  // Callback for message handling
  ws.on('message', function message(data) {
    console.log('received: %s', data);
    handleMessage(ws, data);
  });
    
  // Callback for disconnecting
  ws.on('close', function close() {
    try {
      redisManager.removeWebsocket(ws);
    } catch (err) {
      console.log("Error disconnecting user: " + err);
    }
  });
});

// Message handler function for messages from client through the Websocket
async function handleMessage(ws, data) {
  try {
    console.log('received: %s', data);

    const dataString = data.toString();
    const parsedData = JSON.parse(dataString);

    // Check message type

    // Set new name for the user
    if (parsedData.type === "set-name") {
      const username = parsedData.payload.username;
      const userID = redisManager.websockets.get(ws);
      redisManager.setUsername(userID, username);
      ws.send(JSON.stringify({ message: `Username set to ${username}` }));
    }

    // Subscribe to a channel
    else if (parsedData.type === "join") {
      const channel = parsedData.payload.channel;
      redisManager.subscribeToChannel(channel, ws, listener);
    }

    // Unsubscribe from a channel
    else if (parsedData.type === "leave") {
      const channel = parsedData.payload.channel;
      redisManager.unsubscribeFromChannel(channel, ws);
    }

    // Receive message to a channel
    else if (parsedData.type === "message" || parsedData.type === "message_ai") {
      const userID = redisManager.websockets.get(ws);
      const username = await redisManager.getUsername(userID);

      console.log(`Message received: ${dataString} from ${ws}`);
      const channel = parsedData.payload.channel;
      const message = parsedData.payload.message;
      const historyMessages = parsedData.payload.historyMessages;

      if (!username) {
        ws.send(JSON.stringify({ error: "You must set a username first" }));
        return;
      }
      if (!redisManager.channelSubscriptions.has(channel) || !redisManager.channelSubscriptions.get(channel).has(ws)) {
        ws.send(JSON.stringify({ error: "You must join the channel first" }));
        return;
      }

      const messageToPublish = JSON.stringify({ username, message });
      redisManager.publishToChannel(channel, messageToPublish);
      ws.send(JSON.stringify({ message: `Message sent to ${channel}: ${message}` }));

      // Send to ChatAI
      let aiMessage = "wait dev... update -- 02-13 15:35";
      ws.send(JSON.stringify({ message: `[debug msg] AI Message sent to ${channel}: ${aiMessage}` }));
      try {
        const lambda = new AWS.Lambda();
        const lambdaParams = {
          FunctionName: 'ChatAi',
          Payload: JSON.stringify({ message: message, username: username, channel: channel, historyMessages: historyMessages }), // 构建你的 payload
        };
  
        lambda.invoke(lambdaParams, function(err, data) {
          if (err) {
            console.error("Error invoking Lambda:", err);
            ws.send(JSON.stringify({ error: `Error invoking AI Lambda: ${err}` })); // 将错误发送给客户端
          } else {
            try {
              const payload = JSON.parse(data.Payload);
              console.log("Lambda Response:", payload);
              // 将 Lambda 的响应发送给客户端
              ws.send(JSON.stringify({ type: "ai_response", payload: payload })); //  假设Lambda返回了 {message: "AI的回复"}
            } catch (parseError) {
              console.error("Error parsing Lambda payload:", parseError);
              ws.send(JSON.stringify({ error: `Error parsing AI response: ${parseError}` }));
            }
          }
        });
  
      } catch (lambdaErr) {
          console.error("Lambda调用出错:", lambdaErr)
          ws.send(JSON.stringify({ error: `Error calling AI Lambda: ${lambdaErr}` }));
      }
    }

    // Any other messages
    else {
      ws.send(JSON.stringify({ error: `Invalid message - handleMessage. Unknown type == ${parsedData.type}` }));
    }
  } catch (err) {
    console.log(err);
    ws.send(JSON.stringify({ error: `Error handling message: ${err}` }));
  }
}

// *********** //

// HEALTH CHECK SERVER for load balancer on 8080

const express = require('express');
const { parse } = require('path');

// Server constants
const PORT = 8080;
const HOST = '0.0.0.0';

// Server app
const app = express();

app.use(AWSXRay.express.openSegment('SimpleWebsocketChat-HealthCheck'));
// health check for root get
app.get('/', (req, res) => {
  res.status(200).json({ statusCode: 200, message: "OK" });
});
app.use(AWSXRay.express.closeSegment());

// Setup app
app.listen(PORT, HOST, () => {
  console.log(`Running server on http://${HOST}:${PORT}`);
});