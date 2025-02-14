import json
import boto3
from botocore.exceptions import ClientError


bedrock = boto3.client("bedrock-runtime")


def invoke_claude_v2(talk_history: str, system_prompt: str):
    model_id = "anthropic.claude-v2"

    # user_input = event.get("prompt", talk_history)
    
    # Claude 需要以 "\n\nHuman:" 开头
    # formatted_prompt = f"\n\nHuman: {talk_history}\n\nAssistant:"
    formatted_prompt = f"\n\nHuman: {system_prompt}\n\nHuman: {talk_history}\n\nAssistant:"

    request_body = {
        "prompt": formatted_prompt,
        "max_tokens_to_sample": 200  # 限制输出最大 token 数
    }
    
    # 调用 Bedrock API
    response = bedrock.invoke_model(
        modelId=model_id,
        body=json.dumps(request_body)
    )

    response_body = json.loads(response["body"].read().decode("utf-8"))
    return response_body


def invoke_nova_lite(talk_history: str, system_prompt: str):    
    model_id = 'amazon.nova-lite-v1:0'

    # Define your system prompt(s).
    system_list = [
        {
            "text": system_prompt
        }
    ]

    # Define one or more messages using the "user" and "assistant" roles.
    message_list = [{"role": "user", "content": [{"text": "talk_history"}]}]

    # Configure the inference parameters.
    inf_params = {"max_new_tokens": 500, "top_p": 0.9, "top_k": 20, "temperature": 0.7}

    request_body = {
        "schemaVersion": "messages-v1",
        "messages": message_list,
        "system": system_list,
        "inferenceConfig": inf_params,
    }

 
    # Invoke the model with the response stream
    response = bedrock.invoke_model(
        modelId=model_id, body=json.dumps(request_body)
    )

    response_body = json.loads(response.get('body').read().decode('utf-8'))
    output_txt = response_body['output']['message']['content'][0]['text']
    return output_txt
