# Adding Properties to Hotel Document Type Manually

Since you're already viewing the Hotel document type in Umbraco backoffice, here's how to add the properties:

## Steps

1. **In the Design Tab** (where you are now):
   - Click **"Add group"** button
   - Name it: `Content` (or `Hotel Details`)
   - Click **Save** or press Enter

2. **Add Properties to the Group**:
   - Click **"Add property"** within the group you just created
   - Add each property one by one:

### Property 1: Hotel Name
- **Name**: `Hotel Name`
- **Alias**: `hotelName` (auto-generated, but verify)
- **Editor**: Select **Textstring** (or Textbox)
- **Mandatory**: ✅ Check this box
- Click **Submit**

### Property 2: Description
- **Name**: `Description`
- **Alias**: `description`
- **Editor**: Select **Textarea**
- **Mandatory**: Leave unchecked
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

3. **Save the Document Type**:
   - Click the green **"Save"** button at the bottom right
   - The properties should now be visible

## Quick Check

After adding properties, verify they exist:
```bash
curl http://localhost:44372/api/seed/check-hotel-properties
```

## Note

The API shows these properties already exist in the database. If they don't appear in the UI, try:
1. Refreshing the page (F5)
2. Clicking "Save" on the document type
3. Checking if they're in a different tab (Structure tab)

