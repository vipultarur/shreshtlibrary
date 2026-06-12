from rest_framework.permissions import BasePermission

class IsStudent(BasePermission):
    def has_permission(self, request, view):
        return request.user and request.user.is_authenticated and request.user.role == 'student'


class IsLibraryAdmin(BasePermission):
    def has_permission(self, request, view):
        return request.user and request.user.is_authenticated and request.user.role in ['admin', 'super_admin']


class IsSuperAdmin(BasePermission):
    def has_permission(self, request, view):
        return request.user and request.user.is_authenticated and request.user.role == 'super_admin'

def HasAdminPermission(perm_key):
    class _HasAdminPermission(BasePermission):
        def has_permission(self, request, view):
            if not (request.user and request.user.is_authenticated and request.user.role in ['admin', 'super_admin']):
                return False
            if request.user.role == 'super_admin':
                return True
            return bool(request.user.permissions.get(perm_key, False))
    return _HasAdminPermission
