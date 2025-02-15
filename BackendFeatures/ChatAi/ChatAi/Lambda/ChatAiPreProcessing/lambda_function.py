import json
import re
from model_invoker import invoke_claude_v2, invoke_nova_lite

def lambda_handler(event, context):

    is_valid = True
    invalid_reason: str = None

    # AI
    if re.search(r'\bai\b', event["message"], re.IGNORECASE):
        is_valid = False
        invalid_reason = "AI关键字过滤"

    # 意图识别：抢用户等
    if is_valid:
        model_id = event["model_id"]
        print(model_id)
        system_prompt = "判断输入的信息，只要其中一项出现，就回答 Yes；不是，回答 No。不要多说任何话。判断条件：1.是推广营销。2.讨论政治、政党、政治人物相关话题。3.讨论编程等技术问题。4.非中文用户。5.关于AI。"
        message = event["message"]

        response_msg = None
        if model_id == "claude-v2":
            response_msg = invoke_claude_v2(message, system_prompt)
        elif model_id == "nova-lite":
            response_msg = invoke_nova_lite(message, system_prompt)

        is_valid = True
        if response_msg == "Yes":
            is_valid = False
            invalid_reason = "意图识别"


    return {
        'statusCode': 200 if is_valid else 300,
        'body': {
            'response_msg': event["message"] if is_valid else f"[Pre] Invalid message, should stop AI. r = {invalid_reason}"
        }
    }
