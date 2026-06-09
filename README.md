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
