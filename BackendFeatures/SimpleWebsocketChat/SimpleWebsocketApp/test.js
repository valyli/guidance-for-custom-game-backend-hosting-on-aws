// Test Code tips
// 我发现把这句注释调，就正常了。知道原因吗？const AWS = AWSXRay.captureAWS(require('aws-sdk'));


const AWS = require('aws-sdk');
// const AWS = AWSXRay.captureAWS(require('aws-sdk'));

const lambda = new AWS.Lambda();

// 尝试调用 Lambda 函数 (即使参数不完整)
lambda.listFunctions({}, function(err, data) {
    if (err) {
        console.error(err);
    } else {
        console.log(data);
    }
});
