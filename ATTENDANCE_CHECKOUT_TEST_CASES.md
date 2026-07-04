# Study Area & Attendance Integration – Checkout Test Cases

---

# Scenario 1: Checkout Button Visible

### Preconditions
* Student is checked in.
* Attendance status is **Present**.

### Steps
1. Open the Attendance screen.

### Expected Result
* A **Check Out** button is visible in the AppBar.
* Button is enabled.
* Button is hidden or disabled after checkout.

---

# Scenario 2: Manual Checkout

### Steps
1. Tap **Check Out**.
2. Confirm checkout (if confirmation dialog exists).

### Expected Result
* Student is successfully checked out.
* Checkout time is stored.
* Attendance record is updated with checkout time.
* Success message is displayed.
* Check Out button is disabled or hidden.

---

# Scenario 3: Active Study Session During Checkout

### Preconditions
* Student has an active study session.

### Steps
1. Tap **Check Out**.

### Expected Result
* Active study session ends automatically.
* Study duration is calculated.
* Study session is synced to the backend.
* Attendance checkout completes successfully.
* No active study session remains.

---

# Scenario 4: Start Study After Checkout

### Preconditions
* Student has already checked out.

### Steps
1. Open the Study Area.
2. Tap **Start Study Session**.

### Expected Result
* New study session is NOT created.
* User receives a validation message:
  * "You have already checked out for today. New study sessions are not allowed."

---

# Scenario 5: Resume Study After Checkout

### Preconditions
* Student checked out while a study session was active.

### Steps
1. Return to the Study Area.

### Expected Result
* Resume button is not available.
* Previous session remains completed.
* Student cannot continue the old session.

---

# Scenario 6: Multiple Checkout Attempts

### Steps
1. Perform checkout.
2. Tap **Check Out** again.

### Expected Result
* Second checkout is blocked.
* Checkout time is not overwritten.
* Appropriate message is displayed:
  * "You have already checked out."

---

# Scenario 7: Auto Checkout at Library Closing Time

### Preconditions
* Student is checked in.
* Student forgets to check out.

### Steps
1. Wait until library closing time.

### Expected Result
* Student is automatically checked out.
* Checkout time is recorded as the auto-checkout time.
* Attendance record is updated.
* Active study session is automatically ended.
* Study session is synced.
* Student receives an optional notification (if implemented).

---

# Scenario 8: Auto Checkout with Active Study Session

### Preconditions
* Study session is active.
* Student does not manually check out.

### Steps
1. Wait until the library closes.

### Expected Result
* Study session ends automatically.
* Final study duration is calculated correctly.
* Attendance is automatically checked out.
* Backend records both checkout time and study session end time.

---

# Scenario 9: Auto Checkout Without Active Study Session

### Preconditions
* Student is checked in.
* No study session is active.

### Steps
1. Wait until the library closes.

### Expected Result
* Attendance is automatically checked out.
* No study session is created.
* No errors occur.

---

# Scenario 10: Checkout Timestamp Validation

### Steps
1. Perform manual checkout.

### Expected Result
* Checkout time stored in the database matches the actual checkout time.
* Student App displays the correct checkout time.
* Admin Dashboard displays the same checkout time.

---

# Scenario 11: Study History After Checkout

### Preconditions
* Student completes a study session by checking out.

### Steps
1. Open Study History.

### Expected Result
* Session appears in history.
* Start time is correct.
* End time equals checkout time (if ended by checkout).
* Study duration is calculated correctly.

---

# Scenario 12: Dashboard Synchronization

### Steps
1. Student checks out.
2. Open Admin Dashboard.

### Expected Result
* Attendance reflects the checkout.
* Study session status is updated.
* No active study session is shown.
* Dashboard statistics are updated.

---

# Scenario 13: Network Failure During Checkout

### Steps
1. Disconnect the internet.
2. Tap **Check Out**.

### Expected Result
* Appropriate error message is displayed.
* Checkout is not partially saved.
* No duplicate study session is created.
* User can retry when the network is restored.

---

# Scenario 14: App Restart After Checkout

### Steps
1. Check out.
2. Close the app.
3. Reopen the app.

### Expected Result
* Checkout status persists.
* Check Out button remains disabled/hidden.
* No active study session exists.
* Student cannot start a new study session for that day.

---

# Scenario 15: End-to-End Checkout Flow

### Steps
1. Student checks in using QR.
2. Starts a study session.
3. Studies for some time.
4. Taps **Check Out**.
5. Opens Study History.
6. Opens Attendance screen.

### Expected Result
* Attendance checkout is completed successfully.
* Active study session ends automatically.
* Study history contains the completed session.
* Attendance displays the correct check-in and check-out times.
* Student cannot start another study session on the same day after checkout.
* Admin Dashboard, Student App, Attendance records, and Study History remain fully synchronized.
