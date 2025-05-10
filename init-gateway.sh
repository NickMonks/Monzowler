#!/bin/sh

set -e

echo "⏳ Creating API Gateway and connecting it to Lambda..."

API_ID=$(aws --endpoint-url=http://localstack:4566 apigateway create-rest-api \
  --name monzowler-api \
  --region us-east-1 \
  --query 'id' --output text)

PARENT_ID=$(aws --endpoint-url=http://localstack:4566 apigateway get-resources \
  --rest-api-id "$API_ID" \
  --query 'items[0].id' --output text)

RESOURCE_ID=$(aws --endpoint-url=http://localstack:4566 apigateway create-resource \
  --rest-api-id "$API_ID" \
  --parent-id "$PARENT_ID" \
  --path-part "{proxy+}" \
  --query 'id' --output text)

aws --endpoint-url=http://localstack:4566 apigateway put-method \
  --rest-api-id "$API_ID" \
  --resource-id "$RESOURCE_ID" \
  --http-method ANY \
  --authorization-type NONE

aws --endpoint-url=http://localstack:4566 apigateway put-integration \
  --rest-api-id "$API_ID" \
  --resource-id "$RESOURCE_ID" \
  --http-method ANY \
  --type AWS_PROXY \
  --integration-http-method POST \
  --uri arn:aws:apigateway:us-east-1:lambda:path/2015-03-31/functions/arn:aws:lambda:us-east-1:000000000000:function:monzowler-api-lambda/invocations

aws --endpoint-url=http://localstack:4566 lambda add-permission \
  --function-name monzowler-api-lambda \
  --statement-id apigateway-permission \
  --action lambda:InvokeFunction \
  --principal apigateway.amazonaws.com \
  --source-arn arn:aws:execute-api:us-east-1:000000000000:"$API_ID"/*/*/{proxy+}

aws --endpoint-url=http://localstack:4566 apigateway create-deployment \
  --rest-api-id "$API_ID" \
  --stage-name dev

echo "✅ API Gateway ready!"
echo "➡️  Test it with:"
echo "curl -X POST http://localhost:4566/restapis/$API_ID/dev/_user_request_/crawl -d '{\"url\": \"https://example.com\"}' -H \"Content-Type: application/json\""
