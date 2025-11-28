# Finding the Hotel Document Type in Umbraco Backoffice

## Where to Look

### Option 1: Settings → Document Types
1. Go to **Settings** (gear icon) in left sidebar
2. Click **Document Types**
3. Look for **"Hotel"** in the list
4. If you see it, click on it to view/edit

### Option 2: Content Section
1. Go to **Content** in left sidebar
2. Look for content nodes of type "Hotel"
3. If none exist, you won't see hotels here yet

## If Hotel Document Type Doesn't Appear

### Check via API
```bash
curl http://localhost:44372/api/seed/check-hotel-properties
```

This will tell you if the document type exists in the database.

### Possible Issues

1. **Document Type Exists But Not Visible**
   - Check if it's in a folder/group
   - Check user permissions
   - Try refreshing the backoffice (Ctrl+F5)

2. **Document Type Doesn't Exist**
   - The API check will confirm this
   - You may need to create it manually or via API

3. **Looking in Wrong Place**
   - Document Types are in **Settings** → **Document Types**
   - Not in Content section

## Create Hotel Document Type (If Missing)

If the document type doesn't exist, you can:

1. **Create Manually**:
   - Settings → Document Types → Create → Document Type
   - Name: `Hotel`
   - Alias: `hotel`
   - Add properties (see below)

2. **Or Use API** (after creating Decimal data type):
   ```bash
   curl -X POST http://localhost:44372/api/seed/create-document-types
   ```

## Required Properties for Hotel

- `hotelName` (Textstring) - Mandatory
- `description` (Textarea)
- `address` (Textstring)
- `city` (Textstring)
- `country` (Textstring)
- `phone` (Textstring)
- `email` (Textstring)

## Verify Document Type Exists

Run this to check:
```bash
curl http://localhost:44372/api/seed/check-hotel-properties
```

If `"exists": true`, the document type exists in the database but might not be visible in the UI for some reason.

