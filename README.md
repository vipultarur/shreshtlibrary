# Shresht Library - Digital Library Management System

This is the backend API service for Shresht Library. It provides full integration for student check-ins, memberships management, seat layouts, payments tracking, notifications dispatching, and analytical reporting.

## Folder Structure

The project has been refactored into modular components:

- `apps/` - Feature modules (e.g. accounts, students, attendance, memberships, payments, seats, notifications, library, study)
- `core/` - Global logging and settings
- `api/v1/` - URL routing, serializing, and views divided by component namespaces
- `utils/` - Global response standardizations, paginators, FCM interfaces, QR helpers, and exporters
- `shreshtlibrary/settings/` - Environment-based settings configurations (base, development, staging, production)

## Quick Start

1. Create a python virtual environment and install requirements:
   ```bash
   pip install -r requirements.txt
   ```

2. Run database migrations:
   ```bash
   python manage.py makemigrations
   python manage.py migrate
   ```

3. Start development server:
   ```bash
   python manage.py runserver
   ```
DJANGO_ENV=development
SECRET_KEY=django-insecure-ing&tc-&$3+-0bw!f0!n2$-vbl63#40i#@^&rbn)gaohd4(c@d

# Supabase Auth Configuration
NEXT_PUBLIC_SUPABASE_URL=https://crrfhaaqeainuqzkmged.supabase.co
NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY=sb_publishable_m0z-t34QvHCIjmiFc9n4mQ_yy6epwbt

SUPABASE_ANON_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNycmZoYWFxZWFpbnVxemttZ2VkIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODA5MzE4NTksImV4cCI6MjA5NjUwNzg1OX0.2_Sykl4JfF7T5W0Z7pK-2rueLArwvlwGk4eMce1BxwI
SUPABASE_SERVICE_ROLE_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNycmZoYWFxZWFpbnVxemttZ2VkIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc4MDkzMTg1OSwiZXhwIjoyMDk2NTA3ODU5fQ.YsKtFhHCUKdLN6HkvBg9qB5GvGlFw2RTFtd1vFPZ_sc--

# Copy your JWT Secret from: Settings -> API -> JWT Settings -> JWT Secret
SUPABASE_JWT_SECRET=your_jwt_secret_from_supabase_dashboard

# PostgreSQL Connection String (Replace [YOUR-DATABASE-PASSWORD] with your actual Supabase DB Password)
DATABASE_URL=postgresql://postgres:shreshtlibrary@db.crrfhaaqeainuqzkmged.supabase.co:5432/postgres
