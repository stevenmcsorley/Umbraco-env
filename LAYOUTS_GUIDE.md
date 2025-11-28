# Layout System Guide

## Available Layouts

The Razor Site Kit supports multiple layout options for flexible page design:

### 1. Main Layout (`Layouts/Main.cshtml`)
**Default layout** - Standard container with header and footer.

**Features:**
- Container-based content area
- Simple header navigation
- Footer

**Best for:** Standard pages, hotel listings, general content

### 2. Holy Grail Layout (`Layouts/HolyGrail.cshtml`)
**Three-column layout** - Left sidebar, main content, right sidebar.

**Features:**
- Left sidebar (hidden on mobile, visible on lg+)
- Main content area
- Right sidebar (hidden on mobile/tablet, visible on xl+)
- Responsive breakpoints

**Best for:** Content-rich pages, dashboards, pages needing multiple navigation areas

**Usage:**
```razor
@{
    Layout = "Layouts/HolyGrail.cshtml";
    ViewData["SidebarContent"] = "<h3>Filters</h3><p>Filter content here</p>";
    ViewData["RightSidebarContent"] = "<h3>Related</h3><p>Related content</p>";
}
```

### 3. Sidebar Layout (`Layouts/Sidebar.cshtml`)
**Two-column layout** - Sidebar + main content.

**Features:**
- Left sidebar (always visible)
- Main content area
- Good for navigation-heavy pages

**Best for:** Hotel details, pages with filters/navigation

**Usage:**
```razor
@{
    Layout = "Layouts/Sidebar.cshtml";
    ViewData["SidebarContent"] = "<h3>Navigation</h3><nav>...</nav>";
}
```

### 4. Centered Layout (`Layouts/Centered.cshtml`)
**Centered content** - Content centered with max-width.

**Features:**
- Centered content area
- Max-width constraint (4xl)
- Clean, focused design

**Best for:** Landing pages, forms, focused content

**Usage:**
```razor
@{
    Layout = "Layouts/Centered.cshtml";
}
```

### 5. Full Width Layout (`Layouts/FullWidth.cshtml`)
**Full-width content** - No container constraints.

**Features:**
- Full-width content
- No max-width
- Header and footer only

**Best for:** Hero sections, galleries, immersive experiences

**Usage:**
```razor
@{
    Layout = "Layouts/FullWidth.cshtml";
}
```

## How to Choose Layout

### Option 1: Set in View File
```razor
@{
    Layout = "Layouts/HolyGrail.cshtml";  // Choose your layout
    ViewData["Title"] = "Page Title";
}
```

### Option 2: Set via Controller
```csharp
public IActionResult HotelDetails(string id, string layout = "Main")
{
    ViewData["Layout"] = $"Layouts/{layout}.cshtml";
    return View("hotel");
}
```

Then access via URL: `/hotels/123?layout=HolyGrail`

### Option 3: Set via ViewBag/ViewData
```csharp
ViewData["Layout"] = "Layouts/Sidebar.cshtml";
```

## Layout Customization

Each layout supports custom content via ViewData:

- `ViewData["SiteName"]` - Site name in header/footer
- `ViewData["SidebarContent"]` - Left sidebar HTML (for HolyGrail/Sidebar layouts)
- `ViewData["RightSidebarContent"]` - Right sidebar HTML (for HolyGrail layout)

## Examples

### Hotel List with Holy Grail Layout
```razor
@{
    Layout = "Layouts/HolyGrail.cshtml";
    ViewData["SidebarContent"] = @"
        <h3 class='font-bold mb-4'>Filters</h3>
        <div class='space-y-2'>
            <label><input type='checkbox'> WiFi</label>
            <label><input type='checkbox'> Pool</label>
        </div>
    ";
    ViewData["RightSidebarContent"] = @"
        <h3 class='font-bold mb-4'>Quick Links</h3>
        <a href='/offers'>Special Offers</a>
    ";
}
```

### Room Page with Sidebar Layout
```razor
@{
    Layout = "Layouts/Sidebar.cshtml";
    ViewData["SidebarContent"] = @"
        <h3 class='font-bold mb-4'>Room Features</h3>
        <ul class='space-y-2'>
            <li>✓ WiFi</li>
            <li>✓ TV</li>
            <li>✓ Mini Bar</li>
        </ul>
    ";
}
```

## Responsive Behavior

- **Holy Grail**: Sidebars hidden on mobile, visible on larger screens
- **Sidebar**: Always visible, stacks on mobile
- **Centered**: Responsive max-width
- **Full Width**: Always full width
- **Main**: Container-based, responsive

## Summary

✅ **5 Layout Options**: Main, HolyGrail, Sidebar, Centered, FullWidth
✅ **Flexible**: Choose per page via Layout assignment
✅ **Customizable**: Sidebar content via ViewData
✅ **Responsive**: All layouts adapt to screen size

