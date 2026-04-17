# Obsidian Agent - Features

Obsidian Agent exposes 34 MCP tools that let AI models interact with an Obsidian vault through the Obsidian CLI. These tools go beyond basic file operations — they understand the knowledge graph, task management, metadata, and vault structure, enabling intelligent workflows both inside and outside the Obsidian application.

## Capabilities

### Note Management

Core CRUD operations for working with notes in the vault.

| Tool | Description |
|------|-------------|
| `ReadNote` | Read the content of a note |
| `CreateNote` | Create a new note (fails if it already exists) |
| `UpdateNote` | Overwrite an existing note's content |
| `DeleteNote` | Delete a note from the vault |
| `AppendToNote` | Append content to the end of an existing note |
| `ListNotes` | List markdown notes in the vault or a subfolder |

### Search

Find information across the vault with full-text search.

| Tool | Description |
|------|-------------|
| `SearchNotes` | Full-text search returning matching file paths |
| `SearchWithContext` | Search with surrounding line context — see matches in their original context without reading entire files |

### Knowledge Graph Navigation

Navigate the relationships between notes. These tools understand wikilinks and let the agent reason about your vault's structure rather than treating notes as isolated files.

| Tool | Description |
|------|-------------|
| `GetBacklinks` | List files that link to a given note, with link counts |
| `GetLinks` | List outgoing links from a note |
| `FindOrphans` | Find notes with no incoming links — forgotten or disconnected knowledge |
| `FindDeadEnds` | Find notes with no outgoing links — candidates for expansion |
| `FindUnresolved` | Find broken wikilinks — references to notes that don't exist yet |

**Why this matters:** Backlinks reveal context ("what references this?"), orphans surface forgotten notes, and unresolved links highlight gaps in your knowledge base. Together, these tools let the agent understand how your notes connect and suggest improvements.

### Task Management

Track and update tasks across your vault without opening Obsidian.

| Tool | Description |
|------|-------------|
| `ListTasks` | List tasks across the vault or in a specific file, with filters for done/todo status |
| `UpdateTask` | Mark a task as done, todo, or toggle its status by file path and line number |

**Why this matters:** Tasks scattered across project notes, meeting notes, and daily notes can be surfaced in one place. Combined with external services, this enables bidirectional task sync.

### Daily Notes

Interact with today's daily note — the natural hub for journaling, logging, and daily review.

| Tool | Description |
|------|-------------|
| `ReadDailyNote` | Read the content of today's daily note |
| `AppendToDailyNote` | Append content to today's daily note |
| `PrependToDailyNote` | Prepend content to today's daily note |
| `GetDailyNotePath` | Get the file path of today's daily note |

**Why this matters:** The daily note is the entry point for many workflows — standup summaries, activity logs, and end-of-day reviews. These tools let the agent add to it throughout the day without interrupting your flow.

### Properties and Metadata

Read and write frontmatter properties — the structured data layer of Obsidian.

| Tool | Description |
|------|-------------|
| `ListProperties` | List all frontmatter properties used in the vault or a file, with counts |
| `ReadProperty` | Read a specific property value from a file |
| `SetProperty` | Set a property on a file (supports text, list, number, checkbox, date, datetime) |
| `RemoveProperty` | Remove a property from a file |
| `ListTags` | List tags in the vault or a file, with occurrence counts |
| `GetTag` | Get detailed info about a tag, including which files use it |

**Why this matters:** Properties like `status`, `priority`, `due`, and `assignee` turn notes into lightweight database records. The agent can query and update these fields to power automated workflows — update a project's status when work completes, tag notes for review, or query all notes with a specific property value.

### Templates

Create notes that follow your vault's conventions using pre-defined templates.

| Tool | Description |
|------|-------------|
| `ListTemplates` | List available note templates |
| `ReadTemplate` | Read a template's content |
| `CreateFromTemplate` | Create a new note from a template |

**Why this matters:** Templates ensure consistency. When the agent creates meeting notes, project plans, or research entries, it uses your established format rather than inventing its own.

### Vault Intelligence

Understand the structure and state of your vault.

| Tool | Description |
|------|-------------|
| `GetOutline` | Get the heading structure of a note — useful for summarization and targeted content insertion |
| `ListBookmarks` | List all bookmarks in the vault |
| `AddBookmark` | Bookmark a file for quick access |
| `ListRecents` | List recently opened files — understand what's being actively worked on |
| `GetVaultInfo` | Get vault name, path, file count, and size |

### Configuration

| Tool | Description |
|------|-------------|
| `SetVault` | Get or set the active vault name (persists across sessions) |

## Integration Ideas

These tools are designed to work together and integrate with external services. Here are workflows that combine multiple capabilities:

### Morning Briefing

Start your day with an automated summary appended to your daily note.

**Tools used:** `ReadDailyNote`, `ListTasks` (todo), `ListRecents`

1. Read today's daily note to see what's already there
2. Pull all open tasks across the vault
3. Check recently opened files to see what was in progress
4. Generate a summary and append it to the daily note with `AppendToDailyNote`

*Future extension:* Combine with calendar integration to include today's meetings.

### PR-to-Note Sync

Keep project notes in sync with development activity.

**Tools used:** `SetProperty`, `AppendToNote`, `UpdateTask`, `ReadProperty`

1. When a GitHub PR merges, read the project note's current status with `ReadProperty`
2. Update the status property with `SetProperty` (e.g., "in-progress" to "completed")
3. Append a log entry with the PR link using `AppendToNote`
4. Mark related tasks as done with `UpdateTask`

### Research Capture

Turn web research into linked, tagged notes that connect to existing knowledge.

**Tools used:** `CreateFromTemplate`, `ListTags`, `GetBacklinks`, `SetProperty`

1. Create a new research note from your research template with `CreateFromTemplate`
2. Check existing tags with `ListTags` to use consistent taxonomy
3. Use `GetBacklinks` on related notes to discover connections
4. Set metadata properties (source URL, date, category) with `SetProperty`

### Vault Health Check

Periodically audit your knowledge base for quality and completeness.

**Tools used:** `FindOrphans`, `FindDeadEnds`, `FindUnresolved`, `AppendToDailyNote`

1. Find orphan notes that nothing links to — candidates for linking or archiving
2. Find dead-end notes with no outgoing links — candidates for expansion
3. Find unresolved links — references to notes that should be created
4. Summarize the findings and append them to the daily note for review

### Meeting Notes Workflow

Streamline the meeting-to-action pipeline.

**Tools used:** `CreateFromTemplate`, `SetProperty`, `AppendToDailyNote`, `ListTasks`

1. Create a meeting note from your meeting template with `CreateFromTemplate`
2. Set properties (date, attendees, project) with `SetProperty`
3. After the meeting, add a link to the daily note with `AppendToDailyNote`
4. Later, use `ListTasks` to pull out action items from across all meeting notes

### Cross-Vault Search

Find information across multiple Obsidian vaults.

**Tools used:** `SetVault`, `SearchNotes`, `SearchWithContext`

1. Use `SetVault` to switch to a different vault
2. Run `SearchNotes` or `SearchWithContext` to find relevant content
3. Switch back to your primary vault and create linked reference notes

## Architecture

For details on the project architecture, prerequisites, and configuration, see the [README](README.md).
