# Admin Test Suite — Design Spec
**Date:** 2026-06-05  
**Scope:** All admin controllers (Login, Dashboard, Products, News, Pages, Contact)

---

## Overview

Write a comprehensive test suite covering the 6 admin controllers of Mitech.Web. The suite combines **unit tests** (controller logic in isolation) and **integration tests** (full HTTP pipeline with auth). Target: xUnit, Moq, EF InMemory, Microsoft.AspNetCore.Mvc.Testing.

---

## Architecture

### Two test layers

| Layer | Purpose | Tools |
|---|---|---|
| Unit | Test each controller action in isolation | xUnit, Moq, EF InMemory |
| Integration | Test full HTTP pipeline (routing, auth, CRUD flow) | WebApplicationFactory |

### File structure

```
tests/Mitech.Tests/
├── Controllers/
│   ├── LoginControllerTests.cs
│   ├── DashboardControllerTests.cs
│   ├── ProductsAdminControllerTests.cs
│   ├── NewsAdminControllerTests.cs
│   ├── PagesAdminControllerTests.cs
│   └── ContactAdminControllerTests.cs
├── Integration/
│   ├── AdminAuthTests.cs
│   ├── AdminLoginFlowTests.cs
│   └── AdminCrudFlowTests.cs
├── Helpers/
│   └── AdminWebFactory.cs
└── Services/
    └── PageContentServiceTests.cs   (existing — keep)
```

---

## Packages to Add

Add to `tests/Mitech.Tests/Mitech.Tests.csproj`:
- `Moq` (latest stable) — mock SignInManager, IPageContentService
- `Microsoft.AspNetCore.Mvc.Testing` (9.x) — WebApplicationFactory

---

## Unit Tests

### Shared helper: `CreateInMemoryContext()`
Reuse the pattern from `PageContentServiceTests` — fresh InMemory DB per test via `Guid.NewGuid()`.

### LoginControllerTests (Moq SignInManager)
| Test method | Condition | Expected |
|---|---|---|
| `Index_Get_ReturnsView` | GET request | ViewResult |
| `Index_Post_ValidCredentials_RedirectsToAdmin` | SignIn succeeds | RedirectResult to `/admin` |
| `Index_Post_ValidCredentials_WithReturnUrl_RedirectsToReturnUrl` | SignIn succeeds + returnUrl | Redirects to returnUrl |
| `Index_Post_InvalidCredentials_ReturnsViewWithError` | SignIn fails | ViewResult, ViewData["Error"] set |
| `Logout_Post_SignsOutAndRedirectsHome` | POST logout | RedirectToActionResult("Index", "Home") |

### DashboardControllerTests (InMemory DB)
| Test method | Condition | Expected |
|---|---|---|
| `Index_ReturnsView_WithZeroCounts_WhenDbEmpty` | Empty DB | ViewData counts all 0 |
| `Index_ReturnsCorrectCounts` | Seeded DB | ViewData matches actual row counts |
| `Index_UnreadCount_OnlyCountsUnreadMessages` | Mix of read/unread messages | UnreadCount = unread only |

### ProductsAdminControllerTests (InMemory DB)
| Test method | Condition | Expected |
|---|---|---|
| `Index_ReturnsProductsSortedBySortOrder` | Products with different SortOrder | Ordered ascending |
| `Create_Get_ReturnsView_WithNewProduct` | GET | ViewResult |
| `Create_Post_ValidProduct_SavesAndRedirectsToIndex` | Valid model, no image | Product saved, redirect Index |
| `Create_Post_InvalidModel_ReturnsView` | ModelState invalid | ViewResult |
| `Edit_Get_ReturnsView_WithProduct_WhenExists` | Valid id | ViewResult with product |
| `Edit_Get_ReturnsNotFound_WhenProductMissing` | Unknown id | NotFoundResult |
| `Edit_Post_ValidProduct_UpdatesAndRedirectsToIndex` | Valid model, matching id | Product updated, redirect Index |
| `Edit_Post_IdMismatch_ReturnsBadRequest` | URL id ≠ product.Id | BadRequestResult |
| `Edit_Post_InvalidModel_ReturnsView` | ModelState invalid | ViewResult |
| `Delete_Post_RemovesProduct_AndRedirectsToIndex` | Existing product | Removed, redirect Index |
| `Delete_Post_NonExistentId_StillRedirectsToIndex` | Unknown id | Redirect Index (no error) |

### NewsAdminControllerTests (InMemory DB)
| Test method | Condition | Expected |
|---|---|---|
| `Index_ReturnsArticles_OrderedByPublishedAtDesc` | Multiple articles | Newest first |
| `Create_Get_ReturnsView_WithCategories` | DB has categories | ViewData["Categories"] set |
| `Create_Post_ValidArticle_SetsCreatedAt_AndRedirects` | Valid model | CreatedAt set to UTC now, redirect |
| `Create_Post_InvalidModel_ReturnsView_WithCategories` | Invalid model | ViewResult, categories reloaded |
| `Edit_Get_ReturnsView_WithArticle_WhenExists` | Valid id | ViewResult |
| `Edit_Get_ReturnsNotFound_WhenMissing` | Unknown id | NotFoundResult |
| `Edit_Post_ValidArticle_UpdatesAndRedirects` | Valid model | Updated, redirect |
| `Edit_Post_IdMismatch_ReturnsBadRequest` | id mismatch | BadRequestResult |
| `Edit_Post_InvalidModel_ReturnsView_WithCategories` | Invalid model | ViewResult, categories reloaded |
| `Delete_Post_RemovesArticle_AndRedirects` | Existing article | Removed, redirect |
| `Delete_Post_NonExistentId_StillRedirects` | Unknown id | Redirect |

### ContactAdminControllerTests (InMemory DB)
| Test method | Condition | Expected |
|---|---|---|
| `Index_ReturnsMessages_OrderedByReceivedAtDesc` | Multiple messages | Newest first |
| `Detail_ReturnsView_WithMessage_WhenExists` | Valid id | ViewResult |
| `Detail_ReturnsNotFound_WhenMessageMissing` | Unknown id | NotFoundResult |
| `Detail_MarksUnreadMessage_AsRead` | IsRead=false | IsRead=true after call |
| `Detail_AlreadyReadMessage_DoesNotSaveAgain` | IsRead=true | No SaveChanges called |
| `Delete_Post_RemovesMessage_AndRedirects` | Existing message | Removed, redirect |
| `Delete_Post_NonExistentId_StillRedirects` | Unknown id | Redirect |

### PagesAdminControllerTests (Moq IPageContentService)
| Test method | Condition | Expected |
|---|---|---|
| `Index_CallsGetAllAsync_AndReturnsView` | Normal | GetAllAsync called once, ViewResult |
| `Update_Post_CallsSetAsync_WithCorrectArgs_AndRedirects` | key/lang/value posted | SetAsync(key, lang, value), redirect Index |

---

## Integration Tests

### Prerequisites
`Program.cs` uses top-level statements — `Program` class is internal by default. Must add `public partial class Program { }` at the end of `Program.cs` to expose it for `WebApplicationFactory<Program>`.

### Helpers: AdminWebFactory
- Extends `WebApplicationFactory<Program>` with InMemory DB substitution
- Seeds one admin user (email: `admin@mitech.local`, password: `Admin@1234`)
- Seeds one Admin role and assigns to user
- Exposes `CreateAuthenticatedClient()` that logs in and returns HttpClient with auth cookie

### AdminAuthTests
Test every admin route without authentication — expect redirect to login.

Routes to test: Dashboard Index, Products Index/Create/Edit, News Index/Create/Edit, Pages Index, Contact Index/Detail.

Expected: `302` with `Location` header containing `/admin/login`.

### AdminLoginFlowTests
| Test | Expected |
|---|---|
| POST valid credentials | Redirect to `/admin` (302) + Set-Cookie header |
| POST invalid credentials | 200 OK, response body contains error text |
| POST logout when authenticated | Redirect to `/` (302) |

### AdminCrudFlowTests
Using authenticated client (seeded admin user):
| Test | Expected |
|---|---|
| GET Dashboard | 200 OK |
| GET Products Index | 200 OK |
| POST Create Product (valid) | 302 redirect to Index |
| GET Products Index after create | 200 OK, list grows |
| POST Delete Product | 302 redirect |
| GET News Index | 200 OK |
| POST Create News (valid) | 302 redirect |
| GET Contact Index | 200 OK |
| GET Contact Detail (marks read) | 200 OK |
| POST Delete Contact | 302 redirect |

---

## Data Flow

```
Unit test            Integration test
     │                     │
     ▼                     ▼
Controller            WebApp (InMemory)
     │                     │
     ▼                     ▼
InMemory DB / Mock    InMemory DB + Identity
     │                     │
     ▼                     ▼
Assert result         Assert HTTP response
```

---

## Error Handling

- Unit tests: verify exact result types (NotFoundResult, BadRequestResult, ViewResult, RedirectToActionResult)
- Integration tests: verify HTTP status codes (200, 302) and Location headers
- ModelState invalidity: tested by adding errors before calling the action or by sending incomplete POST bodies

---

## Out of Scope

- UI/browser rendering tests (no Playwright/Selenium)
- Email sending
- File upload (SaveImageAsync) — tested indirectly; mocking the filesystem is out of scope
- Performance or load testing
