# 📘 Product Requirements Document  
## Universal Content Importer, Site Kit & React Booking Engine for Umbraco  
**Version:** 1.0  
**Status:** Draft  
**Owner:** Engineering  

---

# 1. Purpose

This project delivers a **universal, reusable website-building system** for **Umbraco CMS** combined with a **React-based Booking Engine**.

The system must enable rapid deployment of new websites for any industry vertical by:

- Importing content at scale  
- Mapping data into reusable page templates  
- Using a universal design system and component library  
- Providing a flexible booking engine for availability-based products  

Industries supported include:

- Hotels  
- Event venues  
- Stadiums & arenas  
- Attractions  
- Multi-location businesses  
- Tourism boards  
- Any structured-content domain  

---

# 2. High-Level Goals

## 2.1 Rapid Site Setup  
Guided, wizard-driven content import workflow to convert CSV/JSON/XML/API data into fully structured Umbraco pages.

## 2.2 Reusable Component System  
A universal, industry-agnostic, token-driven frontend library.

## 2.3 React Booking Engine  
A flexible, standalone booking engine supporting rooms, tickets, events, passes, and time-slot based products.

## 2.4 Universal Data Importer  
A mapping system that converts raw imported data → page templates → Umbraco Block Grid content.

---

# 3. System Architecture

## 3.1 Key Components

### Umbraco Backoffice Plugin
- Import Jobs UI  
- Page Editor  
- Template Builder  
- Mapping Wizard  
- Import Runner  
- Media Manager integration  
- Dataset ingestion API  
- Locking system  
- Batch processing  

### React Site Kit
- Component library  
- Design tokens  
- Page templates  
- Booking engine integration  
- Brand theming  

### React Booking Engine
- Availability API adapters  
- Generic product model  
- Checkout flow  
- Payment integration  
- Embeddable widget  

---

# 4. Functional Requirements

---

## 4.1 Import Jobs System

A workflow for importing hundreds or thousands of pages from structured sources.

### 4.1.1 Create Import Job
User provides:

- Job name  
- Source type (CSV, JSON, XML, API)  
- File upload or API endpoint  
- Expected page templates  
- Field mapping rules  

### 4.1.2 Dataset Parsing
System automatically extracts:

- Distinct URLs  
- Page hierarchy  
- Patterns and groupings  
- Template predictions  
- Required fields  

Outputs include:

- URL  
- Parent/child inference  
- Proposed template  
- Parse status  

### 4.1.3 Template Mapping Wizard
Supports pattern-based mapping:

/hotels/* → Hotel Overview Template
/hotels//rooms/ → Room Template
/events/* → Event Template
/venues/* → Venue Template

mathematica
Copy code

### 4.1.4 Page Editor
Each data row becomes an editable page:

- Template-driven field list  
- Auto-save  
- Required/optional validation  
- Page statuses: Not Started / In Progress / Ready / Imported  

### 4.1.5 Claim & Lock System
- User claims a page before editing  
- Locked during editing  
- Auto-unlock after inactivity  

### 4.1.6 Batch Import
Creates Umbraco content nodes:

- Correct hierarchy  
- Template → Block Grid JSON  
- Media downloaded/imported  
- All content saved as **drafts**  

Import modes:

- Single page  
- Selected pages  
- All ready pages  

### 4.1.7 Import Logs
Logs:

- Created nodes  
- Media imports  
- Errors  
- Warnings per page  
- Template usage  

---

## 4.2 Page Template System

A Page Template defines a page’s components and required fields.

### 4.2.1 Template JSON Structure

```json
{
  "templateId": "hotel-page",
  "name": "Hotel Page",
  "components": [
    { "type": "hero", "fields": ["heading", "image", "tagline"] },
    { "type": "intro", "fields": ["title", "body"] },
    { "type": "gallery", "fields": ["images"] },
    { "type": "features", "fields": ["items"] },
    { "type": "faq", "fields": ["items"] }
  ]
}
4.2.2 Template Analyzer (Phase 2)
Detect blocks from an existing Umbraco page

Propose templates

Allow manual editing

Prevent destructive changes

4.3 Universal Component Library (React)
4.3.1 Core Components
Hero

Gallery

FAQ

Rich Text

Features

Offers

Rooms

Events

Testimonials

Cards

CTA Panels

Accordion

Tabs

Map

List/Grid renderer

4.3.2 Design Tokens
Tokens define:

Color palette

Typography scale

Spacing rules

Border radius

Shadow presets

Motion/animation presets

4.3.3 Page Templates
Includes prebuilt layouts:

Home

Hotel

Venue

Stadium

Event

Offer

Product

Multi-location index

Landing page

4.4 React Booking Engine
4.4.1 Universal Product Model
Supports:

Rooms

Tickets

Events

Passes

Time slot products

Bookable units

4.4.2 Availability Provider Adapters
Local JSON

Generic API

Hotel availability endpoints

Venue/ticketing APIs

Custom business-specific adapters

4.4.3 Features
Calendar

Price matrix

Product selection

Add-ons

Checkout

Payment provider hooks

Confirmation page

4.4.4 Embedding
React component

Script embed

iframe wrapper

4.5 AI Integration Hooks (Future Phases)
4.5.1 Content Suggestion API
Generate:

Headings

Intro copy

Suggested components

4.5.2 Template Classification
Predict:

Best-fit template

Required blocks

Page structure

(Not required for MVP)

5. Data Models
5.1 Import Job
id

name

status

sourceType

createdAt

updatedAt

pages[]

templates[]

5.2 PageEntry
pageId

url

templateId

status

contentJson

parentUrl

claimedBy

5.3 Template
templateId

name

components[]

version

5.4 ContentJson
Intermediary JSON before Block Grid conversion.

6. Non-Functional Requirements
6.1 Performance
Must reliably import 1,000+ pages

Local caching

Efficient media downloading

6.2 Reliability
Jobs resumable

Recovery mechanisms

Full logging

6.3 Security
Backoffice-only

Role-restricted access

No public ingestion endpoints

7. MVP Scope
7.1 Included in MVP
Import job creation

CSV ingestion

Template mapping flow

Page editor

Draft import

Basic React component library

Skeleton booking engine

7.2 Phase 2+
Template analyzer

API ingestion

Full booking engine integrations

AI assistance

8. Implementation Roadmap
Phase 1 — Core Importer
Job creation

CSV ingestion

Template mapping wizard

Page editor

Draft import

Phase 2 — Component Library
React components

Token system

Page templates

Phase 3 — Booking Engine
Base engine

Generic availability system

Checkout pipeline

Phase 4 — AI Integration
Content suggestions

Template prediction

End of Document