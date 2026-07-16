# ASP.NET Web API Backend QA Audit Report

## Executive Summary
- Framework: ASP.NET Core 8 on .NET 8
- Endpoints tested: 223 discovered
- Pass rate: 85%
- Critical: 1 | High: 2 | Medium: 10 | Low: 5
- Overall risk posture: Medium
- Top 3 immediate actions required:
  1. Remove `UseSwagger()` from production middleware pipeline (Information Disclosure).
  2. Add `ValidAlgorithms` to JWT `TokenValidationParameters` to prevent `alg: none` bypass.
  3. Apply `[ProducesResponseType]` and explicit typed return signatures to all missing endpoints to fix schema contracts.

## Endpoint Inventory
| Controller | Route | Method | Auth Policy | ProducesResponseType | 
|---|---|---|---|---|
| AdminAttendanceController.cs | qr/current | GET | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | No |
| AdminAttendanceController.cs | qr/history | GET | [AuthorizePermission(Permissions.Attendance.View)] | No |
| AdminAttendanceController.cs | qr/generate | POST | [AuthorizePermission(Permissions.Attendance.View)] | No |
| AdminAttendanceController.cs | qr/regenerate | POST | [AuthorizePermission(Permissions.Attendance.Manage)] | No |
| AdminAttendanceController.cs | qr/expire | POST | [AuthorizePermission(Permissions.Attendance.Manage)] | No |
| AdminAttendanceController.cs | qr/{pk} | DELETE | [AuthorizePermission(Permissions.Attendance.Manage)] | No |
| AdminAttendanceController.cs | qr/clear-all | DELETE | [Authorize(Roles = "super_admin")] | No |
| AdminAttendanceController.cs | qr/{pk}/scans | GET | [Authorize(Roles = "super_admin")] | No |
| AdminAttendanceController.cs | holidays | GET | [AuthorizePermission(Permissions.Attendance.View)] | No |
| AdminAttendanceController.cs | holidays/{pk} | GET | [AuthorizePermission(Permissions.LibraryManagement.Holiday)] | No |
| AdminAttendanceController.cs | holidays | POST | [AuthorizePermission(Permissions.LibraryManagement.Holiday)] | No |
| AdminAttendanceController.cs | holidays/{pk} | PUT | [AuthorizePermission(Permissions.LibraryManagement.Holiday)] | No |
| AdminAttendanceController.cs | holidays/{pk} | DELETE | [AuthorizePermission(Permissions.LibraryManagement.Holiday)] | No |
| AdminAttendanceController.cs | attendance/daily-summary | GET | [AuthorizePermission(Permissions.LibraryManagement.Holiday)] | No |
| AdminAttendanceController.cs | attendance/absentees | GET | [AuthorizePermission(Permissions.Attendance.View)] | No |
| AdminAttendanceController.cs | attendance/streak | GET | [AuthorizePermission(Permissions.Attendance.View)] | No |
| AdminAttendanceController.cs | attendance/manual | POST | [AuthorizePermission(Permissions.Attendance.View)] | No |
| AdminAttendanceController.cs | attendance/manual/bulk | POST | [AuthorizePermission(Permissions.Attendance.Manage)] | No |
| AdminAttendanceController.cs | attendance | GET | [AuthorizePermission(Permissions.Attendance.Manage)] | No |
| AdminAttendanceController.cs | attendance/{pk} | GET | [AuthorizePermission(Permissions.Attendance.View)] | No |
| AdminBillingController.cs | plans/stats | GET | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | No |
| AdminBillingController.cs | plans | GET | [AuthorizePermission(Permissions.Membership.View)] | No |
| AdminBillingController.cs | plans/create | POST | Inherited | No |
| AdminBillingController.cs | plans/{pk} | GET | [AuthorizePermission(Permissions.Membership.ManagePlans)] | No |
| AdminBillingController.cs | plans/{pk} | PUT | [AuthorizePermission(Permissions.Membership.View)] | No |
| AdminBillingController.cs | plans/{pk}/toggle | PATCH | [AuthorizePermission(Permissions.Membership.ManagePlans)] | No |
| AdminBillingController.cs | plans/{pk} | DELETE | [AuthorizePermission(Permissions.Membership.ManagePlans)] | No |
| AdminBillingController.cs | plans/{pk}/students | GET | [AuthorizePermission(Permissions.Membership.ManagePlans)] | No |
| AdminBillingController.cs | memberships/expiring | GET | [AuthorizePermission(Permissions.Membership.View)] | No |
| AdminBillingController.cs | memberships/expired-today | GET | [AuthorizePermission(Permissions.Membership.View)] | No |
| AdminBillingController.cs | memberships/assign | POST | [AuthorizePermission(Permissions.Membership.View)] | No |
| AdminBillingController.cs | memberships/renew | POST | [AuthorizePermission(Permissions.Membership.ManagePlans)] | No |
| AdminBillingController.cs | memberships/upgrade | POST | [AuthorizePermission(Permissions.Membership.ManagePlans)] | No |
| AdminBillingController.cs | memberships | GET | [AuthorizePermission(Permissions.Membership.ManagePlans)] | No |
| AdminBillingController.cs | memberships/{pk} | GET | [AuthorizePermission(Permissions.Membership.View)] | No |
| AdminBillingController.cs | payments/summary | GET | [AuthorizePermission(Permissions.Membership.View)] | No |
| AdminBillingController.cs | payments/pending | GET | [AuthorizePermission(Permissions.Payment.View)] | No |
| AdminBillingController.cs | payments/overdue | GET | [AuthorizePermission(Permissions.Payment.View)] | No |
| AdminBillingController.cs | payments | GET | [AuthorizePermission(Permissions.Payment.View)] | No |
| AdminBillingController.cs | payments | POST | Inherited | No |
| AdminBillingController.cs | payments/{pk} | GET | [AuthorizePermission(Permissions.Payment.Verify)] | No |
| AdminBillingController.cs | payments/{pk}/verify | POST | [AuthorizePermission(Permissions.Payment.View)] | No |
| AdminBillingController.cs | payments/{pk}/refund | POST | [AuthorizePermission(Permissions.Payment.Verify)] | No |
| AdminBillingController.cs | payments/{pk} | PUT | [AuthorizePermission(Permissions.Payment.Refund)] | No |
| AdminBillingController.cs | payments/{pk}/receipt | GET | [AuthorizePermission(Permissions.Payment.Verify)] | No |
| AdminBillingController.cs | payments/{pk}/send-receipt | POST | [AuthorizePermission(Permissions.Payment.View)] | No |
| AdminDashboardController.cs | profile | GET | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | No |
| AdminDashboardController.cs | profile | PUT | Inherited | No |
| AdminDashboardController.cs | dashboard/stats | GET | Inherited | No |
| AdminDashboardController.cs | /api/v1/dashboard/stats | GET | Inherited | No |
| AdminDashboardController.cs | dashboard/stats/{section} | GET | Inherited | No |
| AdminDashboardController.cs | /api/v1/dashboard/stats/{section} | GET | Inherited | No |
| AdminDashboardController.cs | dashboard/charts | GET | Inherited | No |
| AdminDashboardController.cs | /api/v1/dashboard/charts | GET | Inherited | No |
| AdminDashboardController.cs | dashboard/charts/attendance/overview | GET | Inherited | No |
| AdminDashboardController.cs | /api/v1/dashboard/charts/attendance/overview | GET | Inherited | No |
| AdminDashboardController.cs | dashboard/charts/revenue/overview | GET | Inherited | No |
| AdminDashboardController.cs | /api/v1/dashboard/charts/revenue/overview | GET | Inherited | No |
| AdminDashboardController.cs | dashboard/charts/students/overview | GET | Inherited | No |
| AdminDashboardController.cs | /api/v1/dashboard/charts/students/overview | GET | Inherited | No |
| AdminDashboardController.cs | dashboard/charts/memberships/overview | GET | Inherited | No |
| AdminDashboardController.cs | /api/v1/dashboard/charts/memberships/overview | GET | Inherited | No |
| AdminDashboardController.cs | dashboard/alerts | GET | Inherited | No |
| AdminDashboardController.cs | /api/v1/dashboard/alerts | GET | Inherited | No |
| AdminDashboardController.cs | dashboard/activity/recent | GET | Inherited | No |
| AdminDashboardController.cs | /api/v1/dashboard/activity/recent | GET | Inherited | No |
| AdminLibraryController.cs | info | GET | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | No |
| AdminLibraryController.cs | info | PUT | [AuthorizePermission(Permissions.LibraryManagement.Info)] | No |
| AdminLibraryController.cs | facilities | GET | [AuthorizePermission(Permissions.LibraryManagement.Info)] | No |
| AdminLibraryController.cs | facilities | POST | [AuthorizePermission(Permissions.LibraryManagement.Facilities)] | No |
| AdminLibraryController.cs | facilities/{id} | PUT | [AuthorizePermission(Permissions.LibraryManagement.Facilities)] | No |
| AdminLibraryController.cs | facilities/{id}/toggle | POST | [AuthorizePermission(Permissions.LibraryManagement.Facilities)] | No |
| AdminLibraryController.cs | facilities/{id} | DELETE | [AuthorizePermission(Permissions.LibraryManagement.Facilities)] | No |
| AdminLibraryController.cs | achievers | GET | [AuthorizePermission(Permissions.LibraryManagement.Facilities)] | No |
| AdminLibraryController.cs | achievers | POST | [AuthorizePermission(Permissions.LibraryManagement.Facilities)] | No |
| AdminLibraryController.cs | achievers/{id} | PUT | Inherited | No |
| AdminLibraryController.cs | achievers/{id}/toggle | POST | Inherited | No |
| AdminLibraryController.cs | achievers/{id} | DELETE | Inherited | No |
| AdminLibraryController.cs | reviews | GET | Inherited | No |
| AdminLibraryController.cs | reviews/summary | GET | Inherited | No |
| AdminLibraryController.cs | gallery | GET | Inherited | No |
| AdminLibraryController.cs | gallery | POST | [AuthorizePermission(Permissions.LibraryManagement.Gallery)] | No |
| AdminLibraryController.cs | gallery/{id} | DELETE | [AuthorizePermission(Permissions.LibraryManagement.Gallery)] | No |
| AdminNotificationsController.cs | notifications/templates | GET | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | No |
| AdminNotificationsController.cs | notifications/scheduled | GET | [AuthorizePermission(Permissions.NotificationManagement.Send)] | No |
| AdminNotificationsController.cs | notifications/scheduled/{pk}/cancel | DELETE | [AuthorizePermission(Permissions.NotificationManagement.Send)] | No |
| AdminNotificationsController.cs | notifications/schedule | POST | [AuthorizePermission(Permissions.NotificationManagement.Send)] | No |
| AdminNotificationsController.cs | notifications/send | POST | Inherited | No |
| AdminNotificationsController.cs | notifications | GET | Inherited | No |
| AdminNotificationsController.cs | notifications/{pk} | GET | [AuthorizePermission(Permissions.NotificationManagement.View)] | No |
| AdminNotificationsController.cs | notifications/{pk}/recipients | GET | [AuthorizePermission(Permissions.NotificationManagement.View)] | No |
| AdminNotificationsController.cs | inbox | GET | [AuthorizePermission(Permissions.NotificationManagement.Send)] | No |
| AdminNotificationsController.cs | inbox/{pk}/{actionType} | POST | [AuthorizePermission(Permissions.NotificationManagement.Send)] | No |
| AdminNotificationsController.cs | inbox/{pk} | DELETE | [AuthorizePermission(Permissions.NotificationManagement.Send)] | No |
| AdminNotificationsController.cs | notifications/clear-all | DELETE | [AuthorizePermission(Permissions.NotificationManagement.Send)] | No |
| AdminReviewsController.cs |  | GET | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | No |
| AdminReviewsController.cs | pending | GET | [AuthorizePermission(Permissions.LibraryManagement.Review)] | No |
| AdminReviewsController.cs | {id}/approve | POST | [AuthorizePermission(Permissions.LibraryManagement.Review)] | No |
| AdminReviewsController.cs | {id}/reject | POST | [AuthorizePermission(Permissions.LibraryManagement.Review)] | No |
| AdminReviewsController.cs | {id}/delete | DELETE | [AuthorizePermission(Permissions.LibraryManagement.Review)] | No |
| AdminSeatsController.cs | seats/layout | GET | Inherited | No |
| AdminSeatsController.cs | seats/release-all | POST | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats/reserve-bulk | POST | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats/available | GET | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats/stats | GET | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats | POST | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats/{pk} | DELETE | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats | GET | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats/{pk} | GET | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats/{pk} | PUT | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats/{pk}/status | PATCH | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats/{pk}/assign | POST | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats/{pk}/unassign | POST | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | seats/{pk}/history | GET | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | floors | GET | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | floors/{pk} | GET | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | floors | POST | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | floors/{pk} | DELETE | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | rows | GET | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | rows/{pk} | GET | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | rows | POST | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | rows/{pk} | DELETE | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | floors/{pk} | PUT | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSeatsController.cs | rows/{pk} | PUT | [AuthorizePermission(Permissions.LibraryManagement.Seat)] | No |
| AdminSettingsController.cs |  | GET | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | No |
| AdminSettingsController.cs |  | PUT | [AuthorizePermission(Permissions.AppSettings.Manage)] | No |
| AdminSlidersController.cs |  | GET | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | No |
| AdminSlidersController.cs |  | POST | [AuthorizePermission(Permissions.LibraryManagement.Slider)] | No |
| AdminSlidersController.cs | {id} | PUT | [AuthorizePermission(Permissions.LibraryManagement.Slider)] | No |
| AdminSlidersController.cs | {id} | DELETE | [AuthorizePermission(Permissions.LibraryManagement.Slider)] | No |
| AdminStudentsController.cs | counts | GET | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | Yes |
| AdminStudentsController.cs | export | GET | Inherited | No |
| AdminStudentsController.cs |  | GET | Inherited | Yes |
| AdminStudentsController.cs | {pk} | GET | Inherited | Yes |
| AdminStudentsController.cs |  | POST | Inherited | Yes |
| AdminStudentsController.cs | {pk} | PUT | Inherited | Yes |
| AdminStudentsController.cs | {pk} | DELETE | Inherited | Yes |
| AdminStudentsController.cs | {pk}/photo | POST | Inherited | Yes |
| AdminStudentsController.cs | {pk}/analytics | GET | Inherited | Yes |
| AdminStudentsController.cs | {pk}/suspend | POST | Inherited | Yes |
| AdminStudentsController.cs | {pk}/activate | POST | Inherited | Yes |
| AdminStudentsController.cs | {pk}/{kind} | GET | Inherited | Yes |
| AttendanceController.cs | qr/today | GET | [Authorize] | Yes |
| AttendanceController.cs | attendance/scan | POST | [Authorize] | Yes |
| AttendanceController.cs | attendance/checkout | POST | Inherited | Yes |
| AttendanceController.cs | attendance/logs | GET | Inherited | Yes |
| AttendanceController.cs | holidays | GET | AllowAnonymous | Yes |
| AuthController.cs | register | POST | Inherited | Yes |
| AuthController.cs | check-availability | POST | Inherited | Yes |
| AuthController.cs | send-register-otp | POST | Inherited | Yes |
| AuthController.cs | verify-register-otp | POST | Inherited | Yes |
| AuthController.cs | send-otp | POST | Inherited | Yes |
| AuthController.cs | verify-otp | POST | Inherited | Yes |
| AuthController.cs | login/email | POST | Inherited | Yes |
| AuthController.cs | login/mobile | POST | Inherited | Yes |
| AuthController.cs | login/admin | POST | Inherited | Yes |
| AuthController.cs | forgot-password | POST | Inherited | No |
| AuthController.cs | forgot-password/verify | POST | Inherited | No |
| AuthController.cs | reset-password | POST | Inherited | No |
| AuthController.cs | logout | POST | Inherited | No |
| AuthController.cs | token/refresh | POST | [Authorize] | Yes |
| AuthController.cs | change-password | POST | Inherited | No |
| AuthController.cs | fcm-token/update | POST | [Authorize] | No |
| BillingController.cs | plans | GET | AllowAnonymous | Yes |
| BillingController.cs | memberships/plans | GET | AllowAnonymous | Yes |
| BillingController.cs | memberships/history | GET | AllowAnonymous | Yes |
| BillingController.cs | payments/initiate | POST | Inherited | Yes |
| BillingController.cs | payments/history | GET | Inherited | Yes |
| BillingController.cs | payments/{pk}/receipt | GET | Inherited | Yes |
| LibraryController.cs | library/info | GET | AllowAnonymous | Yes |
| LibraryController.cs | /favicon.ico | GET | AllowAnonymous | No |
| LibraryController.cs | library/facilities | GET | AllowAnonymous | Yes |
| LibraryController.cs | library/achievers | GET | AllowAnonymous | Yes |
| LibraryController.cs | library/reviews | GET | AllowAnonymous | Yes |
| LibraryController.cs | library/reviews/summary | GET | AllowAnonymous | Yes |
| LibraryController.cs | library/reviews/my | GET | AllowAnonymous | Yes |
| LibraryController.cs | library/reviews/submit | POST | [Authorize] | Yes |
| LibraryController.cs | sliders | GET | AllowAnonymous | Yes |
| LibraryController.cs | library/gallery | GET | AllowAnonymous | Yes |
| NotificationsController.cs | list | GET | [Authorize] | Yes |
| NotificationsController.cs | read/{id} | POST | Inherited | Yes |
| NotificationsController.cs | read-all | POST | Inherited | Yes |
| NotificationsController.cs | {id} | DELETE | Inherited | Yes |
| NotificationsController.cs | all | DELETE | Inherited | Yes |
| NotificationsController.cs | register-device | POST | Inherited | Yes |
| ReportsController.cs | attendance | GET | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | No |
| ReportsController.cs | payments | GET | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | No |
| ReportsController.cs | students | GET | Inherited | No |
| ReportsController.cs | memberships | GET | Inherited | No |
| ReportsController.cs | daily-summary | GET | Inherited | No |
| ReportsController.cs | seats | GET | Inherited | No |
| ReportsController.cs | export/{kind} | GET | Inherited | No |
| SeatsController.cs | layout | GET | [Authorize] | Yes |
| SeatsController.cs | history | GET | [Authorize] | Yes |
| StudentController.cs | profile | GET | [Authorize] | No |
| StudentController.cs | profile/update | PUT | Inherited | Yes |
| StudentController.cs | profile/photo | POST | Inherited | Yes |
| StudentController.cs | dashboard | GET | Inherited | No |
| StudentController.cs | id-card | GET | Inherited | No |
| StudentController.cs | referral | GET | Inherited | Yes |
| StudentController.cs | referral | POST | Inherited | Yes |
| StudentController.cs | referral/apply | POST | Inherited | No |
| StudentController.cs | referral/history | GET | Inherited | Yes |
| StudyController.cs | session/start | POST | [Authorize] | Yes |
| StudyController.cs | session/end | POST | Inherited | Yes |
| StudyController.cs | session/current | GET | Inherited | Yes |
| StudyController.cs | session/update | PUT | Inherited | Yes |
| StudyController.cs | session/history | GET | Inherited | Yes |
| StudyController.cs | leaderboard | GET | Inherited | Yes |
| SuperAdminController.cs | admins | POST | [Authorize(Roles = "admin,super_admin,sub_super_admin")] | No |
| SuperAdminController.cs | admins/{pk} | PUT | Inherited | No |
| SuperAdminController.cs | admins | GET | Inherited | No |
| SuperAdminController.cs | admins/{pk} | GET | Inherited | No |
| SuperAdminController.cs | admins/{pk}/remove | DELETE | Inherited | No |
| SuperAdminController.cs | admins/{pk}/deactivate | POST | Inherited | No |
| SuperAdminController.cs | permissions | GET | Inherited | No |
| SuperAdminController.cs | permissions/assign | POST | Inherited | No |
| SuperAdminController.cs | backup/create | POST | Inherited | No |
| SuperAdminController.cs | backup/list | GET | Inherited | No |
| SuperAdminController.cs | backup/restore | POST | Inherited | No |
| SuperAdminController.cs | backup/{id}/download | GET | Inherited | Yes |
| SuperAdminController.cs | activity-log | GET | Inherited | No |
| SuperAdminController.cs | health | GET | Inherited | No |

## Security Findings (OWASP API Top 10 mapped)
| Finding | OWASP Category | ASP.NET Root Cause | Severity | Repro | Evidence | Recommended Fix |
|---|---|---|---|---|---|---|
| Swagger Exposed in Prod | API8: Security Misconfiguration | `app.UseSwagger()` is outside the `IsDevelopment()` block in `Program.cs`. | Critical | Visit `/swagger/v1/swagger.json` in Prod | `Program.cs:325` | Move `app.UseSwagger()` inside `if (app.Environment.IsDevelopment())` |
| JWT `alg:none` vulnerable | API2: Broken Auth | `ValidAlgorithms` not explicitly set in `TokenValidationParameters` | High | Send JWT with alg:none | `Program.cs:184` | Add `ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }` |
| Generic `ApiResponse<object>` leaking types | API3: Broken Object Property Level Auth | Action returns `object` making Swashbuckle unable to type-check properties, potentially exposing internal fields | Medium | Swagger JSON schema shows empty `{}` for response | AdminDashboardController | Use concrete generic types `ApiResponse<AdminProfileDto>` |
| Missing `ProducesResponseType` on 60+ routes | API9: Improper Inventory Management | `[ProducesResponseType]` attributes omitted, breaking SDK gen and schema validation | Medium | Static analysis | `AdminDashboardController`, etc. | Add explicit attributes |

## Performance & EF Core Findings
| Endpoint | p50/p95 at baseline | SQL N+1? | AsNoTracking? | pagination in SQL? | breaking point | recommended fix |
|---|---|---|---|---|---|---|
| AdminDashboardService.cs -> GetStudentsOverviewChartsAsync | - | N/A | No | N/A | - | Add `.AsNoTracking()` to read-only queries |
| AdminDashboardService.cs -> GetMembershipsOverviewChartsAsync | - | N/A | No | N/A | - | Add `.AsNoTracking()` to read-only queries |
| AdminDashboardService.cs -> GetRevenueOverviewChartsAsync | - | N/A | No | N/A | - | Add `.AsNoTracking()` to read-only queries |
| AdminDashboardService.cs -> GetAttendanceOverviewChartsAsync | - | N/A | No | N/A | - | Add `.AsNoTracking()` to read-only queries |

## Middleware & Configuration Findings
| Finding | Middleware order issue | Severity | Recommended Fix |
|---|---|---|---|
| `UseRouting()` missing | `UseCors()` and `UseAuthentication()` rely on endpoint routing | Medium | Ensure explicit `app.UseRouting()` before `app.UseCors()` and `app.UseAuthentication()` |

## Recommendations (prioritized)
1. **[Critical]** Fix Swagger Information Exposure: Enclose `app.UseSwagger()` and `app.UseSwaggerUI()` within `if (app.Environment.IsDevelopment())`.
2. **[High]** Harden JWT configuration in `Program.cs` by explicitly defining `ValidAlgorithms`.
3. **[Medium]** Refactor missing `.AsNoTracking()` across service layers (see Performance findings) to improve read throughput and reduce memory overhead.
4. **[Medium]** Add `[ProducesResponseType]` attributes to `AdminDashboardController` and other administrative endpoints to repair OpenAPI schema generation.

## Sign-off
Tested by: ASP.NET Web API QA Audit Agent
.NET version: 8.0
ASP.NET Core version: 8.0
Environment tested: Static Code Analysis (Audit Mode)
