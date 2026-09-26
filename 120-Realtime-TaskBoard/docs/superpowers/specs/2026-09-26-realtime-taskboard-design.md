# 120-Realtime-TaskBoard Design Specification

## Overview
Collaborative Kanban/Trello-style Task Board with real-time synchronization via SignalR. 
Backend: ASP.NET Core (.NET 10) with Vertical Slice Architecture. Frontend: Angular 19. Database: PostgreSQL (EF Core).

## Architecture: Vertical Slice per Feature
Each feature folder contains its own Entity, DTO, Controller, Hub, and EF Configuration — no horizontal layer splitting.

## Data Model
```
Board (1) ──< Column (N) ──< TaskItem (N)
                                    │
                            Activity (N)
```

| Entity     | Fields |
|------------|--------|
| Board      | Id (Guid), Name, Description, CreatedAt, UpdatedAt |
| Column     | Id (Guid), BoardId (FK), Name, Position (int), Color (string) |
| TaskItem   | Id (Guid), ColumnId (FK), Title, Description, Priority (enum), Assignee, Position (int), Labels (JSON), DueDate, CreatedAt, UpdatedAt |
| Activity   | Id (Guid), BoardId (FK), ActorName, ActionType (enum), EntityType (string), EntityId (Guid), OldValue, NewValue, Timestamp |

## SignalR Hubs (3 hubs)

| Hub          | Route            | Events                                        |
|--------------|------------------|-----------------------------------------------|
| BoardHub     | /hubs/board      | BoardUpdated, ColumnAdded, ColumnMoved, ColumnDeleted |
| TaskHub      | /hubs/task       | TaskCreated, TaskMoved, TaskUpdated, TaskDeleted |
| ActivityHub  | /hubs/activity   | ActivityLogged                                |

**Group Strategy**: Client joins `board-{boardId}`. Broadcast scoped to group only.

## API Endpoints

### Boards
- GET    /api/boards
- GET    /api/boards/{id}
- POST   /api/boards
- PUT    /api/boards/{id}
- DELETE /api/boards/{id}

### Columns
- GET    /api/boards/{boardId}/columns
- POST   /api/boards/{boardId}/columns
- PUT    /api/columns/{id}
- PUT    /api/columns/reorder
- DELETE /api/columns/{id}

### Tasks
- GET    /api/columns/{columnId}/tasks
- POST   /api/columns/{columnId}/tasks
- PUT    /api/tasks/{id}
- PUT    /api/tasks/{id}/move  (move between columns)
- DELETE /api/tasks/{id}

### Activities
- GET    /api/boards/{boardId}/activities?limit=50

## Port: 5155 (API), 4200 (Angular dev)

## Angular Frontend
- Board list page
- Board detail with drag-drop columns/tasks
- Activity feed sidebar
- SignalR service for real-time updates

## Tech Stack
- .NET 10, EF Core 10, Npgsql, SignalR
- Angular 19 (standalone components, signals)
- @microsoft/signalr npm package
- xUnit, WebApplicationFactory, Microsoft.AspNetCore.SignalR.Client (tests)
