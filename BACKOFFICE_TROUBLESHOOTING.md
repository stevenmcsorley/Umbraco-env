# Umbraco Backoffice Loading Issues - Troubleshooting Guide

## Problem: Backoffice Shows Loading Dots for 20+ Minutes

This is usually caused by JavaScript bundle loading issues or OAuth redirect problems.

## Quick Fixes

### 1. **Check Browser Console (Most Important!)**
- Press `F12` to open Developer Tools
- Go to **Console** tab
- Look for red errors
- Common errors:
  - `Failed to load resource` (JavaScript bundles)
  - `CORS policy` errors
  - `Mixed Content` warnings (HTTP/HTTPS mismatch)
  - `NET::ERR_CERT_AUTHORITY_INVALID` (SSL certificate)

### 2. **Use Correct URL**
- **HTTPS**: `https://localhost:44372/umbraco` ✅ (Recommended)
- **HTTP**: `http://localhost:44372/umbraco` (May cause OAuth issues)

### 3. **Accept SSL Certificate**
If using HTTPS:
1. Click **"Advanced"** on the certificate warning
2. Click **"Proceed to localhost (unsafe)"**
3. This is safe for local development

### 4. **Clear Browser Cache Completely**
- Press `Ctrl+Shift+Delete` (Windows) or `Cmd+Shift+Delete` (Mac)
- Select **"All time"**
- Check **"Cached images and files"**
- Click **"Clear data"**

### 5. **Try Incognito/Private Mode**
- This bypasses all cache and extensions
- If it works in incognito, it's a cache/extension issue

### 6. **Check Network Tab**
- Press `F12` → **Network** tab
- Refresh the page
- Look for failed requests (red)
- Check if `/umbraco/backoffice/` JavaScript bundles are loading

## Common Issues

### Issue 1: JavaScript Bundles Not Loading
**Symptoms**: Loading dots forever, no errors in console

**Fix**:
1. Check Network tab - are `.js` files loading?
2. If 404 errors, Umbraco might not be fully initialized
3. Wait 2-3 minutes for first-time initialization
4. Restart container: `docker-compose restart mydockerproject`

### Issue 2: OAuth Redirect Loop
**Symptoms**: Page keeps redirecting, never loads

**Fix**:
1. Clear all cookies for `localhost`
2. Use HTTPS consistently (not mixing HTTP/HTTPS)
3. Check `appsettings.json` has correct `BackOfficePath`

### Issue 3: CORS Errors
**Symptoms**: Console shows CORS policy errors

**Fix**:
- This shouldn't happen for same-origin requests
- Check if you're accessing via different port/protocol

### Issue 4: Database Connection Issues
**Symptoms**: Errors in Docker logs about database

**Fix**:
```bash
# Check database is healthy
docker ps | grep database

# Check database logs
docker logs mydockerproject_database --tail 50
```

## Diagnostic Commands

### Check Umbraco Logs
```bash
docker logs mydockerproject-mydockerproject-1 --tail 100
```

### Check for Errors
```bash
docker logs mydockerproject-mydockerproject-1 --tail 200 | grep -i "error\|exception\|fail"
```

### Restart Umbraco Container
```bash
docker-compose restart mydockerproject
```

### Full Restart (Nuclear Option)
```bash
docker-compose down
docker-compose up -d
```

## Expected Behavior

### First Time Load
- **30-60 seconds**: Normal for first-time initialization
- Umbraco creates indexes, caches, etc.
- Loading dots are expected during this time

### Subsequent Loads
- **5-10 seconds**: Should be much faster
- If still slow, there's likely an issue

## Still Not Working?

1. **Check Docker logs** for any errors
2. **Check browser console** for JavaScript errors
3. **Try different browser** (Chrome, Firefox, Edge)
4. **Check firewall/antivirus** isn't blocking localhost
5. **Verify port 44372** isn't being used by another application

## Configuration Check

Verify `appsettings.json` has:
```json
{
  "Umbraco": {
    "CMS": {
      "Global": {
        "BackOfficePath": "/umbraco"
      }
    }
  }
}
```

## Contact Points

If none of these work, check:
- Docker container status: `docker ps`
- Umbraco logs: `docker logs mydockerproject-mydockerproject-1`
- Browser console errors (F12)

