using System.Collections.Generic;

[System.Serializable] // Unity 需要这个特性来序列化/反序列化
public class Payload
{
    public int statusCode;
    public Body body;
}

[System.Serializable]
public class Body
{
    public string message;
    public Event @event;
    public string response_msg;
}

[System.Serializable]
public class Event
{
    public string message;
    public string username;
    public string channel;
    public List<string> historyMessages;
}

[System.Serializable]
public class Response
{
    public string type;
    public string completion;
    public string stop_reason;
    public string stop;
}

[System.Serializable]
public class AIResponse
{
    public string type;
    public Payload payload;
}