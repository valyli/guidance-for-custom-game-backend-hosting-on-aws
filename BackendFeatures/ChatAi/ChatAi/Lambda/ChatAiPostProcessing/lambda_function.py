import json
import boto3
from botocore.exceptions import ClientError
from model_invoker import invoke_claude_v2, invoke_nova_lite

bedrock = boto3.client("bedrock-runtime")


def lambda_handler(event, context):

    print(event)
    model_id = event["model_id"]
    print(model_id)
    system_prompt = "判断输入的信息，只要其中一项出现，就回答 Yes；不是，回答 No。不要多说任何话。判断条件：1.像AI或者机器人在说话？2.说话太啰嗦。3.不像真人在说话。4.承认自己是AI或者机器人。"
    message = event["message"]

    response_msg = None
    if model_id == "claude-v2":
        response_msg = invoke_claude_v2(message, system_prompt)
    elif model_id == "nova-lite":
        response_msg = invoke_nova_lite(message, system_prompt)

    print(response_msg)
    is_valid = True
    if response_msg == "Yes":
        is_valid = False

    return {
        'statusCode': 200 if is_valid else 300,
        'body': {
            'response_msg': event["message"] if is_valid else "Invalid message, should stop AI"
        }
    }
