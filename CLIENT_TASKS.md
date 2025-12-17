# Client-Side (Angular) Tasks

## Status Overview

**Current Status**: ✅ **Grade A Achieved** - Client is production-ready

**What's Working**:
- ✅ SPA with routing
- ✅ Reactive forms with validation
- ✅ Authentication UI (Owner JWT, User Okta)
- ✅ Route guards
- ✅ State management
- ✅ Error handling
- ✅ Deployed to Vercel

---

## Required Tasks (Critical)

### 1. Update API Endpoint Configuration 🔴 CRITICAL

**Priority**: HIGH  
**Estimated Time**: 15-30 minutes

**Task**: Update Angular environment files to point to AWS Elastic Beanstalk endpoint

**Files to Modify**:
- `src/environments/environment.prod.ts`
- `src/environments/environment.ts` (if needed)

**Current State**:
```typescript
// Likely has localhost or placeholder
apiUrl: 'http://localhost:5224/api'
// or
apiUrl: '/api'  // Vercel proxy
```

**Update To**:
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://your-api.elasticbeanstalk.com/api',
  // Or if using Vercel proxy:
  // apiUrl: '/api'  // Keep this if using Vercel proxy
};
```

**Steps**:
1. [ ] Get AWS Elastic Beanstalk endpoint URL after deployment
2. [ ] Update `environment.prod.ts` with AWS endpoint
3. [ ] Test API calls work with new endpoint
4. [ ] Update `vercel.json` if using proxy (see Server Tasks)

---

### 2. Update Okta Configuration 🔴 CRITICAL

**Priority**: HIGH  
**Estimated Time**: 15-30 minutes

**Task**: Configure Okta settings to match backend configuration

**Files to Modify**:
- `src/environments/environment.prod.ts`
- `src/app/core/services/okta-auth.facade.ts` (if needed)

**Current State**: May have placeholder values

**Update To**:
```typescript
export const environment = {
  production: true,
  okta: {
    issuer: 'https://dev-123456.okta.com/oauth2/default',
    clientId: 'your-okta-client-id',
    redirectUri: window.location.origin + '/login/callback',
    scopes: ['openid', 'profile', 'email'],
    pkce: true
  }
};
```

**Steps**:
1. [ ] Get Okta domain and Client ID from Okta dashboard
2. [ ] Update `environment.prod.ts` with Okta configuration
3. [ ] Verify redirect URI matches Okta application settings
4. [ ] Test Okta login flow

---

### 3. Update CORS Configuration (if needed)

**Priority**: MEDIUM  
**Estimated Time**: 10 minutes

**Task**: Ensure client can call AWS API (CORS handled on server, but verify)

**Steps**:
1. [ ] Test API calls from browser console
2. [ ] Verify no CORS errors
3. [ ] If CORS errors occur, coordinate with server team to add Vercel domain

---

## Optional Enhancement Tasks

### 4. Improve Unit Test Coverage ⚠️ OPTIONAL

**Priority**: LOW (Already Grade A, but can strengthen)  
**Estimated Time**: 8-12 hours

**Current Status**: Only 3 test files exist

**Tasks**:
- [ ] Create tests for guards (`owner.guard.spec.ts`, `user.guard.spec.ts`)
- [ ] Create tests for interceptors (`auth.interceptor.spec.ts`, `http-error.interceptor.spec.ts`)
- [ ] Create tests for services (`vehicle.service.spec.ts`, `telemetry.service.spec.ts`, `city.service.spec.ts`)
- [ ] Create tests for components (especially forms)
- [ ] Create tests for validators (`vin.validator.spec.ts`)
- [ ] Create tests for state services

**Target**: 70%+ code coverage

**Files to Create**:
```
src/app/core/guards/
├── owner.guard.spec.ts
└── user.guard.spec.ts

src/app/core/interceptors/
├── auth.interceptor.spec.ts
└── http-error.interceptor.spec.ts

src/app/core/services/
├── vehicle.service.spec.ts
├── telemetry.service.spec.ts
├── city.service.spec.ts
└── toast.service.spec.ts

src/app/core/validators/
└── vin.validator.spec.ts
```

---

### 5. Implement SignalR Client (Replace Polling) ⚠️ OPTIONAL

**Priority**: LOW (Enhancement, not required)  
**Estimated Time**: 3-4 hours

**Current Status**: Uses HTTP polling for telemetry updates

**Task**: Replace polling with SignalR for real-time updates

**Steps**:
1. [ ] Install SignalR client: `npm install @microsoft/signalr`
2. [ ] Create `signalr.service.ts` to manage connection
3. [ ] Update `telemetry.service.ts` to use SignalR instead of polling
4. [ ] Update components to listen to SignalR events
5. [ ] Test real-time updates

**Files to Create**:
```
src/app/core/services/
└── signalr.service.ts
```

**Files to Modify**:
```
src/app/core/services/
└── telemetry.service.ts  // Remove polling, add SignalR
```

---

### 6. Update Vercel Configuration (if using proxy)

**Priority**: MEDIUM  
**Estimated Time**: 10 minutes

**Task**: Update `vercel.json` to proxy to AWS endpoint

**File**: `vercel.json`

**Update To**:
```json
{
  "version": 2,
  "framework": "angular",
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-api.elasticbeanstalk.com/api/:path*"
    }
  ]
}
```

**Steps**:
1. [ ] Get AWS endpoint URL
2. [ ] Update `vercel.json` with AWS endpoint
3. [ ] Redeploy to Vercel
4. [ ] Test proxy works

---

## Testing Tasks

### 7. Integration Testing

**Priority**: MEDIUM  
**Estimated Time**: 2-3 hours

**Tasks**:
- [ ] Test Owner login flow (JWT)
- [ ] Test Owner registration flow
- [ ] Test User login flow (Okta)
- [ ] Test Fleet CRUD operations
- [ ] Test Vehicle CRUD operations
- [ ] Test Telemetry viewing
- [ ] Test error handling
- [ ] Test token refresh

---

## Deployment Tasks

### 8. Production Deployment Verification

**Priority**: HIGH  
**Estimated Time**: 30 minutes

**Tasks**:
- [ ] Verify environment variables are set in Vercel
- [ ] Test production build: `ng build --configuration production`
- [ ] Deploy to Vercel
- [ ] Verify API calls work from production URL
- [ ] Test authentication flows in production
- [ ] Verify Okta redirects work in production

---

## Summary Checklist

### Critical (Must Do):
- [ ] Update API endpoint to AWS
- [ ] Update Okta configuration
- [ ] Test integration with backend

### Important (Should Do):
- [ ] Update Vercel proxy configuration
- [ ] Integration testing
- [ ] Production deployment verification

### Optional (Nice to Have):
- [ ] Improve unit test coverage
- [ ] Implement SignalR client
- [ ] Additional testing

---

## Estimated Time Breakdown

| Task | Priority | Time |
|------|----------|------|
| Update API endpoint | 🔴 Critical | 15-30 min |
| Update Okta config | 🔴 Critical | 15-30 min |
| Update Vercel config | 🟡 Important | 10 min |
| Integration testing | 🟡 Important | 2-3 hours |
| Production verification | 🟡 Important | 30 min |
| Unit test coverage | 🟢 Optional | 8-12 hours |
| SignalR client | 🟢 Optional | 3-4 hours |

**Total Critical Tasks**: ~1-2 hours  
**Total Important Tasks**: ~3-4 hours  
**Total Optional Tasks**: ~11-16 hours

---

## Dependencies

**Client tasks depend on**:
- ✅ Server must be deployed to AWS first (to get endpoint URL)
- ✅ Okta application must be created (to get Client ID)
- ✅ Server must have CORS configured for Vercel domain

**Client can work independently on**:
- ✅ Unit test improvements
- ✅ SignalR client (once server implements SignalR hub)

---

## Notes

1. **Most client work is already done** - The Angular app is Grade A and production-ready
2. **Main tasks are configuration** - Updating endpoints and Okta settings
3. **Optional enhancements** - Can be done after initial deployment
4. **Testing is important** - Ensure everything works with the new backend

---

**Next Steps**: Wait for server deployment, then update configuration files with AWS endpoint and Okta credentials.















