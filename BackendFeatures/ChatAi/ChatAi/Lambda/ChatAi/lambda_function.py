import json
import boto3
from botocore.exceptions import ClientError
from model_invoker import invoke_claude_v2, invoke_nova_lite

bedrock = boto3.client("bedrock-runtime")


def lambda_handler(event, context):

    model_id = event["model_id"]
    print(model_id)
    system_prompt = event["system_prompt"]
    talk_history = "\n ".join(event["historyMessages"])

    response_msg = None
    if model_id == "claude-v2":
        response_msg = invoke_claude_v2(talk_history, system_prompt)
    elif model_id == "nova-lite":
        response_msg = invoke_nova_lite(talk_history, system_prompt)
        
    return {
        'statusCode': 200,
        # 'body': json.dumps('Hello from Lambda!')
        'body': {
            'response_msg': response_msg
        }
    }
