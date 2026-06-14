import os
import django
from django.core.files import File

# Set up Django environment
os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'shreshtlibrary.settings')
django.setup()

from apps.library.models import Facility

facilities_data = [
    {
        'name': 'શાંતિપૂર્ણ વાતાવરણ',
        'description': 'અત્યંત શાંતિપૂર્ણ વાતાવરણ',
        'icon_key': 'peace',
        'image_path': r'C:\Users\VIPUL\.gemini\antigravity\brain\c983af37-3d75-43ed-8897-45e80c5ef343\facility_peaceful_environment_1781419839444.png',
        'order': 1
    },
    {
        'name': 'ચાર્જિંગ પોઈન્ટ',
        'description': 'પર્સનલ ચાર્જિંગ પોઈન્ટની સુવિધા',
        'icon_key': 'charging',
        'image_path': r'C:\Users\VIPUL\.gemini\antigravity\brain\c983af37-3d75-43ed-8897-45e80c5ef343\facility_charging_point_1781419853927.png',
        'order': 2
    },
    {
        'name': 'પાર્કિંગ અને પાણી',
        'description': 'પાર્કિંગ વ્યવસ્થા તથા મીનરલ પાણીની સુવિધા',
        'icon_key': 'parking_water',
        'image_path': r'C:\Users\VIPUL\.gemini\antigravity\brain\c983af37-3d75-43ed-8897-45e80c5ef343\facility_parking_water_1781419867236.png',
        'order': 3
    },
    {
        'name': 'CCTV કેમેરા',
        'description': 'સી.સી.ટી.વી. કેમેરાથી સજ્જ',
        'icon_key': 'cctv',
        'image_path': r'C:\Users\VIPUL\.gemini\antigravity\brain\c983af37-3d75-43ed-8897-45e80c5ef343\facility_cctv_1781419880998.png',
        'order': 4
    },
    {
        'name': 'ડેઈલી ન્યુઝપેપર',
        'description': 'ડેઈલી ન્યુઝપેપર ઉપલબ્ધ',
        'icon_key': 'newspaper',
        'image_path': r'C:\Users\VIPUL\.gemini\antigravity\brain\c983af37-3d75-43ed-8897-45e80c5ef343\facility_newspaper_1781419894441.png',
        'order': 5
    },
    {
        'name': 'ફ્રી વાઈ-ફાઈ',
        'description': 'ફ્રી હાઈ સ્પીડ વાઈ-ફાઈ સુવિધા',
        'icon_key': 'wifi',
        'image_path': r'C:\Users\VIPUL\.gemini\antigravity\brain\c983af37-3d75-43ed-8897-45e80c5ef343\facility_wifi_1781419915381.png',
        'order': 6
    },
    {
        'name': 'આરામદાયક ફર્નિચર',
        'description': 'આરામદાયક ટેબલ ખુરશીની વ્યવસ્થા',
        'icon_key': 'furniture',
        'image_path': r'C:\Users\VIPUL\.gemini\antigravity\brain\c983af37-3d75-43ed-8897-45e80c5ef343\facility_furniture_1781419929057.png',
        'order': 7
    },
    {
        'name': 'છોકરીઓ માટે અલગ બેઠક',
        'description': 'છોકરીઓ માટે અલગ બેઠક વ્યવસ્થા',
        'icon_key': 'girls_seating',
        'image_path': r'C:\Users\VIPUL\.gemini\antigravity\brain\c983af37-3d75-43ed-8897-45e80c5ef343\facility_girls_seating_1781419941632.png',
        'order': 8
    },
    {
        'name': 'વ્યક્તિગત બેઠક',
        'description': 'વિદ્યાર્થી દીઠ બેઠકની વ્યક્તિગત વ્યવસ્થા',
        'icon_key': 'individual_seating',
        'image_path': r'C:\Users\VIPUL\.gemini\antigravity\brain\c983af37-3d75-43ed-8897-45e80c5ef343\facility_individual_seating_1781419953225.png',
        'order': 9
    },
    {
        'name': '૨૪ કલાક વાંચન',
        'description': 'લાઈબ્રેરીમાં વાંચન સમય ૨૪ કલાક',
        'icon_key': '24_hours',
        'image_path': r'C:\Users\VIPUL\.gemini\antigravity\brain\c983af37-3d75-43ed-8897-45e80c5ef343\facility_24_hours_1781419969889.png',
        'order': 10
    },
    {
        'name': 'મર્યાદિત સીટ',
        'description': 'વ્યવસ્થા જળવાઈ રહે તે માટે મર્યાદિત સીટ',
        'icon_key': 'limited_seats',
        'image_path': r'C:\Users\VIPUL\.gemini\antigravity\brain\c983af37-3d75-43ed-8897-45e80c5ef343\facility_limited_seats_1781419989305.png',
        'order': 11
    }
]

print("Adding facilities to the database...")

for data in facilities_data:
    facility, created = Facility.objects.get_or_create(
        name=data['name'],
        defaults={
            'description': data['description'],
            'icon_key': data['icon_key'],
            'order': data['order']
        }
    )
    if not created:
        facility.description = data['description']
        facility.icon_key = data['icon_key']
        facility.order = data['order']
        facility.save()
        
    if os.path.exists(data['image_path']):
        with open(data['image_path'], 'rb') as f:
            facility.image.save(os.path.basename(data['image_path']), File(f), save=True)
        print(f"Added/Updated: {data['icon_key']} with image.")
    else:
        print(f"Added/Updated: {data['icon_key']} WITHOUT image (file not found).")

print("Done!")
