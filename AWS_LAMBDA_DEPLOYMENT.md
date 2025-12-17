# AWS Lambda Deployment Guide for .NET API

## ⚠️ Important Considerations

**AWS Lambda is designed for serverless functions, not full web applications.** While it's possible to deploy ASP.NET Core to Lambda, there are important trade-offs:

### Limitations:
- ❌ **Cold starts**: First request can be slow (1-3 seconds)
- ❌ **Request timeout**: Maximum 15 minutes (API Gateway: 30 seconds)
- ❌ **Not ideal for long-running requests**
- ❌ **More complex setup** than Elastic Beanstalk
- ❌ **Cost can be higher** for high-traffic applications

### When Lambda Makes Sense:
- ✅ Low to moderate traffic
- ✅ Event-driven architecture
- ✅ Cost-effective for sporadic usage
- ✅ Auto-scaling without managing servers
- ✅ Integration with other AWS services

### When to Use Elastic Beanstalk Instead:
- ✅ High traffic applications
- ✅ Need consistent performance (no cold starts)
- ✅ Long-running requests
- ✅ Simpler deployment
- ✅ Traditional web API architecture

---

## Option 1: Lambda with Container Image (Recommended for ASP.NET Core)

This approach packages your ASP.NET Core app as a container and runs it on Lambda using the Lambda Web Adapter.

### Prerequisites

1. **AWS CLI installed and configured**
   ```bash
   aws --version
   aws configure
   ```

2. **Docker installed**
   ```bash
   docker --version
   ```

3. **.NET 8 SDK installed** (for building)

### Step 1: Create Lambda-Compatible Dockerfile

Create `Dockerfile.lambda` in your project root:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY FleetManagement.Api/FleetManagement.Api.csproj FleetManagement.Api/
COPY FleetManagement.Services/FleetManagement.Services.csproj FleetManagement.Services/
COPY FleetManagement.Data/FleetManagement.Data.csproj FleetManagement.Data/

RUN dotnet restore FleetManagement.Api/FleetManagement.Api.csproj

# Copy everything and build
COPY . .
WORKDIR /src/FleetManagement.Api
RUN dotnet build -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage with Lambda Web Adapter
FROM public.ecr.aws/lambda/dotnet:8 AS runtime
WORKDIR /var/task

# Install Lambda Web Adapter
COPY --from=public.ecr.aws/aws-observability/aws-lambda-adapter:latest /opt/extensions/ /opt/extensions/

# Copy published app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080
ENV _LAMBDA_TELEMETRY_LOG_FD=3

# Lambda handler (Web Adapter handles this)
CMD [ "FleetManagement.Api.dll" ]
```

### Step 2: Build and Push Docker Image to ECR

```bash
# Set variables
AWS_REGION=us-east-1
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
IMAGE_NAME=fleetmanagement-api-lambda
REPOSITORY_URI=$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$IMAGE_NAME

# Create ECR repository
aws ecr create-repository \
  --repository-name $IMAGE_NAME \
  --region $AWS_REGION

# Get login token
aws ecr get-login-password --region $AWS_REGION | \
  docker login --username AWS --password-stdin $REPOSITORY_URI

# Build image
docker build -f Dockerfile.lambda -t $IMAGE_NAME .

# Tag image
docker tag $IMAGE_NAME:latest $REPOSITORY_URI:latest

# Push image
docker push $REPOSITORY_URI:latest
```

### Step 3: Create Lambda Function

```bash
# Create Lambda function from container image
aws lambda create-function \
  --function-name fleetmanagement-api \
  --package-type Image \
  --code ImageUri=$REPOSITORY_URI:latest \
  --role arn:aws:iam::$AWS_ACCOUNT_ID:role/lambda-execution-role \
  --timeout 30 \
  --memory-size 512 \
  --region $AWS_REGION
```

**Note**: You'll need to create an IAM role first (see Step 4).

### Step 4: Create IAM Role for Lambda

```bash
# Create trust policy
cat > trust-policy.json <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "lambda.amazonaws.com"
      },
      "Action": "sts:AssumeRole"
    }
  ]
}
EOF

# Create role
aws iam create-role \
  --role-name lambda-execution-role \
  --assume-role-policy-document file://trust-policy.json

# Attach basic execution policy
aws iam attach-role-policy \
  --role-name lambda-execution-role \
  --policy-arn arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole
```

### Step 5: Create API Gateway

```bash
# Create REST API
aws apigateway create-rest-api \
  --name fleetmanagement-api \
  --region $AWS_REGION

# Note the API ID from output, then:
API_ID=your-api-id

# Create resource (proxy)
aws apigateway create-resource \
  --rest-api-id $API_ID \
  --parent-id $(aws apigateway get-resources --rest-api-id $API_ID --query 'items[0].id' --output text) \
  --path-part '{proxy+}' \
  --region $API_REGION

# Create ANY method
RESOURCE_ID=your-resource-id
aws apigateway put-method \
  --rest-api-id $API_ID \
  --resource-id $RESOURCE_ID \
  --http-method ANY \
  --authorization-type NONE \
  --region $AWS_REGION

# Set up Lambda integration
LAMBDA_ARN=arn:aws:lambda:$AWS_REGION:$AWS_ACCOUNT_ID:function:fleetmanagement-api
aws apigateway put-integration \
  --rest-api-id $API_ID \
  --resource-id $RESOURCE_ID \
  --http-method ANY \
  --type AWS_PROXY \
  --integration-http-method POST \
  --uri arn:aws:apigateway:$AWS_REGION:lambda:path/2015-03-31/functions/$LAMBDA_ARN/invocations \
  --region $AWS_REGION

# Deploy API
aws apigateway create-deployment \
  --rest-api-id $API_ID \
  --stage-name prod \
  --region $AWS_REGION
```

### Step 6: Update Program.cs for Lambda

Modify `FleetManagement.Api/Program.cs`:

```csharp
using Amazon.Lambda.AspNetCoreServer;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercelAndLocalhost", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                if (origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:"))
                    return true;
                if (origin.EndsWith(".vercel.app") || origin.EndsWith(".vercel.app/"))
                    return true;
                return false;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseCors("AllowVercelAndLocalhost");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fleet Management API v1");
    c.RoutePrefix = "swagger";
});

// Your endpoints
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Lambda entry point
var lambdaEntryPoint = new LambdaEntryPoint();
var handler = lambdaEntryPoint.CreateHandler(app);

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

### Step 7: Add Lambda NuGet Package

Add to `FleetManagement.Api/FleetManagement.Api.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Amazon.Lambda.AspNetCoreServer" Version="8.0.0" />
</ItemGroup>
```

### Step 8: Create Lambda Entry Point

Create `FleetManagement.Api/LambdaEntryPoint.cs`:

```csharp
using Amazon.Lambda.AspNetCoreServer;

namespace FleetManagement.Api;

public class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder
            .UseStartup<Startup>()
            .UseLambdaServer();
    }
}
```

Actually, for minimal APIs, use this simpler approach:

```csharp
using Amazon.Lambda.AspNetCoreServer;

namespace FleetManagement.Api;

public class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder
            .ConfigureAppConfiguration((context, config) =>
            {
                // Add configuration sources
            })
            .UseLambdaServer();
    }
}
```

---

## Option 2: AWS Lambda Web Adapter (Simpler Approach)

This uses AWS Lambda Web Adapter which makes it easier to run ASP.NET Core on Lambda.

### Step 1: Update Dockerfile

Use the Lambda Web Adapter approach:

```dockerfile
FROM public.ecr.aws/lambda/dotnet:8 AS base
WORKDIR /var/task

# Copy Lambda Web Adapter
COPY --from=public.ecr.aws/aws-observability/aws-lambda-adapter:latest /opt/extensions/ /opt/extensions/

# Copy your published app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080
ENV _LAMBDA_TELEMETRY_LOG_FD=3

CMD [ "FleetManagement.Api.dll" ]
```

### Step 2: Build and Deploy

Follow the same ECR and Lambda creation steps as Option 1, but use this Dockerfile.

---

## Option 3: Serverless Framework (Easiest)

Use the Serverless Framework to simplify deployment.

### Step 1: Install Serverless Framework

```bash
npm install -g serverless
npm install serverless-dotnet
```

### Step 2: Create serverless.yml

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

### Step 3: Deploy

```bash
serverless deploy
```

---

## Option 4: AWS SAM (AWS Serverless Application Model)

### Step 1: Install AWS SAM CLI

```bash
# Windows
winget install Amazon.SAM-CLI
```

### Step 2: Create template.yaml

```yaml
AWSTemplateFormatVersion: '2010-09-09'
Transform: AWS::Serverless-2016-10-31

Resources:
  FleetManagementApi:
    Type: AWS::Serverless::Function
    Properties:
      PackageType: Image
      ImageUri: YOUR_ECR_URI:latest
      Timeout: 30
      MemorySize: 512
      Events:
        Api:
          Type: HttpApi
          Properties:
            Path: /{proxy+}
            Method: ANY

Outputs:
  ApiUrl:
    Description: "API Gateway endpoint URL"
    Value: !Sub "https://${ServerlessHttpApi}.execute-api.${AWS::Region}.amazonaws.com/"
```

### Step 3: Build and Deploy

```bash
sam build
sam deploy --guided
```

---

## Configuration for Lambda

### Environment Variables

Set in Lambda function configuration:

```bash
aws lambda update-function-configuration \
  --function-name fleetmanagement-api \
  --environment Variables="{
    ASPNETCORE_ENVIRONMENT=Production,
    ConnectionStrings__SqlServer=your-connection-string
  }"
```

### Memory and Timeout

```bash
aws lambda update-function-configuration \
  --function-name fleetmanagement-api \
  --memory-size 1024 \
  --timeout 30
```

**Recommendations:**
- **Memory**: 512MB minimum, 1024MB recommended
- **Timeout**: 30 seconds (API Gateway limit), or 15 minutes for Function URL

---

## Cost Comparison

| Service | Free Tier | Cost (1M requests) | Best For |
|---------|-----------|-------------------|----------|
| **Lambda** | 1M requests/month | ~$0.20 + compute | Low traffic |
| **Elastic Beanstalk** | 750 hrs/month | ~$15-30/month | High traffic |
| **App Runner** | None | ~$10-20/month | Medium traffic |

**Lambda is cost-effective for:**
- < 1M requests/month
- Sporadic traffic
- Event-driven workloads

**Elastic Beanstalk is better for:**
- > 1M requests/month
- Consistent traffic
- Traditional web APIs

---

## Integration with Vercel

After deploying to Lambda, update `vercel.json`:

```json
{
  "version": 2,
  "framework": "nextjs",
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://YOUR_API_ID.execute-api.REGION.amazonaws.com/prod/api/:path*"
    }
  ]
}
```

Get your API Gateway URL from:
```bash
aws apigateway get-rest-apis --query 'items[?name==`fleetmanagement-api`].id' --output text
```

---

## Troubleshooting

### Cold Start Issues
- **Problem**: First request takes 1-3 seconds
- **Solution**: 
  - Use Provisioned Concurrency (extra cost)
  - Increase memory allocation
  - Optimize startup time

### Timeout Issues
- **Problem**: Requests timing out
- **Solution**:
  - Increase Lambda timeout (max 15 min)
  - Use API Gateway with longer timeout
  - Consider Elastic Beanstalk for long requests

### CORS Issues
- **Problem**: CORS errors from frontend
- **Solution**: 
  - Configure CORS in API Gateway
  - Update CORS in `Program.cs` (already done)

---

## Quick Start: Simplest Lambda Deployment

If you want the easiest path:

1. **Use Serverless Framework** (Option 3)
2. **Install**: `npm install -g serverless`
3. **Create** `serverless.yml` (see above)
4. **Deploy**: `serverless deploy`
5. **Done!**

---

## Recommendation

**For your ASP.NET Core Web API:**

1. **If low traffic (< 100K requests/month)**: Use Lambda ✅
2. **If high traffic or need consistent performance**: Use Elastic Beanstalk ✅
3. **If unsure**: Start with Elastic Beanstalk, migrate to Lambda if needed

**For most cases, Elastic Beanstalk is simpler and more suitable for traditional web APIs.**

---

## Next Steps

1. Choose deployment method (Serverless Framework recommended for simplicity)
2. Set up AWS credentials
3. Deploy using chosen method
4. Get API Gateway URL
5. Update `vercel.json` with Lambda URL
6. Test integration

---

## Resources

- **AWS Lambda .NET Documentation**: https://docs.aws.amazon.com/lambda/latest/dg/lambda-dotnet-coreclr-deployment-package.html
- **Lambda Web Adapter**: https://github.com/awslabs/aws-lambda-web-adapter
- **Serverless Framework**: https://www.serverless.com/framework/docs
- **AWS SAM**: https://docs.aws.amazon.com/serverless-application-model/

---

**Ready to deploy to Lambda? Start with Serverless Framework for the easiest experience!** 🚀

















