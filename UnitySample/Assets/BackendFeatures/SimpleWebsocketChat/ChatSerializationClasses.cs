
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class UserNameData {
    public string username;
}

[Serializable]
public class SetUserNameRequest
{
    public string type;
    // Define the payload as a
    public UserNameData payload;
}

[Serializable]
public class ChannelData
{
    public string channel;
}

[Serializable]
public class ChannelRequest
{
    public string type;
    // Define the payload as a
    public ChannelData payload;
}

[Serializable]
public class MessageData
{
    public string message;
    public string channel;
}

// [Serializable]
// public class HistoryMessageData
// {
//     public string who;
//     public string message;
// }

[Serializable]
public class MessageAiData
{
    public string message;
    public string channel;
    public List<string> historyMessages;
}

[Serializable]
public class SendMessageRequest
{
    public string type;
    // Define the payload as a
    public MessageData payload;
}

[Serializable]
public class SendMessageAiRequest
{
    public string type;
    // Define the payload as a
    public MessageAiData payload;
}
