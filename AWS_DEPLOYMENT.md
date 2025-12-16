# AWS Deployment Guide for .NET API

## ✅ Yes, You Can Deploy to AWS!

AWS provides excellent support for .NET applications. Here are the best options:

---

## Option 1: AWS Elastic Beanstalk (Easiest) ⭐ Recommended

**Why Elastic Beanstalk:**
- ✅ Automatic .NET runtime support
- ✅ Easy deployment via Visual Studio or CLI
- ✅ Auto-scaling and load balancing included
- ✅ Free tier available (t2.micro instances)
- ✅ Minimal configuration needed

### Steps to Deploy

#### Method A: Visual Studio (Easiest)

1. **Install AWS Toolkit for Visual Studio**
   - Download from: https://aws.amazon.com/visualstudio/
   - Or via Visual Studio Extensions

2. **Publish to AWS**
   - Right-click `FleetManagement.Api` project
   - Select "Publish to AWS Elastic Beanstalk"
   - Follow the wizard:
     - Create new application
     - Choose .NET 8.0 runtime
     - Select instance type (t3.micro for free tier)
     - Configure environment

3. **Set Environment Variables**
   - In AWS Console → Elastic Beanstalk → Configuration → Software
   - Add environment variables:
     - `ASPNETCORE_ENVIRONMENT=Production`
     - `ConnectionStrings__SqlServer=<your-connection-string>`

#### Method B: AWS CLI

1. **Install AWS CLI**
   ```bash
   # Windows (PowerShell)
   winget install Amazon.AWSCLI
   
   # Or download from: https://aws.amazon.com/cli/
   ```

2. **Install EB CLI**
   ```bash
   pip install awsebcli
   ```

3. **Initialize Elastic Beanstalk**
   ```bash
   cd FleetManagement.Api
   eb init -p "64bit Amazon Linux 2023 v2.0.0 running .NET 8" fleetmanagement-api
   ```

4. **Create and Deploy**
   ```bash
   eb create fleetmanagement-env
   eb deploy
   ```

5. **Get Your URL**
   ```bash
   eb status
   # Your API will be at: http://fleetmanagement-env.elasticbeanstalk.com
   ```

### Configuration Files

Create `.ebextensions/app.config`:

```yaml
option_settings:
  aws:elasticbeanstalk:application:environment:
    ASPNETCORE_ENVIRONMENT: Production
    ConnectionStrings__SqlServer: "Server=your-server;Database=your-db;..."
```

### Cost
- **Free Tier**: 750 hours/month of t2.micro/t3.micro (12 months)
- **After Free Tier**: ~$15-30/month depending on instance size

---

## Option 2: AWS App Runner (Container-Based)

**Why App Runner:**
- ✅ Fully managed container service
- ✅ Auto-scaling
- ✅ Pay only for what you use
- ✅ Great for Docker deployments

### Steps

1. **Build and Push Docker Image**
   ```bash
   # Build image
   docker build -t fleetmanagement-api .
   
   # Tag for ECR
   docker tag fleetmanagement-api:latest \
     YOUR_ACCOUNT_ID.dkr.ecr.REGION.amazonaws.com/fleetmanagement-api:latest
   
   # Push to ECR
   aws ecr create-repository --repository-name fleetmanagement-api
   docker push YOUR_ACCOUNT_ID.dkr.ecr.REGION.amazonaws.com/fleetmanagement-api:latest
   ```

2. **Create App Runner Service**
   - Go to AWS Console → App Runner
   - Create service
   - Select "Container registry" → ECR
   - Choose your image
   - Configure:
     - Port: 8080
     - Environment variables
   - Deploy!

3. **Get Your URL**
   - App Runner provides: `https://your-service.REGION.awsapprunner.com`

### Cost
- **Free Tier**: None
- **Pricing**: ~$0.007 per vCPU per hour + $0.0008 per GB memory per hour
- **Estimated**: $10-20/month for small app

---

## Option 3: AWS ECS with Fargate

**Why ECS Fargate:**
- ✅ Serverless containers (no EC2 to manage)
- ✅ Auto-scaling
- ✅ Good for production workloads

### Steps

1. **Create ECR Repository**
   ```bash
   aws ecr create-repository --repository-name fleetmanagement-api
   ```

2. **Build and Push Image**
   ```bash
   # Get login token
   aws ecr get-login-password --region REGION | docker login --username AWS --password-stdin ACCOUNT_ID.dkr.ecr.REGION.amazonaws.com
   
   # Build and tag
   docker build -t fleetmanagement-api .
   docker tag fleetmanagement-api:latest ACCOUNT_ID.dkr.ecr.REGION.amazonaws.com/fleetmanagement-api:latest
   
   # Push
   docker push ACCOUNT_ID.dkr.ecr.REGION.amazonaws.com/fleetmanagement-api:latest
   ```

3. **Create ECS Cluster and Service**
   - Use AWS Console or CloudFormation
   - Or use AWS Copilot CLI (easier):
   ```bash
   # Install Copilot
   winget install Amazon.CopilotCLI
   
   # Initialize
   copilot app init fleetmanagement
   copilot svc init --name api --svc-type "Backend Service" --dockerfile Dockerfile
   copilot svc deploy
   ```

### Cost
- **Free Tier**: None
- **Pricing**: ~$0.04 per vCPU per hour + $0.004 per GB memory per hour
- **Estimated**: $15-30/month

---

## Option 4: AWS Lambda (Serverless)

**Why Lambda:**
- ✅ Serverless (no server management)
- ✅ Auto-scaling
- ✅ Pay per request
- ✅ Good for low to moderate traffic

**Considerations:**
- ⚠️ Cold starts (1-3 seconds for first request)
- ⚠️ 30-second timeout with API Gateway
- ⚠️ More complex setup than Elastic Beanstalk
- ⚠️ Better for event-driven architectures

**See [AWS_LAMBDA_DEPLOYMENT.md](./AWS_LAMBDA_DEPLOYMENT.md) for complete Lambda deployment guide** including:
- Container-based deployment (recommended)
- Lambda Web Adapter approach
- Serverless Framework (easiest)
- AWS SAM deployment

---

## Option 5: EC2 (Full Control)

**Why EC2:**
- ✅ Full control over the environment
- ✅ Can run any .NET version
- ✅ Good for complex setups

### Steps

1. **Launch EC2 Instance**
   - Choose Amazon Linux 2023 or Ubuntu
   - Instance type: t3.micro (free tier) or t3.small
   - Configure security group (open ports 80, 443, 8080)

2. **Install .NET 8**
   ```bash
   # SSH into instance
   ssh -i your-key.pem ec2-user@your-instance-ip
   
   # Install .NET 8
   sudo dnf install -y dotnet-sdk-8.0
   ```

3. **Deploy Application**
   ```bash
   # Clone your repo
   git clone https://github.com/your-repo/your-project.git
   cd your-project/FleetManagement.Api
   
   # Publish
   dotnet publish -c Release -o /var/www/fleetmanagement
   
   # Run as service
   sudo systemd service file (see below)
   ```

4. **Create Systemd Service**
   `/etc/systemd/system/fleetmanagement.service`:
   ```ini
   [Unit]
   Description=Fleet Management API
   
   [Service]
   WorkingDirectory=/var/www/fleetmanagement
   ExecStart=/usr/bin/dotnet /var/www/fleetmanagement/FleetManagement.Api.dll
   Restart=always
   RestartSec=10
   Environment=ASPNETCORE_URLS=http://+:8080
   Environment=ASPNETCORE_ENVIRONMENT=Production
   
   [Install]
   WantedBy=multi-user.target
   ```

5. **Start Service**
   ```bash
   sudo systemctl enable fleetmanagement
   sudo systemctl start fleetmanagement
   ```

### Cost
- **Free Tier**: 750 hours/month of t2.micro (12 months)
- **After Free Tier**: ~$10-15/month for t3.micro

---

## Database Options on AWS

### Option 1: Amazon RDS (SQL Server)
- Managed SQL Server
- Easy setup
- Auto backups
- **Cost**: ~$15-50/month depending on instance

### Option 2: Amazon RDS (PostgreSQL)
- More cost-effective
- Good .NET support via Npgsql
- **Cost**: ~$15-30/month

### Option 3: Amazon Aurora Serverless
- Auto-scaling
- Pay per use
- **Cost**: Varies based on usage

---

## Recommended Setup for Your Project

### Best Option: AWS Elastic Beanstalk

**Why:**
- Easiest to set up
- Automatic .NET 8 support
- Free tier available
- Minimal configuration

**Steps:**
1. Install AWS Toolkit for Visual Studio
2. Right-click project → Publish to AWS Elastic Beanstalk
3. Follow wizard
4. Set environment variables
5. Deploy!

**Your API URL will be:**
```
http://your-env.REGION.elasticbeanstalk.com
```

---

## Integration with Vercel

After deploying to AWS, update your `vercel.json`:

```json
{
  "version": 2,
  "framework": "nextjs",
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-env.REGION.elasticbeanstalk.com/api/:path*"
    }
  ]
}
```

Or if using App Runner:
```json
{
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-service.REGION.awsapprunner.com/api/:path*"
    }
  ]
}
```

---

## AWS Setup Checklist

### Prerequisites
- [ ] AWS Account created
- [ ] AWS CLI installed (optional, for CLI deployment)
- [ ] AWS Toolkit for Visual Studio (for easiest deployment)
- [ ] IAM user with appropriate permissions

### Deployment
- [ ] Choose deployment method (Elastic Beanstalk recommended)
- [ ] Deploy .NET API
- [ ] Get API URL
- [ ] Configure environment variables
- [ ] Set up database (RDS)
- [ ] Test API endpoints

### Integration
- [ ] Update `vercel.json` with AWS API URL
- [ ] Configure CORS in `Program.cs` (already done)
- [ ] Test proxy through Vercel

---

## Cost Comparison

| Service | Free Tier | Monthly Cost (After Free Tier) | Difficulty |
|---------|-----------|-------------------------------|------------|
| **Elastic Beanstalk** | ✅ 12 months | $15-30 | ⭐ Easy |
| **App Runner** | ❌ | $10-20 | ⭐⭐ Medium |
| **ECS Fargate** | ❌ | $15-30 | ⭐⭐⭐ Hard |
| **EC2** | ✅ 12 months | $10-15 | ⭐⭐⭐ Hard |

---

## Security Best Practices

1. **Use IAM Roles** instead of access keys when possible
2. **Enable HTTPS** (Elastic Beanstalk does this automatically)
3. **Configure Security Groups** to restrict access
4. **Use Secrets Manager** for sensitive environment variables
5. **Enable CloudWatch Logs** for monitoring
6. **Set up VPC** for production (isolated network)

---

## Monitoring and Logs

### CloudWatch
- Automatic log collection
- Metrics and alarms
- Dashboard creation

### Application Insights
- Performance monitoring
- Error tracking
- Request tracing

---

## Next Steps

1. **Choose AWS Service** (Elastic Beanstalk recommended)
2. **Create AWS Account** if you don't have one
3. **Deploy API** using chosen method
4. **Get API URL** from AWS console
5. **Update vercel.json** with AWS URL
6. **Test Integration** through Vercel proxy

---

## Resources

- **AWS Elastic Beanstalk Docs**: https://docs.aws.amazon.com/elasticbeanstalk/
- **AWS .NET SDK**: https://aws.amazon.com/sdk-for-net/
- **AWS Toolkit for Visual Studio**: https://aws.amazon.com/visualstudio/
- **AWS Free Tier**: https://aws.amazon.com/free/

---

**Ready to deploy to AWS? Start with Elastic Beanstalk - it's the easiest!** 🚀


