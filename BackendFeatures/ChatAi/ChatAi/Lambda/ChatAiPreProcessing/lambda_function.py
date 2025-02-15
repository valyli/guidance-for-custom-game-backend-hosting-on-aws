import json
import re

def lambda_handler(event, context):

    is_valid = True
    if re.search(r'\bai\b', event["message"], re.IGNORECASE):
        is_valid = False

    return {
        'statusCode': 200 if is_valid else 300,
        'body': {
            'response_msg': event["message"] if is_valid else "Invalid message, should stop AI"
        }
    }
