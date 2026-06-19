from rest_framework.permissions import BasePermission

class IsStudent(BasePermission):
    def has_permission(self, request, view):
        if not (request.user and request.user.is_authenticated and request.user.is_active and request.user.role == 'student'):
            return False
            
        profile = getattr(request.user, 'student_profile', None)
        if not profile:
            return True
            
        if profile.status == 'SUSPENDED':
            return False
            
        if profile.status == 'EXPIRED':
            from apps.library.models import AppConfig
            config = AppConfig.objects.first()
            if config:
                allowed_paths = config.expired_student_permissions.get('allowed_paths', [
                    '/api/v1/student/profile/',
                    '/api/v1/memberships/plans/',
                    '/api/v1/payments/',
                    '/api/v1/auth/',
                    '/api/v1/study/leaderboard/',
                    '/api/v1/notifications/'
                ])
                for path in allowed_paths:
                    if request.path.startswith(path):
                        return True
            return False
            
        return True


class IsLibraryAdmin(BasePermission):
    def has_permission(self, request, view):
        return request.user and request.user.is_authenticated and request.user.is_active and request.user.role in ['admin', 'super_admin']


class IsSuperAdmin(BasePermission):
    def has_permission(self, request, view):
        return request.user and request.user.is_authenticated and request.user.is_active and request.user.role == 'super_admin'

def HasAdminPermission(perm_key):
    class _HasAdminPermission(BasePermission):
        def has_permission(self, request, view):
            if not (request.user and request.user.is_authenticated and request.user.is_active and request.user.role in ['admin', 'super_admin']):
                return False
            if request.user.role == 'super_admin':
                return True
            return bool(request.user.permissions.get(perm_key, False))
    return _HasAdminPermission

