from rest_framework import serializers
from apps.library.models import LibraryInfo, Achiever, Review

class LibraryInfoSerializer(serializers.ModelSerializer):
    class Meta:
        model = LibraryInfo
        fields = ['rules', 'facilities', 'about']


class AchieverSerializer(serializers.ModelSerializer):
    class Meta:
        model = Achiever
        fields = ['id', 'name', 'photo', 'achievement', 'year']
        read_only_fields = ['id']


class ReviewSerializer(serializers.ModelSerializer):
    student_name = serializers.CharField(source='student.get_full_name', read_only=True)

    class Meta:
        model = Review
        fields = ['id', 'student_name', 'rating', 'comment', 'created_at']
        read_only_fields = ['id', 'student_name', 'created_at']

    def validate_rating(self, value):
        if value < 1 or value > 5:
            raise serializers.ValidationError("Rating must be between 1 and 5.")
        return value
