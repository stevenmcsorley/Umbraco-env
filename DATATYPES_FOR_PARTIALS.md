# Data Types Required for Site Kit Partials

This document lists all data types needed for each partial component and layout in the Site Kit.

## Required Data Types

The following data types are required for all partials to work properly:

| Data Type Name | Editor Alias | Used By |
|---------------|--------------|---------|
| **Textstring** | `Umbraco.TextBox` | Hero (heading, tagline), Gallery (title), FAQ (title, question), Cards (title, link), Features (title), Testimonials (name, role), Events (name, location), Map (address), CTAPanel (heading, buttonText, buttonLink), Tabs (title) |
| **Textarea** | `Umbraco.TextArea` | Hero (tagline), FAQ (answer), Cards (description), Features (description), Testimonials (quote), Events (description), CTAPanel (text), RichText (content) |
| **Rich Text Editor** | `Umbraco.RichText` | RichText partial, Homepage (bodyText) |
| **Media Picker** | `Umbraco.MediaPicker3` | Hero (image), Gallery (images), Cards (image), Testimonials (avatar), Events (image) |
| **Integer** | `Umbraco.Integer` | Cards (columns), Testimonials (rating), Room (maxOccupancy) |
| **Decimal** | `Umbraco.Decimal` | Room (priceFrom), Offer (discount), Events (price), Map (latitude, longitude) |
| **DateTime** | `Umbraco.DateTime` | Offer (validFrom, validTo), Events (eventDate) |
| **URL Picker** | `Umbraco.MultiUrlPicker` | Cards (link), Events (link), CTAPanel (buttonLink), Homepage (mainButtonLink) |
| **True/False** | `Umbraco.TrueFalse` | Various optional fields |
| **Color Picker** | `Umbraco.ColorPicker` | Hero (backgroundColor, textColor), CTAPanel (backgroundColor, textColor) |

## Check Data Types Status

### Via API

```bash
# Check which data types exist
curl http://localhost:44372/api/seed/check-datatypes
```

Or in PowerShell:
```powershell
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/check-datatypes" -Method GET
```

### Response Format

```json
{
  "required": [
    {
      "name": "Textstring",
      "editor": "Umbraco.TextBox",
      "exists": true
    },
    ...
  ],
  "missing": [
    "Media Picker (Umbraco.MediaPicker3)"
  ],
  "totalRequired": 10,
  "totalExisting": 9
}
```

## Create Missing Data Types

### Option 1: Manual Creation (Recommended)

1. Go to **Settings** → **Data Types** in Umbraco backoffice
2. Click **Create**
3. For each missing data type:
   - Enter the **Name** (e.g., "Textstring")
   - Select the **Editor** from the dropdown (e.g., "Textbox")
   - Click **Save**

### Option 2: Use uSync Package

1. Install **uSync** package in Umbraco
2. Export data types from a configured environment
3. Import them into your environment

## Data Types by Partial

### Hero Partial
- Textstring (heading, tagline, backgroundColor, textColor)
- Textarea (tagline - optional)
- Media Picker (image)

### Gallery Partial
- Textstring (title)
- Media Picker (images - multiple)

### FAQ Partial
- Textstring (title, question)
- Textarea (answer)

### Cards Partial
- Textstring (title, link)
- Textarea (description)
- Media Picker (image)
- Integer (columns)

### Features Partial
- Textstring (title, icon)
- Textarea (description)

### RichText Partial
- Rich Text Editor (content)
- Textstring (className)

### Testimonials Partial
- Textstring (name, role)
- Textarea (quote)
- Media Picker (avatar)
- Integer (rating)

### Events Partial
- Textstring (name, location)
- Textarea (description)
- Media Picker (image)
- DateTime (eventDate)
- Decimal (price)
- URL Picker (link)

### Map Partial
- Textstring (address)
- Decimal (latitude, longitude)
- Textstring (height)

### CTAPanel Partial
- Textstring (heading, buttonText, buttonLink)
- Textarea (text)
- Color Picker (backgroundColor, textColor)

### Tabs Partial
- Textstring (title)
- Textarea (content per tab)

### Offers Partial (Document Type)
- Textstring (offerName)
- Textarea (description)
- Decimal (discount)
- DateTime (validFrom, validTo)

### Rooms Partial (Document Type)
- Textstring (roomName, roomType)
- Textarea (description)
- Integer (maxOccupancy)
- Decimal (priceFrom)

## Layouts

Layouts (`Main.cshtml`, `HolyGrail.cshtml`, `Sidebar.cshtml`, `Centered.cshtml`, `FullWidth.cshtml`) don't require specific data types - they are pure HTML/CSS layouts.

## Quick Setup Script

After creating data types, verify they exist:

```bash
curl http://localhost:44372/api/seed/check-datatypes
```

If all data types exist, you can then create document types:

```bash
curl -X POST http://localhost:44372/api/seed/create-document-types
```

