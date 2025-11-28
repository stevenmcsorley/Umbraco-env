# Creating Templates in Umbraco Backoffice

To enable creating pages in the Umbraco backoffice, you need to create **Templates** and assign them to your **Document Types**.

## Step 1: Create Templates

1. Go to **Settings** → **Templates**
2. Create the following templates:

### Template: "Hotel List"
- **Name**: `Hotel List`
- **Alias**: `hotelList`
- **Content**: 
```razor
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
@{
    Layout = "Layouts/Main.cshtml";
    ViewData["Title"] = "Hotels";
}
@Html.Partial("hotelList")
```

### Template: "Hotel"
- **Name**: `Hotel`
- **Alias**: `hotel`
- **Content**:
```razor
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
@{
    Layout = "Layouts/Main.cshtml";
    ViewData["Title"] = Model.Name;
}
@Html.Partial("hotel", Model)
```

### Template: "Room"
- **Name**: `Room`
- **Alias**: `room`
- **Content**:
```razor
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
@{
    Layout = "Layouts/Main.cshtml";
    ViewData["Title"] = Model.Name;
}
@Html.Partial("room", Model)
```

## Step 2: Assign Templates to Document Types

### For "Hotel" Document Type:
1. Go to **Settings** → **Document Types** → **Hotel**
2. Go to **Templates** tab
3. Check ✅ **"Hotel List"** (for listing page)
4. Check ✅ **"Hotel"** (for detail page)
5. Set **"Hotel"** as default template
6. Click **Save**

### For "Room" Document Type:
1. Go to **Settings** → **Document Types** → **Room**
2. Go to **Templates** tab
3. Check ✅ **"Room"**
4. Set **"Room"** as default template
5. Click **Save**

## Step 3: Create Content in Backoffice

Now you can create pages:

1. Go to **Content** section
2. Right-click **Content** → **Create** → **Hotel**
3. Fill in hotel details
4. Click **Save and Publish**
5. Create **Room** nodes as children of the Hotel

## Step 4: Access Pages

Pages will be accessible via:
- **Hotel List**: `/hotels` (custom route)
- **Hotel Details**: `/hotels/{id}` (custom route) OR Umbraco URL
- **Room**: `/hotels/{hotelId}/rooms/{roomId}` (custom route) OR Umbraco URL

## Note on Custom Routes

The custom routes (`/hotels`, `/hotels/{id}`) work alongside Umbraco's document routing. Content created in the backoffice will be accessible via both:
- Custom routes (if IDs match)
- Umbraco's native URLs (based on content node URLs)

