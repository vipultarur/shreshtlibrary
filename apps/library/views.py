from django.views.generic import TemplateView
from apps.seats.models import Seat
from apps.memberships.models import MembershipPlan
from apps.library.models import LibraryInfo, Achiever, Review

class LandingPageView(TemplateView):
    template_name = "library/landing.html"

    def get_context_data(self, **kwargs):
        context = super().get_context_data(**kwargs)
        
        try:
            total_seats = Seat.objects.count()
            occupied_seats = Seat.objects.filter(status='occupied').count()
            available_seats = Seat.objects.filter(status='available').count()
        except Exception:
            total_seats = 0
            occupied_seats = 0
            available_seats = 0

        try:
            plans = list(MembershipPlan.objects.all())
        except Exception:
            plans = []

        try:
            info = LibraryInfo.objects.first()
        except Exception:
            info = None

        try:
            achievers = list(Achiever.objects.all()[:6])
        except Exception:
            achievers = []

        try:
            reviews = list(Review.objects.filter(is_approved=True)[:6])
        except Exception:
            reviews = []

        try:
            seats = list(Seat.objects.all().order_by('floor', 'row', 'seat_number')[:30])
        except Exception:
            seats = []

        context.update({
            "total_seats": total_seats,
            "occupied_seats": occupied_seats,
            "available_seats": available_seats,
            "plans": plans,
            "info": info,
            "achievers": achievers,
            "reviews": reviews,
            "seats": seats[:20],  # Limit grid preview to first 20 seats
        })
        return context
