import json
import boto3
from botocore.exceptions import ClientError

bedrock = boto3.client("bedrock-runtime")


def lambda_handler(event, context):

    print("{}".format(event))
    talk_history = "\n ".join(event["historyMessages"])

    model_id = "anthropic.claude-v2"  # 也可以使用 "amazon.titan-text-lite-v1" 等
    
    # prompt = event.get("prompt", "Hello, how can I help you?")
    user_input = event.get("prompt", talk_history)
    
    # Claude 需要以 "\n\nHuman:" 开头
    formatted_prompt = f"\n\nHuman: {user_input}\n\nAssistant:"

    request_body = {
        "prompt": formatted_prompt,
        "max_tokens_to_sample": 200  # 限制输出最大 token 数
    }
    
    # 调用 Bedrock API
    response = bedrock.invoke_model(
        modelId=model_id,
        body=json.dumps(request_body)
    )
    
    # 解析返回结果
    response_body = json.loads(response["body"].read().decode("utf-8"))
    
    return {
        'statusCode': 200,
        # 'body': json.dumps('Hello from Lambda!')
        'body': {
            'message': 'Hello from Lambda!',
            'event': event,
            'response': response_body
        }
    }
