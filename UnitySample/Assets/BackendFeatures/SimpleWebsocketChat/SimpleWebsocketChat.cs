// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class SimpleWebsocketChat : MonoBehaviour
{
    public string loginEndpointUrl;
    public string websocketEndpointUrl;
    public Text logOutput;

    WebsocketClient websocketClient;

    // UI Fields and buttons
    public InputField usernameInput;
    public InputField JoinChannelInput;
    public InputField ChannelNameInput;
    public InputField SystemPromptInput;
    public InputField ActionInput;
    public InputField SendMessageInput;
    public Button SetUserNameButton;
    public Button JoinChannelButton;
    public Button LeaveChannelButton;
    public Button SendMessageButton;
    public Button SendMessageAiButton;
    public Button ResetConversationButton;
    public ScrollRect ChatScrollRect;
    public TMP_Dropdown AiModelDropdown;


    private bool refreshForNewMessage = false;

    // A list of messages received from the server
    private List<string> messages = new List<string>();
    private List<string> talkHistory = new List<string>();
    private List<string> actionHistory = new List<string>();
    private float messageTimer = 0.0f;

    private bool connected = false;
    
    private string currentAiModel = "";

    // Start is called before the first frame update
    void Start()
    {
        // Find the Websocket Client
        this.websocketClient = GameObject.Find("WebsocketClient").GetComponent<WebsocketClient>();

        // Set the login endpoint
        Debug.Log("Setting login endpoint");
        AWSGameSDKClient.Instance.Init(loginEndpointUrl, this.OnLoginOrRefreshError);

        // If we have existing identity, request auth token for that
        if(PlayerPrefs.GetString("user_id", "") != "" && PlayerPrefs.GetString("guest_secret", "") != "")
        {
            Debug.Log("Requesting auth token for existing identity: " + PlayerPrefs.GetString("user_id"));
            this.logOutput.text += "Requesting auth token for existing identity: " + PlayerPrefs.GetString("user_id") + "\n";
        
            AWSGameSDKClient.Instance.LoginAsGuest(PlayerPrefs.GetString("user_id"), PlayerPrefs.GetString("guest_secret"), this.OnLoginResponse);
        }
        else
        {
            Debug.Log("Requesting new identity");
            this.logOutput.text += "Requesting new identity\n";
            AWSGameSDKClient.Instance.LoginAsNewGuestUser(this.OnLoginResponse);
        }

        // Set the callbacks for the UI buttons
        this.SetUserNameButton.onClick.AddListener(this.SetUserName);
        this.JoinChannelButton.onClick.AddListener(this.JoinChannel);
        this.SendMessageButton.onClick.AddListener(this.SendMessage);
        this.SendMessageAiButton.onClick.AddListener(this.SendMessageAi);
        this.ResetConversationButton.onClick.AddListener(this.ResetConversation);
        this.LeaveChannelButton.onClick.AddListener(this.LeaveChannel);
        
        AiModelDropdown.options.Clear();
        AiModelDropdown.options.Add(new TMP_Dropdown.OptionData("nova-lite"));
        AiModelDropdown.options.Add(new TMP_Dropdown.OptionData("claude-v2"));
        AiModelDropdown.RefreshShownValue();
        AiModelDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        this.currentAiModel = AiModelDropdown.options[AiModelDropdown.value].text;
    }
    
    void OnDropdownValueChanged(int index)
    {
        Debug.Log("Change model to: " + AiModelDropdown.options[index].text);
        this.currentAiModel = AiModelDropdown.options[index].text;
    }

    // Define the callbacks for the UI buttons
    void SetUserName()
    {
        // If name field is empty, return
        if (this.usernameInput.text == "")
        {
            Debug.Log("Username field is empty");
            this.logOutput.text += "Username field is empty\n";
            return;
        }

        Debug.Log("Setting username to: " + this.usernameInput.text);
        this.logOutput.text += "Setting username to: " + this.usernameInput.text + "\n";

        // Define the SetUserNameRequest
        SetUserNameRequest request = new SetUserNameRequest();
        request.type = "set-name";
        request.payload = new UserNameData();
        request.payload.username = this.usernameInput.text;
        // Send the set-name command over JSON to the server
        this.websocketClient.SendMessage(JsonUtility.ToJson(request));

        //AWSGameSDKClient.Instance.SetUsername(this.usernameInput.text);
    }

    void JoinChannel()
    {
        // If channel field is empty, return
        if (this.JoinChannelInput.text == "")
        {
            Debug.Log("Channel field is empty");
            this.logOutput.text += "Channel field is empty\n";
            return;
        }

        Debug.Log("Joining channel: " + this.JoinChannelInput.text);
        this.logOutput.text += "Joining channel: " + this.JoinChannelInput.text + "\n";

        // Define the ChannelRequest and send over websocket
        ChannelRequest request = new ChannelRequest();
        request.type = "join";
        request.payload = new ChannelData();
        request.payload.channel = this.JoinChannelInput.text;
        this.websocketClient.SendMessage(JsonUtility.ToJson(request));
    }

    void LeaveChannel()
    {
        // If channel field is empty, return
        if (this.JoinChannelInput.text == "")
        {
            Debug.Log("Channel field is empty");
            this.logOutput.text += "Channel field is empty\n";
            return;
        }

        Debug.Log("Leaving channel: " + this.JoinChannelInput.text);
        this.logOutput.text += "Leaving channel: " + this.JoinChannelInput.text + "\n";

        // Define the ChannelRequest and send over websocket
        ChannelRequest request = new ChannelRequest();
        request.type = "leave";
        request.payload = new ChannelData();
        request.payload.channel = this.JoinChannelInput.text;
        this.websocketClient.SendMessage(JsonUtility.ToJson(request));
    }

    void SendMessage()
    {
        // If channel field is empty, return
        if (this.ChannelNameInput.text == "")
        {
            Debug.Log("Channel field is empty");
            this.logOutput.text += "Channel field is empty\n";
            return;
        }

        // If message field is empty, return
        if (this.SendMessageInput.text == "")
        {
            Debug.Log("Message field is empty");
            this.logOutput.text += "Message field is empty\n";
            return;
        }
        
        Debug.Log("Sending message to channel: " + this.ChannelNameInput.text);
        this.logOutput.text += "Sending message to channel: " + this.ChannelNameInput.text + "\n";

        // Define the MessageRequest and send over websocket
        SendMessageRequest request = new SendMessageRequest();
        request.type = "message";
        request.payload = new MessageData();
        request.payload.channel = this.ChannelNameInput.text;
        request.payload.message = this.SendMessageInput.text;
        this.websocketClient.SendMessage(JsonUtility.ToJson(request));
    }

    void ResetConversation()
    {
        this.talkHistory.Clear();
        this.actionHistory.Clear();
        this.messages.Clear();
        this.logOutput.text = "";
    }
    
    // Send to Ai
    void SendMessageAi()
    {
        // If channel field is empty, return
        if (this.ChannelNameInput.text == "")
        {
            Debug.Log("Channel field is empty");
            this.logOutput.text += "Channel field is empty\n";
            return;
        }

        // If message field is empty, return
        if (this.SendMessageInput.text == "" && this.ActionInput.text == "")
        {
            Debug.Log("Action & Message field are empty. Must fill one at least.");
            this.logOutput.text += "Action & Message field are empty. Must fill one at least.\n";
            return;
        }

        Debug.Log("Sending message to channel: " + this.ChannelNameInput.text);
        // this.logOutput.text += "Sending message to channel: " + this.ChannelNameInput.text + "\n";
        if (!string.IsNullOrEmpty(this.SendMessageInput.text))
        {
            this.messages.Add($"{this.usernameInput.text} : {this.SendMessageInput.text}");
            this.talkHistory.Add($"{this.usernameInput.text} : {this.SendMessageInput.text}");
        }

        // Define the MessageRequest and send over websocket
        SendMessageAiRequest request = new SendMessageAiRequest();
        request.type = "message_ai";
        request.payload = new MessageAiData();
        request.payload.enable_debug = true;
        request.payload.channel = this.ChannelNameInput.text;
        request.payload.message = this.SendMessageInput.text;
        request.payload.model_id = this.currentAiModel;
        request.payload.system_prompt = this.SystemPromptInput.text;//"现在你正在和其他玩家进行游戏，你和其他玩家在聊天频道中聊天。你是其中的一名玩家。你的性格很搞笑，说话精辟，人狠话不多那种。当其他玩家怀疑你是AI时，你将进入其他话题。";
        request.payload.action_history = this.ActionInput.text;
        request.payload.historyMessages = talkHistory;
        this.websocketClient.SendMessage(JsonUtility.ToJson(request));
    }

    // private new List<HistoryMessageData> BuildHistoryMessageData()
    // {
    //     List<HistoryMessageData> historyMessages = new List<HistoryMessageData>();
    //     
    // }

    // Update is called once per frame
    void Update()
    {
        //Iterate through the messages and add them to the logOutput
        foreach (string message in this.messages)
        {
            this.logOutput.text += message + "\n";
        }
        // Clean up the messages list
        this.messages.Clear();

        // Remove the first line from the log output if it's longer than 20 lines
        if (this.logOutput.text.Split('\n').Length > 20)
        {
            this.logOutput.text = this.logOutput.text.Substring(this.logOutput.text.IndexOf('\n') + 1);
        }

        if (this.refreshForNewMessage)
        {
            this.refreshForNewMessage = false;
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.logOutput.rectTransform); // 重新计算布局
            Canvas.ForceUpdateCanvases(); // 强制更新 UI
            ChatScrollRect.verticalNormalizedPosition = 0f; // 滚动到底部
            StartCoroutine(ScrollToBottom());
        }
    }
    
    IEnumerator ScrollToBottom()
    {
        yield return null; // 等待一帧
        ChatScrollRect.verticalNormalizedPosition = 0f;
    }

    // Triggered by the SDK if there's any login error or error refreshing access token
    void OnLoginOrRefreshError(string error)
    {
        Debug.LogError("Login or refresh error: " + error);
        this.logOutput.text += "Login or refresh error: " + error + "\n";

        // NOTE: You would here trigger a new log in or other remediation
    }

    async void OnLoginResponse(LoginRequestData userInfo)
    {
        Debug.Log("Login response: UserID: " + userInfo.user_id + "GuestSecret: " + userInfo.guest_secret);
        this.logOutput.text += "Login response: \nUserID: " + userInfo.user_id + " \nGuestSecret: " 
                                + userInfo.guest_secret + "\n";
        
        // Store identity to PlayerPrefs
        PlayerPrefs.SetString("user_id", userInfo.user_id);
        PlayerPrefs.SetString("guest_secret", userInfo.guest_secret);
        PlayerPrefs.Save();

        // Create the Websocket client, TODO: Could optimally be managed by the SDK with callbacks!
        //AWSGameSDKClient.Instance.InitWebsocketClient(websocketEndpointUrl, userInfo.auth_token, this.OnWebsocketError);
        websocketClient.CreateWebSocketConnection(this.websocketEndpointUrl, userInfo.auth_token, this.OnWebsocketMessage);

    }

    // Define a callback for Websocket messages
    void OnWebsocketMessage(string message)
    {
        Debug.Log("Websocket message: " + message);

        try
        {
            var aiResponse = JsonUtility.FromJson<AIResponse>(message);

            if (aiResponse != null)
            {
                if (aiResponse.type != null)
                {
                    if (aiResponse.type == "ai_response")
                    {
                        var displayText = $"{"我自己"} : {aiResponse.payload.body.response_msg}";
                        // Add to the messages list so we can display this in the main thread
                        this.messages.Add(displayText);
                        
                        this.talkHistory.Add(displayText);
                    }
                    else if (aiResponse.type == "ai_debug")
                    {
                        var displayText = $"ai_debug: {aiResponse.payload.body.response_msg}";
                        this.messages.Add(displayText);
                        Debug.LogWarning(displayText);
                    }
                    
                    this.refreshForNewMessage = true;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        
        // TODO: Check the message and mark us as successfully connected
        this.connected = true;
    }
}
