# AWS Lambda Quick Start Guide

## Fastest Way to Deploy to Lambda

### Prerequisites
- AWS CLI installed and configured
- .NET 8 SDK installed
- Docker installed (for container approach)

---

## Method 1: Serverless Framework (Easiest) ⭐ Recommended

### Step 1: Install Serverless Framework
```bash
npm install -g serverless
```

### Step 2: Install Lambda Tools
```bash
dotnet tool install -g Amazon.Lambda.Tools
dotnet new --install Amazon.Lambda.Templates
```

### Step 3: Add Lambda Package
Add to `FleetManagement.Api/FleetManagement.Api.csproj`:
```xml
<ItemGroup>
  <PackageReference Include="Amazon.Lambda.AspNetCoreServer" Version="8.0.0" />
</ItemGroup>
```

### Step 4: Create Lambda Entry Point
Create `FleetManagement.Api/LambdaEntryPoint.cs`:
```csharp
using Amazon.Lambda.AspNetCoreServer;

namespace FleetManagement.Api;

public class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder.UseLambdaServer();
    }
}
```

### Step 5: Create serverless.yml
Create `serverless.yml` in project root:
```yaml
service: fleetmanagement-api

provider:
  name: aws
  runtime: provided.al2
  region: us-east-1
  memorySize: 512
  timeout: 30
  httpApi:
    cors: true

functions:
  api:
    handler: FleetManagement.Api::FleetManagement.Api.LambdaEntryPoint::FunctionHandlerAsync
    package:
      artifact: bin/Release/net8.0/publish.zip
    events:
      - httpApi:
          path: /{proxy+}
          method: ANY
      - httpApi:
          path: /
          method: ANY

plugins:
  - serverless-dotnet
```

### Step 6: Deploy
```bash
cd FleetManagement.Api
dotnet publish -c Release
cd ..
serverless deploy
```

### Step 7: Get Your URL
After deployment, Serverless will output your API Gateway URL:
```
endpoints:
  ANY - https://xxxxx.execute-api.us-east-1.amazonaws.com/{proxy+}
```

---

## Method 2: Container Image (More Control)

### Step 1: Create Dockerfile.lambda
```dockerfile
FROM public.ecr.aws/lambda/dotnet:8 AS base
WORKDIR /var/task

# Copy Lambda Web Adapter
COPY --from=public.ecr.aws/aws-observability/aws-lambda-adapter:latest /opt/extensions/ /opt/extensions/

# Copy published app (from build stage)
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080

CMD [ "FleetManagement.Api.dll" ]
```

### Step 2: Build and Push to ECR
```bash
# Set variables
AWS_REGION=us-east-1
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
IMAGE_NAME=fleetmanagement-api-lambda

# Create repository
aws ecr create-repository --repository-name $IMAGE_NAME --region $AWS_REGION

# Login
aws ecr get-login-password --region $AWS_REGION | \
  docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com

# Build
docker build -f Dockerfile.lambda -t $IMAGE_NAME .

# Tag and push
docker tag $IMAGE_NAME:latest $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$IMAGE_NAME:latest
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$IMAGE_NAME:latest
```

### Step 3: Create Lambda Function
```bash
aws lambda create-function \
  --function-name fleetmanagement-api \
  --package-type Image \
  --code ImageUri=$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$IMAGE_NAME:latest \
  --role arn:aws:iam::$AWS_ACCOUNT_ID:role/lambda-execution-role \
  --timeout 30 \
  --memory-size 512
```

### Step 4: Create API Gateway
Use AWS Console or CLI to create HTTP API and connect to Lambda.

---

## Update vercel.json

After deployment, update `vercel.json`:
```json
{
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://YOUR_API_ID.execute-api.REGION.amazonaws.com/prod/api/:path*"
    }
  ]
}
```

---

## Quick Comparison

| Method | Difficulty | Best For |
|--------|-----------|----------|
| **Serverless Framework** | ⭐ Easy | Quick deployment |
| **Container Image** | ⭐⭐ Medium | More control |
| **AWS SAM** | ⭐⭐⭐ Hard | Complex setups |

---

## Troubleshooting

### Cold Starts
- First request takes 1-3 seconds
- Use Provisioned Concurrency (extra cost) to avoid

### Timeout
- API Gateway: 30 seconds max
- Lambda: 15 minutes max (but API Gateway limits to 30s)

### CORS
- Already configured in `Program.cs`
- Also configure in API Gateway settings

---

## Cost Estimate

**Lambda Pricing:**
- Free tier: 1M requests/month
- After: $0.20 per 1M requests + compute time
- **Estimated**: $5-20/month for moderate traffic

**vs Elastic Beanstalk:**
- Fixed cost: ~$15-30/month
- **Lambda cheaper** for < 1M requests/month

---

## Recommendation

**Start with Serverless Framework** - it's the easiest way to deploy to Lambda!

For detailed instructions, see [AWS_LAMBDA_DEPLOYMENT.md](./AWS_LAMBDA_DEPLOYMENT.md)

















