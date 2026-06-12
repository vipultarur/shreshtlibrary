from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from rest_framework.permissions import AllowAny

from drf_spectacular.utils import extend_schema

from shreshtlibrary.utils.permissions import IsStudent
from utils.response import standard_response
from apps.library.models import LibraryInfo, Achiever, Review
from .serializers import LibraryInfoSerializer, AchieverSerializer, ReviewSerializer

class LibraryInfoView(APIView):
    permission_classes = [AllowAny]

    @extend_schema(responses={200: LibraryInfoSerializer}, tags=['Library Info'])
    def get(self, request):
        info = LibraryInfo.objects.first()
        if not info:
            return standard_response(data=None)
        serializer = LibraryInfoSerializer(info)
        return standard_response(data=serializer.data)


class AchieversListView(APIView):
    permission_classes = [AllowAny]

    @extend_schema(responses={200: AchieverSerializer(many=True)}, tags=['Library Info'])
    def get(self, request):
        achievers = Achiever.objects.all().order_by('-year')
        serializer = AchieverSerializer(achievers, many=True)
        return standard_response(data=serializer.data)


class ReviewsListView(APIView):
    permission_classes = [AllowAny]

    @extend_schema(responses={200: ReviewSerializer(many=True)}, tags=['Library Info'])
    def get(self, request):
        reviews = Review.objects.filter(is_approved=True).order_by('-created_at')
        serializer = ReviewSerializer(reviews, many=True)
        return standard_response(data=serializer.data)



class StudentSubmitReviewView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(request=ReviewSerializer, responses={201: ReviewSerializer}, tags=['Library Info'])
    def post(self, request):
        serializer = ReviewSerializer(data=request.data)
        if serializer.is_valid():
            review = serializer.save(student=request.user, is_approved=False)  # pending approval
            
            try:
                from apps.notifications.models import AdminInboxNotification
                from api.v1.v2_admin import _full_name
                AdminInboxNotification.objects.create(
                    type='SUPPORT',
                    title='New Review/Message Submitted',
                    message=f"Student {_full_name(request.user)} submitted a review/message: {review.comment[:50]}...",
                    related_id=str(review.id),
                    student=request.user
                )
            except Exception:
                pass

            return standard_response(
                message="Review submitted. It will be visible once approved by admin.",
                data=serializer.data,
                status_code=status.HTTP_201_CREATED
            )
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)
