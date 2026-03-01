# AI Coding Agent — Design Document  
*A Zettelkasten‑Driven Knowledge, Research, and Coding Environment*

---

## 1. Purpose and Vision

The AI Coding Agent is a knowledge‑development and coding environment built around the principles of the **Zettelkasten Methodology** described in `METHODOLOGY.md`. The system enables users to create, connect, and explore atomic notes, code snippets, research threads, and project structures in a visually intuitive interface.

The goal is to support:
- Deep thinking and idea development  
- Hyperlinked knowledge networks  
- Multi‑note spatial exploration  
- Smooth navigation across projects and research areas  
- Aesthetic clarity and visual hierarchy  
- A modern, motion‑enhanced UI with microinteractions  
- Theming options for light and dark modes  

---

## 2. Core Functional Requirements

### 2.1 Zettelkasten‑Compliant Notes
Each note must:
- Contain a **single atomic idea**  
- Be written in the user’s own words  
- Be **autonomous** (understandable without external context)  
- Support **bidirectional linking** to other notes  
- Allow **hyperlinks to external websites** that open in a browser  
- Be stored in a format that supports long‑term evolution  

### 2.2 Hyperlinking System
Notes must support:
- Internal links: `[[Note Title]]` or unique IDs  
- External links: standard `https://` URLs  
- Clickable links that open:
  - Internal notes in the app  
  - External links in the system browser  

### 2.3 Notecard‑Style Display
Notes should be displayed as **small, card‑like elements**:
- Minimalist card layout  
- Title + short content preview  
- Expandable on click  
- Draggable for spatial arrangement  
- Multiple cards visible simultaneously  
- Smooth transitions when opening, closing, or rearranging  

### 2.4 Bento Grid Layout
The main workspace uses a **bento grid**:
- Modular, resizable tiles  
- Cards can snap into grid cells  
- Supports freeform arrangement for idea clustering  
- Responsive layout for different screen sizes  

### 2.5 Navigation Panel
A left‑side navigation panel includes:
- **Projects**  
- **Research Areas**  
- **Tags / Themes**  
- **Recent Notes**  
- **Saved Views**  

The panel must be collapsible and use progressive disclosure to avoid overwhelming the user.

---

## 3. UI/UX Design Principles

### 3.1 Clarity
The interface prioritizes:
- Clean typography  
- Minimal visual noise  
- Clear separation between interactive and static elements  
- Consistent spacing and alignment  

### 3.2 Visual Hierarchy
Hierarchy is established through:
- Font size and weight  
- Card elevation and shadows  
- Color contrast  
- Motion cues (e.g., expanding cards rise above others)  

### 3.3 Intuitive Navigation
Navigation must feel natural:
- Left panel for global navigation  
- Bento grid for local exploration  
- Breadcrumbs for deep navigation  
- Back/forward navigation history  
- Search bar with fuzzy matching  

### 3.4 Motion Design
Motion is subtle and purposeful:
- Cards animate when opened or closed  
- Hover states highlight interactive elements  
- Smooth transitions when rearranging cards  
- Navigation panel slides in/out with easing  

### 3.5 Microinteractions
Small feedback loops enhance usability:
- Link hover previews  
- Card “wiggle” when dragged  
- Subtle highlight when linking notes  
- Animated confirmation when saving  

### 3.6 Progressive Disclosure
The UI reveals complexity only when needed:
- Basic note view shows title + summary  
- Full note view expands on click  
- Advanced metadata (tags, backlinks, timestamps) hidden until requested  
- Navigation panel sections expand on demand  

---

## 4. Theming and Aesthetic Options

### 4.1 Paper‑Like Light Theme
- Off‑white textured background  
- Soft shadows  
- Slightly warm color palette  
- Notes resemble index cards  
- Serif or hybrid serif/sans typography  

### 4.2 Dark Theme
- Deep charcoal background  
- High‑contrast card surfaces  
- Cool accent colors  
- Reduced shadows for comfort  
- Sans‑serif typography for clarity  

### 4.3 Theme Switching
- Toggle in settings  
- Smooth animated transition  
- User preferences stored persistently  

---

## 5. System Architecture

### 5.1 Note Storage
Notes stored as:
- Markdown files with frontmatter metadata  
- Unique IDs for linking  
- Backlink index generated automatically  

### 5.2 Rendering Engine
- Renders Markdown into notecards  
- Supports inline code, code blocks, diagrams  
- Converts internal links into clickable UI elements  

### 5.3 Linking Engine
- Maintains graph of note relationships  
- Supports backlink discovery  
- Enables graph‑view visualization (optional future feature)  

### 5.4 Browser Integration
- External links open in system browser  
- Internal links open in app view  

### 5.5 Layout Engine
- Bento grid with drag‑and‑drop  
- Snap‑to‑grid behavior  
- Multi‑card display  

---

## 6. User Workflows

### 6.1 Creating a Note
1. User clicks “New Note”  
2. Blank notecard appears  
3. User writes atomic idea  
4. User adds links to related notes  
5. Note saved automatically  

### 6.2 Linking Notes
1. User types `[[` to trigger link search  
2. Autocomplete suggests existing notes  
3. Selecting a note creates a hyperlink  
4. Backlink automatically added  

### 6.3 Exploring Ideas
- User opens multiple cards  
- Arranges them spatially  
- Follows links to related notes  
- Uses navigation panel to switch contexts  

### 6.4 Project Navigation
- Projects appear in left panel  
- Clicking a project loads its associated notes  
- Bento grid displays relevant cards  

---

## 7. Accessibility Considerations

- Keyboard navigation for all actions  
- High‑contrast mode  
- Adjustable font sizes  
- Screen reader support for note content  

---

## 8. Future Enhancements

- Graph view of note relationships  
- AI‑assisted linking suggestions  
- Version history for notes  
- Collaborative editing  
- Plugin system for custom workflows  

---

## 9. Summary

This design document outlines a Zettelkasten‑driven AI coding agent that blends knowledge development, research navigation, and coding workflows into a cohesive, visually intuitive environment. The system emphasizes clarity, visual hierarchy, intuitive navigation, motion design, microinteractions, and progressive disclosure, all wrapped in a flexible theming system.
