# Shresht Library - QR & Attendance System Documentation

## 1. Overview
This document explains the full lifecycle of the QR Code Generation and Attendance System, including the background workers, QR scanner logic, manual attendance endpoints, and how timings and padding are handled within the ASP.NET backend.

---

## 2. QR Code Generation & Lifecycle

### Auto Generation (Background Service)
- Runs continuously via `AttendanceBackgroundService`.
- Checks if a valid, unexpired QR code already exists (e.g., a manually generated 1-month QR). If one exists, it skips auto-generation.
- Checks if today is marked as a **Holiday**. If yes, skips generation.
- Generates a new daily QR code valid for exactly 1 day.

### Manual Generation (Admin Dashboard)
- Admins can manually generate QR codes valid for 1 day, 7 days, or 30 days (`GenerateQrAsync` and `RegenerateQrAsync` in `AdminAttendanceService`).
- When a new QR is manually generated, all previously active QR codes are instantly expired.

### Expiry & Cleanup
- Both the background process and the API endpoints proactively check for and mark expired QR codes as `IsExpired = true` once their `ExpiresAt` timestamp passes.

---

## 3. Attendance Timings & Padding Logic
The system relies on three core settings for time-based validation:
1. **Opening Time**: Configured per library in the `LibraryLibraryinfos` table.
2. **Closing Time**: Configured per library in the `LibraryLibraryinfos` table.
3. **Attendance Padding Time (Minutes)**: Configured globally in the `CoreGlobalsettings` table (key: `ATTENDANCE_PADDING_MINUTES`).

### The Valid Attendance Window
The allowed time frame for a student to scan a QR code is strictly defined as:
**`Opening Time` to `Closing Time + Padding Time`**

*(Note: Students cannot scan before the Opening Time. The padding is added to the Closing Time to give students extra time to mark their attendance if they are leaving right at or slightly after closing.)*

**Example:**
- **Opening Time:** 09:00 AM
- **Closing Time:** 08:00 PM (20:00)
- **Padding Time:** 60 minutes
- **Valid Scanning Window:** 09:00 AM to 09:00 PM (21:00)

---

## 4. Student QR Scanning Logic (`AttendanceService.cs`)
When a student scans an active QR code:
1. **Validation**:
   - Verifies the QR token against the database.
   - Ensures the student is active and that today is not a holiday.
   - Checks the **Attendance Window** (Current IST Time must be `>= Opening Time` and `<= Closing Time + Padding Time`).
2. **Processing**:
   - If it's the **first scan** of the day, it records the student as `IsPresent = true` and logs their `TimeIn`.
   - If a **subsequent scan** occurs (e.g., checking out), it updates the `MarkedAt` timestamp to reflect their last activity.

---

## 5. Background Attendance Processing (`AttendanceBackgroundService.cs`)
The background worker automatically manages students who do not scan the QR code.

### Step A: Midnight Reset (Daily Initialization)
- At midnight (IST), the system creates a **PENDING** attendance record (`IsPresent = false, Method = "PENDING"`) for all active students.
- This is skipped if today is a declared holiday.

### Step B: Cutoff Processing (Marking Absentees)
- The worker continuously monitors the current time.
- Once the time passes the **Cutoff Time** (`Closing Time + Padding Time`), the attendance window is officially closed for the day.
- All remaining `PENDING` records (students who never scanned) are converted to `SYSTEM` Absentees.
- A system push notification is automatically generated for each absent student informing them of their absence.

---

## 6. Admin & Manual Attendance (`AdminAttendanceService.cs`)
Admins have the ability to override or manage attendance records manually.

### Daily Summary & Absentees
- When the admin views the dashboard (`GetAttendanceDailySummaryAsync`, `GetAttendanceAbsenteesAsync`), statistics are calculated based on the same **Cutoff Time** (`Closing Time + Padding Time`).
- **Before Cutoff:** Students without a scan are grouped as `Pending`.
- **After Cutoff:** Students without a scan are officially grouped as `Absent`.

### Manual Attendance Overrides
- Admins can manually mark a student present/absent for today or the previous 2 days.
- **Late Mark Logic:** If an admin manually marks a student present *after* the `Opening Time + Padding Time`, the system automatically flags that record with `LateMark = true`. 
  - *(Example: If opening is 10:00 AM and padding is 60 mins, manually marking a student present at 11:05 AM flags them as Late, while their attendance is still recorded).*
