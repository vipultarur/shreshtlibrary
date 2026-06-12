import firebase_admin
from firebase_admin import credentials, messaging
import os
from django.conf import settings

# Initialize Firebase app only once
if not firebase_admin._apps:
    try:
        cred_path = os.path.join(settings.BASE_DIR, 'serviceAccountKey.json')
        if os.path.exists(cred_path):
            cred = credentials.Certificate(cred_path)
            firebase_admin.initialize_app(cred)
        else:
            print("WARNING: serviceAccountKey.json not found. FCM push notifications are disabled.")
    except Exception as e:
        print(f"Failed to initialize Firebase Admin: {e}")

def send_push_notification(device_token, title, body, data=None):
    if not firebase_admin._apps:
        print(f"FCM skipped for {device_token} (No Firebase Config)")
        return False
        
    try:
        message = messaging.Message(
            notification=messaging.Notification(
                title=title,
                body=body,
            ),
            data=data or {},
            token=device_token,
        )
        response = messaging.send(message)
        print('Successfully sent FCM message:', response)
        return True
    except Exception as e:
        print('Error sending FCM message:', e)
        return False
