from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from django.shortcuts import get_object_or_404
from django.utils import timezone

from drf_spectacular.utils import extend_schema, OpenApiTypes

from shreshtlibrary.utils.permissions import IsStudent
from utils.response import standard_response
from apps.study.models import StudySession
from .serializers import StudySessionSerializer

class StartStudySessionView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={201: StudySessionSerializer}, tags=['Study Features'])
    def post(self, request):
        active = StudySession.objects.filter(student=request.user, end_time__isnull=True).first()
        if active:
            return standard_response(
                message="You already have an active study session.",
                data=StudySessionSerializer(active).data,
            )
        
        session = StudySession.objects.create(
            student=request.user,
            status='starting'
        )
        return standard_response(
            message="Study session started.",
            data=StudySessionSerializer(session).data,
            status_code=status.HTTP_201_CREATED
        )


class UpdateStudySessionView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(request=StudySessionSerializer, responses={200: StudySessionSerializer}, tags=['Study Features'])
    def post(self, request):
        session = StudySession.objects.filter(student=request.user, end_time__isnull=True).last()
        if not session:
            return Response({"errors": {"non_field_errors": ["No active study session found."]}}, status=status.HTTP_400_BAD_REQUEST)
        
        new_status = request.data.get('status')
        if new_status in dict(StudySession.STATUS_CHOICES):
            session.status = new_status
            if 'duration_minutes' in request.data:
                session.duration_minutes = request.data.get('duration_minutes', session.duration_minutes)
            if 'paused_minutes' in request.data:
                session.paused_minutes = request.data.get('paused_minutes', session.paused_minutes)
            session.save()
            return standard_response(message=f"Session status updated to {new_status}.", data=StudySessionSerializer(session).data)
        return Response({"errors": {"status": ["Invalid status."]}}, status=status.HTTP_400_BAD_REQUEST)


class EndStudySessionView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(request=StudySessionSerializer, responses={200: StudySessionSerializer}, tags=['Study Features'])
    def post(self, request):
        session = StudySession.objects.filter(student=request.user, end_time__isnull=True).last()
        if not session:
            return Response({"errors": {"non_field_errors": ["No active study session found to end."]}}, status=status.HTTP_400_BAD_REQUEST)
        
        session.end_time = timezone.now()
        session.status = 'completed'
        duration_minutes = request.data.get('duration_minutes', 0)
        session.duration_minutes = duration_minutes
        session.paused_minutes = request.data.get('paused_minutes', 0)
        session.save()

        # Update Attendance for today
        from apps.attendance.models import Attendance
        today = timezone.now().date()
        attendance = Attendance.objects.filter(student=request.user, date=today).first()
        if attendance:
            # Update check-out time
            attendance.time_out = session.end_time.time()
            
            # Update total hours by summing all completed study sessions today
            completed_sessions = StudySession.objects.filter(
                student=request.user, 
                start_time__date=today,
                status='completed'
            )
            total_mins = sum(s.duration_minutes for s in completed_sessions)
            
            # Calculate Late Marks: Assuming threshold > 10:00 AM
            if attendance.time_in:
                late_threshold = timezone.datetime.strptime("10:00:00", "%H:%M:%S").time()
                attendance.late_mark = attendance.time_in > late_threshold
            
            # Calculate Under Time: Assuming < 4 hours (240 minutes) is under time
            attendance.under_time = total_mins < 240
            
            # Store formatted hours
            hours = total_mins // 60
            mins = total_mins % 60
            attendance.total_hours = f"{hours}:{mins:02d}"
            
            attendance.save()

        return standard_response(
            message="Study session ended. Great effort!",
            data=StudySessionSerializer(session).data
        )

class CurrentStudySessionView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: StudySessionSerializer}, tags=['Study Features'])
    def get(self, request):
        session = StudySession.objects.filter(student=request.user, end_time__isnull=True).last()
        if session:
            return standard_response(data=StudySessionSerializer(session).data)
        return standard_response(data=None)

class StudySessionHistoryView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: StudySessionSerializer(many=True)}, tags=['Study Features'])
    def get(self, request):
        sessions = StudySession.objects.filter(student=request.user, status='completed').order_by('-start_time')
        return standard_response(data=StudySessionSerializer(sessions, many=True).data)

class LeaderboardView(APIView):
    from rest_framework.permissions import IsAuthenticated
    permission_classes = [IsAuthenticated]

    def get(self, request):
        from django.db.models import Sum
        import datetime
        now = timezone.now()
        duration = request.query_params.get("duration", "month")
        start_date = request.query_params.get("start_date")
        end_date = request.query_params.get("end_date")
        
        filter_kwargs = {'status': 'completed'}
        if duration == 'today':
            filter_kwargs['start_time__date'] = now.date()
        elif duration == 'week':
            week_start = now - datetime.timedelta(days=now.weekday())
            filter_kwargs['start_time__gte'] = week_start.replace(hour=0, minute=0, second=0, microsecond=0)
        elif duration == 'year':
            filter_kwargs['start_time__year'] = now.year
        elif duration == 'custom' and start_date and end_date:
            filter_kwargs['start_time__date__gte'] = start_date
            filter_kwargs['start_time__date__lte'] = end_date
        else:
            filter_kwargs['start_time__month'] = now.month
            filter_kwargs['start_time__year'] = now.year
            
        sessions = StudySession.objects.filter(**filter_kwargs).values('student_id').annotate(
            total_minutes=Sum('duration_minutes')
        ).order_by('-total_minutes')[:100]
        
        from apps.students.models import StudentProfile
        from api.v1.admin.serializers import StudentProfileSerializer
        
        student_ids = [s['student_id'] for s in sessions]
        profiles = list(StudentProfile.objects.filter(user_id__in=student_ids).exclude(status__in=['EXPIRED', 'SUSPENDED']).select_related('user'))
        
        serialized_profiles = StudentProfileSerializer(profiles, many=True, context={'request': request}).data
        serialized_map = {p['user_id']: p for p in serialized_profiles}
        
        leaderboard = []
        rank = 1
        for s in sessions:
            if s['student_id'] in serialized_map:
                student_data = serialized_map[s['student_id']]
                hours = s['total_minutes'] / 60.0
                lvl, t, c = 1, "Newbie", "#94a3b8" # Gray
                if hours >= 280: lvl, t, c = 9, "Library Legend", "#22d3ee" # Cyan
                elif hours >= 220: lvl, t, c = 8, "Grand Master Scholar", "#fbbf24" # Gold
                elif hours >= 170: lvl, t, c = 7, "Academic Champion", "#c084fc" # Purple
                elif hours >= 130: lvl, t, c = 6, "Book Slayer", "#f87171" # Red
                elif hours >= 90: lvl, t, c = 5, "Study Warrior", "#fb923c" # Orange
                elif hours >= 60: lvl, t, c = 4, "Knowledge Knight", "#BDAD40" # Light Blue
                elif hours >= 30: lvl, t, c = 3, "Library Squire", "#4ade80" # Green
                elif hours >= 10: lvl, t, c = 2, "Rookie Scholar", "#69A3E0" # Blue
                
                leaderboard.append({
                    "rank": rank,
                    "student": student_data,
                    "total_minutes": s['total_minutes'],
                    "hours_formatted": f"{s['total_minutes'] // 60}h {s['total_minutes'] % 60}m",
                    "level_info": {"level": lvl, "title": t, "badge_color": c}
                })
                rank += 1
                
        return standard_response(data=leaderboard)
