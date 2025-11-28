# Adding Properties to Hotel Document Type Manually

Since programmatic property group creation is complex, here's how to add properties manually in Umbraco backoffice:

## Steps

1. **Go to Settings → Document Types → Hotel**

2. **Click the "Design" tab** (where you see "Add group")

3. **Click "Add group"**
   - Name it: `Content` or `Hotel Details`
   - Click **Save** or press Enter

4. **Within that group, click "Add property"**

5. **Add each property:**

   ### Property 1: Hotel Name
   - **Name**: `Hotel Name`
   - **Alias**: `hotelName` (should auto-generate)
   - **Editor**: Select **Textstring** (or Textbox)
   - **Mandatory**: ✅ Check this
   - Click **Submit**

   ### Property 2: Description
   - **Name**: `Description`
   - **Alias**: `description`
   - **Editor**: Select **Textarea**
   - Click **Submit**

   ### Property 3: Address
   - **Name**: `Address`
   - **Alias**: `address`
   - **Editor**: Select **Textstring**
   - Click **Submit**

   ### Property 4: City
   - **Name**: `City`
   - **Alias**: `city`
   - **Editor**: Select **Textstring**
   - Click **Submit**

   ### Property 5: Country
   - **Name**: `Country`
   - **Alias**: `country`
   - **Editor**: Select **Textstring**
   - Click **Submit**

   ### Property 6: Phone
   - **Name**: `Phone`
   - **Alias**: `phone`
   - **Editor**: Select **Textstring**
   - Click **Submit**

   ### Property 7: Email
   - **Name**: `Email`
   - **Alias**: `email`
   - **Editor**: Select **Textstring**
   - Click **Submit**

6. **Click "Save" at the bottom right** (green button)

7. **Refresh the page** (F5)

## After Adding Properties

The properties should now appear:
- In the **Design** tab (organized in the group you created)
- In the **Structure** tab (as a list)
- When editing **Grand Hotel Example** content (you'll see the fields)

## Verify

After adding properties, check:
```bash
curl http://localhost:44372/api/seed/check-hotel-properties
```

This should show all properties exist.

