# Vercel Framework Preset Settings

## ✅ "Other" is Correct!

When using Vercel as a **proxy-only** setup (not running any code on Vercel), **"Other" is the correct framework preset**.

## Why "Other"?

- You're not building a frontend framework (Next.js, React, etc.)
- You're not running serverless functions
- You're only using Vercel's **rewrites** feature to proxy requests
- No build process is needed

## Vercel Dashboard Settings

When deploying, configure these settings in the Vercel dashboard:

### Framework Preset
- **Select**: `Other` ✅ (This is correct!)

### Build Settings
- **Build Command**: Leave empty or set to `echo "No build needed"`
- **Output Directory**: Leave empty or set to `.`
- **Install Command**: Leave empty

### Root Directory
- Leave as default (usually root of your repo)

## Alternative: Manual Configuration

If you want to be explicit, you can also set these in `vercel.json`:

```json
{
  "version": 2,
  "buildCommand": null,
  "outputDirectory": ".",
  "installCommand": null,
  "framework": null,
  "rewrites": [...]
}
```

However, **this is optional** - Vercel will work fine with just the `rewrites` configuration.

## What Vercel Does

With "Other" preset and proxy configuration:

1. ✅ **No Build**: Vercel skips the build process
2. ✅ **No Install**: No npm/yarn install needed
3. ✅ **Proxy Only**: Only rewrites/redirects are active
4. ✅ **Fast Deploy**: Deployments are instant (no build time)

## Verification

After deployment, check:

1. **Deployment Logs**: Should show "No build command" or similar
2. **Build Time**: Should be < 1 second
3. **Function Count**: Should be 0 (no serverless functions)
4. **Routes**: Check that rewrites are active in deployment settings

## Common Questions

### Q: Should I change it to something else?
**A**: No, "Other" is correct for proxy-only setups.

### Q: Will it work with "Other"?
**A**: Yes! Vercel's rewrites work regardless of framework preset.

### Q: Do I need to add build settings?
**A**: No, but you can explicitly set them to `null` or empty strings if you want.

### Q: Can I use a different preset?
**A**: You could use "Next.js" if you plan to add a Next.js frontend later, but "Other" is fine for proxy-only.

## Summary

✅ **Keep "Other"** - It's the right choice for your proxy setup!
✅ **No build needed** - Vercel will just use the rewrites
✅ **Fast deployments** - No build time means instant deploys

Your current `vercel.json` configuration is perfect for this use case!


















