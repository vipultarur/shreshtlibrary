import requests
import json

BASE_URL = "http://127.0.0.1:8000"

print("Starting API Endpoints Verification...")

tokens = {}

# Helper to format responses
def log_result(endpoint, method, status_code, response):
    print(f"[{method}] {endpoint} -> Status: {status_code}")
    try:
        data = response.json()
        print(f"  Response: {json.dumps(data)[:200]}...")
    except Exception:
        print(f"  Response (Text): {response.text[:200]}...")

# ----------------- 1. LOGIN & AUTHENTICATION -----------------
# Student Login
login_url = f"{BASE_URL}/api/v1/auth/login/email/"
payload = {"email": "student1@gmail.com", "password": "studentpassword123"}
response = requests.post(login_url, json=payload)
log_result("/api/v1/auth/login/email/", "POST", response.status_code, response)
if response.status_code == 200:
    tokens['student'] = response.json()['data']['tokens']['access']

# Admin Login
admin_login_url = f"{BASE_URL}/api/v1/auth/admin/login/"
payload = {"username": "admin1", "password": "adminpassword123"}
response = requests.post(admin_login_url, json=payload)
log_result("/api/v1/auth/admin/login/ (Admin)", "POST", response.status_code, response)
if response.status_code == 200:
    tokens['admin'] = response.json()['data']['tokens']['access']

# Super Admin Login
response = requests.post(admin_login_url, json={"username": "superadmin", "password": "superpassword123"})
log_result("/api/v1/auth/admin/login/ (Super Admin)", "POST", response.status_code, response)
if response.status_code == 200:
    tokens['superadmin'] = response.json()['data']['tokens']['access']

# ----------------- 2. STUDENT SECURED ENDPOINTS -----------------
if 'student' in tokens:
    headers = {"Authorization": f"Bearer {tokens['student']}"}

    # Student Dashboard
    res = requests.get(f"{BASE_URL}/api/v1/student/dashboard/", headers=headers)
    log_result("/api/v1/student/dashboard/", "GET", res.status_code, res)

    # Student Profile
    res = requests.get(f"{BASE_URL}/api/v1/student/profile/", headers=headers)
    log_result("/api/v1/student/profile/", "GET", res.status_code, res)

    # Student Attendance Log
    res = requests.get(f"{BASE_URL}/api/v1/attendance/logs/", headers=headers)
    log_result("/api/v1/attendance/logs/", "GET", res.status_code, res)

    # Student Membership Details / History
    res = requests.get(f"{BASE_URL}/api/v1/memberships/history/", headers=headers)
    log_result("/api/v1/memberships/history/", "GET", res.status_code, res)

    # Student Payments History
    res = requests.get(f"{BASE_URL}/api/v1/payments/history/", headers=headers)
    log_result("/api/v1/payments/history/", "GET", res.status_code, res)

    # Student List Seats Layout
    res = requests.get(f"{BASE_URL}/api/v1/seats/layout/", headers=headers)
    log_result("/api/v1/seats/layout/", "GET", res.status_code, res)

    # Student Seat Assignment History
    res = requests.get(f"{BASE_URL}/api/v1/seats/history/", headers=headers)
    log_result("/api/v1/seats/history/", "GET", res.status_code, res)

    # Student Study Goal
    res = requests.get(f"{BASE_URL}/api/v1/study/goal/", headers=headers)
    log_result("/api/v1/study/goal/", "GET", res.status_code, res)

    # Start Study Session
    res = requests.post(f"{BASE_URL}/api/v1/study/session/start/", headers=headers)
    log_result("/api/v1/study/session/start/", "POST", res.status_code, res)

    # End Study Session
    res = requests.post(f"{BASE_URL}/api/v1/study/session/end/", headers=headers)
    log_result("/api/v1/study/session/end/", "POST", res.status_code, res)

# ----------------- 3. ADMIN SECURED ENDPOINTS -----------------
if 'admin' in tokens:
    headers = {"Authorization": f"Bearer {tokens['admin']}"}

    # Admin Dashboard Stats
    res = requests.get(f"{BASE_URL}/api/v1/admin/dashboard/stats/", headers=headers)
    log_result("/api/v1/admin/dashboard/stats/", "GET", res.status_code, res)

    # Admin QRCode Generate
    res = requests.post(f"{BASE_URL}/api/v1/admin/qrcode/generate/", headers=headers)
    log_result("/api/v1/admin/qrcode/generate/", "POST", res.status_code, res)

    # Dynamically find a pending payment to verify
    pending_payment_id = 2 # default fallback
    s2_login_res = requests.post(f"{BASE_URL}/api/v1/auth/login/email/", json={"email": "student2@gmail.com", "password": "studentpassword123"})
    if s2_login_res.status_code == 200:
        s2_token = s2_login_res.json()['data']['tokens']['access']
        s2_pay_res = requests.get(f"{BASE_URL}/api/v1/payments/history/", headers={"Authorization": f"Bearer {s2_token}"})
        if s2_pay_res.status_code == 200:
            payments = s2_pay_res.json().get('data', [])
            if payments:
                pending_payment_id = payments[0]['id']

    # Admin Verify Payment
    res = requests.post(f"{BASE_URL}/api/v1/admin/payments/{pending_payment_id}/verify/", headers=headers)
    log_result(f"/api/v1/admin/payments/{pending_payment_id}/verify/", "POST", res.status_code, res)

    # Admin Manual Attendance Override
    res = requests.post(f"{BASE_URL}/api/v1/admin/attendance/manual/", json={"student_mobile": "6666666666"}, headers=headers)
    log_result("/api/v1/admin/attendance/manual/", "POST", res.status_code, res)

# ----------------- 4. SUPER ADMIN SECURED ENDPOINTS -----------------
if 'superadmin' in tokens:
    headers = {"Authorization": f"Bearer {tokens['superadmin']}"}

    # Super Admin List Admins
    res = requests.get(f"{BASE_URL}/api/v1/superadmin/admins/", headers=headers)
    log_result("/api/v1/superadmin/admins/", "GET", res.status_code, res)

print("API Endpoints Verification Completed.")
