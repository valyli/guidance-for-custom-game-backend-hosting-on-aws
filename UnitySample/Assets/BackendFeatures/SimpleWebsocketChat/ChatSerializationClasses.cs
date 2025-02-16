
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

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
    public bool enable_debug;
    public string message;
    public string channel;
    public string model_id;
    public string system_prompt;
    public List<string> historyMessages;
    public string action_history;
    public bool enable_pre_processing;
    public bool enable_post_processing;
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
