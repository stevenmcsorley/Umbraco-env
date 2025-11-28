# Update Home Document Type

To make the homepage content editable in Umbraco, add these properties to the **Home** document type:

## Properties to Add

1. Go to **Settings** → **Document Types** → **Home**
2. Go to **Properties** tab
3. Add these properties:

### Hero Section
- `heroHeading` (Textstring) - Default: "Welcome"
- `heroTagline` (Textarea) - Default: "Your journey starts here"
- `heroImage` (Media Picker) - Optional hero image

### Main Content Section
- `mainHeading` (Textstring) - Default: "Discover Our Hotels"
- `mainDescription` (Textarea) - Default: "Experience luxury accommodations around the world"
- `mainButtonText` (Textstring) - Default: "View All Hotels"
- `mainButtonLink` (Textstring) - Default: "/hotels"

### Features Section
- `featuresTitle` (Textstring) - Default: "Why Choose Us"
- `features` (Block List) - For managing feature items
  - OR use simple textarea with JSON
  - OR create a separate "Feature" document type

4. Click **Save**

## After Adding Properties

The `home.cshtml` view will automatically use these properties instead of hardcoded text.

