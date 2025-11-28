# Create Decimal Data Type in Umbraco

## Quick Steps

1. **Go to Umbraco Backoffice**: `https://localhost:44372/umbraco`

2. **Navigate to Data Types**:
   - Click **Settings** (gear icon) in the left sidebar
   - Click **Data Types**

3. **Create New Data Type**:
   - Click the **"..."** menu (three dots) next to "Data Types"
   - Click **Create**
   - Or click **"Create"** button at the top

4. **Configure Data Type**:
   - **Name**: `Decimal`
   - **Editor**: Select **"Decimal"** from the dropdown
   - Click **Save**

5. **Verify**:
   ```bash
   curl http://localhost:44372/api/seed/check-datatypes
   ```
   Should show `"exists": true` for Decimal

6. **Create Document Types**:
   ```bash
   curl -X POST http://localhost:44372/api/seed/create-document-types
   ```

## Alternative: Create via API (if possible)

Unfortunately, Umbraco 16 doesn't allow easy programmatic creation of data types. You need to create this one manually in the backoffice.

## After Creating Decimal Data Type

Once the Decimal data type exists, you can create all document types:

```bash
curl -X POST http://localhost:44372/api/seed/create-document-types
```

This will create:
- **Hotel** document type
- **Room** document type  
- **Offer** document type

