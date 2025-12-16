# AWS + Supabase + Okta Deployment Checklist

## Quick Answers to Your Questions

### 1. ✅ One-to-Many Relationships: **YES, YOU HAVE THEM!**
- You have **7 one-to-many relationships** (requirement is minimum 2)
- See `ONE_TO_MANY_RELATIONSHIPS_VERIFICATION.md` for details

### 2. ✅ Supabase Configuration: **READY TO CONFIGURE**
- Guide created: `SUPABASE_CONFIGURATION_GUIDE.md`
- Need to: Add Npgsql package and update connection string

### 3. ✅ Okta Configuration: **READY TO CONFIGURE**
- Guide created: `OKTA_CONFIGURATION_GUIDE.md`
- Packages already installed, need to configure

---

## Pre-Deployment Checklist

### Phase 1: Local Configuration

#### Database (Supabase)
- [ ] Create Supabase project at [supabase.com](https://supabase.com)
- [ ] Copy connection string from Supabase dashboard
- [ ] Add `Npgsql.EntityFrameworkCore.PostgreSQL` package to `FleetManagement.Data`
- [ ] Update `appsettings.json` with Supabase connection string
- [ ] Update `Program.cs` to use `UseNpgsql()` instead of `UseSqlServer()`
- [ ] Run migrations locally: `dotnet ef database update`
- [ ] Verify tables created in Supabase dashboard

#### Okta Authentication
- [ ] Create Okta developer account at [developer.okta.com](https://developer.okta.com)
- [ ] Create Okta application (SPA type, OIDC)
- [ ] Configure redirect URIs (localhost + Vercel domain)
- [ ] Copy Client ID and Domain
- [ ] Create API token in Okta
- [ ] Update `appsettings.json` with Okta configuration
- [ ] Configure Okta middleware in `Program.cs`
- [ ] Test Okta login flow locally

#### CORS Configuration
- [ ] Update CORS in `Program.cs` to include your Vercel domain
- [ ] Test CORS with Angular client

---

### Phase 2: AWS Elastic Beanstalk Deployment

#### Initial Deployment
- [ ] Install AWS Toolkit for Visual Studio (or use AWS CLI)
- [ ] Create Elastic Beanstalk application
- [ ] Create environment (.NET 8.0 platform)
- [ ] Deploy application
- [ ] Get AWS endpoint URL

#### Environment Variables (AWS Console)
Set these in Elastic Beanstalk → Configuration → Software → Environment Properties:

**Database**:
```
ConnectionStrings__PostgreSQL=Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;
```

**Okta**:
```
Okta__Domain=https://dev-123456.okta.com
Okta__AuthorizationServerId=default
Okta__Audience=api://default
Okta__ClientId=your-client-id
Okta__ApiToken=your-api-token
```

**JWT (for Owner authentication)**:
```
Jwt__Issuer=FleetManagement.Api
Jwt__Audience=FleetManagement.Client
Jwt__SigningKey=your-secure-random-key-here-min-32-chars
Jwt__AccessTokenMinutes=30
Jwt__RefreshTokenDays=14
```

**CORS**:
```
Cors__AllowedOrigins__0=https://your-app.vercel.app
Cors__AllowedOrigins__1=http://localhost:4200
```

**Environment**:
```
ASPNETCORE_ENVIRONMENT=Production
```

#### Run Migrations on Supabase
- [ ] SSH into Elastic Beanstalk instance (or use AWS Systems Manager)
- [ ] Run: `dotnet ef database update --project FleetManagement.Data`
- [ ] Or add migration code to `Program.cs` to run automatically

---

### Phase 3: Client Configuration

#### Update Angular Environment
- [ ] Update `environment.prod.ts` with AWS API endpoint:
  ```typescript
  export const environment = {
    production: true,
    apiUrl: 'https://your-api.elasticbeanstalk.com/api',
    okta: {
      issuer: 'https://dev-123456.okta.com/oauth2/default',
      clientId: 'your-client-id',
      redirectUri: window.location.origin + '/login/callback'
    }
  };
  ```

#### Update Vercel Configuration
- [ ] Update `vercel.json` if using proxy:
  ```json
  {
    "rewrites": [
      {
        "source": "/api/:path*",
        "destination": "https://your-api.elasticbeanstalk.com/api/:path*"
      }
    ]
  }
  ```
- [ ] Or update Angular to call AWS endpoint directly

---

### Phase 4: Testing

#### Backend API Testing
- [ ] Test health endpoint: `GET https://your-api.elasticbeanstalk.com/swagger`
- [ ] Test CORS: Call API from browser console
- [ ] Test authentication endpoints (once implemented)
- [ ] Test database connection (check logs)

#### Integration Testing
- [ ] Test Owner login flow (JWT)
- [ ] Test User login flow (Okta)
- [ ] Test API calls from Angular client
- [ ] Test CORS from Vercel domain

---

## Quick Reference: Connection Strings

### Supabase Connection String Format

**Option 1: Full Connection String**
```
Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password;SSL Mode=Require;Trust Server Certificate=true;
```

**Option 2: URI Format**
```
postgresql://postgres:password@db.xxxxx.supabase.co:5432/postgres?sslmode=require
```

---

## Quick Reference: Okta Configuration

### Okta Application Settings

**Application Type**: Single-Page App (SPA)

**Redirect URIs**:
- `http://localhost:4200/login/callback`
- `https://your-app.vercel.app/login/callback`

**Sign-out URIs**:
- `http://localhost:4200`
- `https://your-app.vercel.app`

**Grant Types**:
- ✅ Authorization Code
- ✅ Refresh Token

---

## File Modifications Summary

### Files to Modify:

1. **FleetManagement.Data/FleetManagement.Data.csproj**
   - Add: `Npgsql.EntityFrameworkCore.PostgreSQL` package

2. **FleetManagement.Api/appsettings.json**
   - Add: Supabase connection string
   - Update: Okta configuration

3. **FleetManagement.Api/Program.cs**
   - Change: `UseSqlServer()` → `UseNpgsql()`
   - Add: Okta authentication middleware
   - Update: CORS configuration

4. **Angular environment.prod.ts**
   - Update: API URL to AWS endpoint
   - Update: Okta configuration

---

## Deployment Order

### Recommended Sequence:

1. **Configure Supabase** (local)
   - Create project
   - Get connection string
   - Test locally

2. **Configure Okta** (local)
   - Create application
   - Get credentials
   - Test locally

3. **Deploy to AWS**
   - Deploy application
   - Set environment variables
   - Run migrations

4. **Update Client**
   - Update Angular environment
   - Test integration

5. **Final Testing**
   - End-to-end testing
   - Verify all features work

---

## Troubleshooting Quick Reference

### Database Connection Issues
- **Check**: Supabase project is active (not paused)
- **Check**: Connection string format is correct
- **Check**: SSL mode is set to `Require`

### Okta Authentication Issues
- **Check**: Redirect URIs match exactly (including trailing slashes)
- **Check**: Client ID matches in both client and server
- **Check**: Token expiration (refresh if needed)

### CORS Issues
- **Check**: Vercel domain is in allowed origins
- **Check**: CORS middleware is before other middleware
- **Check**: Credentials are allowed if using cookies

### AWS Deployment Issues
- **Check**: Environment variables are set correctly
- **Check**: .NET 8.0 runtime is selected
- **Check**: Health check endpoint responds

---

## Security Checklist

- [ ] Use strong, random JWT signing key (min 32 characters)
- [ ] Store sensitive values in AWS Secrets Manager (not plain env vars)
- [ ] Enable HTTPS only in production
- [ ] Restrict CORS to specific domains
- [ ] Use SSL for Supabase connection
- [ ] Rotate Okta API token regularly
- [ ] Enable Okta MFA for admin accounts

---

## Cost Estimates

### Supabase
- **Free Tier**: 500 MB database, 2 GB bandwidth/month
- **Paid**: Starts at $25/month for more resources

### Okta
- **Free Developer**: Up to 1,000 monthly active users
- **Paid**: Starts at $2/user/month

### AWS Elastic Beanstalk
- **Free Tier**: 750 hours/month t2.micro (12 months)
- **After Free Tier**: ~$15-30/month for t3.small

**Total Estimated Cost**: $0-30/month (depending on usage)

---

## Next Steps After Deployment

1. **Monitor**: Set up CloudWatch logs
2. **Backup**: Configure Supabase backups
3. **Scaling**: Configure auto-scaling in Elastic Beanstalk
4. **SSL**: Set up custom domain with SSL certificate
5. **CDN**: Consider CloudFront for static assets

---

## Support Resources

- **Supabase Docs**: https://supabase.com/docs
- **Okta Docs**: https://developer.okta.com/docs
- **AWS Elastic Beanstalk Docs**: https://docs.aws.amazon.com/elasticbeanstalk/
- **Entity Framework PostgreSQL**: https://www.npgsql.org/efcore/

---

**You're all set! Follow the guides in order and you'll have a fully deployed application.** 🚀













