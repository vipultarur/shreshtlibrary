import uuid

from django.db import migrations


def _next_student_id(StudentProfile, start):
    sequence = start
    while True:
        candidate = f"SHR-{sequence:04d}"
        if not StudentProfile.objects.filter(student_id=candidate).exists():
            return candidate, sequence + 1
        sequence += 1


def forwards(apps, schema_editor):
    StudentProfile = apps.get_model("students", "StudentProfile")
    MembershipPlan = apps.get_model("memberships", "MembershipPlan")
    Membership = apps.get_model("memberships", "Membership")
    Payment = apps.get_model("payments", "Payment")
    QRCode = apps.get_model("attendance", "QRCode")
    Floor = apps.get_model("seats", "Floor")
    SeatRow = apps.get_model("seats", "SeatRow")
    Seat = apps.get_model("seats", "Seat")
    SeatAssignment = apps.get_model("seats", "SeatAssignment")
    LibraryInfo = apps.get_model("library", "LibraryInfo")
    Facility = apps.get_model("library", "Facility")
    Achiever = apps.get_model("library", "Achiever")

    sequence = 1
    for profile in StudentProfile.objects.select_related("user").order_by("id"):
        updates = []
        if not profile.student_id:
            profile.student_id, sequence = _next_student_id(StudentProfile, sequence)
            updates.append("student_id")
        if not profile.status:
            profile.status = "LIVE"
            updates.append("status")
        if profile.status and profile.status.islower():
            profile.status = profile.status.upper()
            updates.append("status")
        if updates:
            profile.save(update_fields=list(set(updates)))

    for plan in MembershipPlan.objects.all():
        updates = []
        target_days = max(int(plan.duration_months or 1) * 30, 1)
        if not plan.duration_days or plan.duration_days == 30:
            plan.duration_days = target_days
            updates.append("duration_days")
        if updates:
            plan.save(update_fields=updates)

    for membership in Membership.objects.select_related("plan").all():
        updates = []
        if membership.status:
            normalized = membership.status.lower()
            if normalized != membership.status:
                membership.status = normalized
                updates.append("status")
        if membership.is_active != (membership.status == "active"):
            membership.is_active = membership.status == "active"
            updates.append("is_active")
        if membership.plan_id and not membership.plan_name_snapshot:
            membership.plan_name_snapshot = membership.plan.name
            updates.append("plan_name_snapshot")
        if membership.plan_id and not membership.price_snapshot:
            membership.price_snapshot = membership.plan.price
            updates.append("price_snapshot")
        if updates:
            membership.save(update_fields=list(set(updates)))

    for payment in Payment.objects.all().order_by("payment_date", "id"):
        updates = []
        if not payment.method and payment.payment_mode:
            payment.method = payment.payment_mode.upper().replace(" ", "_")
            updates.append("method")
        if not payment.transaction_ref and payment.transaction_id:
            payment.transaction_ref = payment.transaction_id
            updates.append("transaction_ref")
        if not payment.payment_id:
            payment.payment_id = f"PAY-{payment.payment_date:%Y%m%d}-{payment.id:03d}"
            updates.append("payment_id")
        if updates:
            payment.save(update_fields=list(set(updates)))

    for qr in QRCode.objects.all():
        updates = []
        if not qr.token:
            qr.token = uuid.uuid4()
            updates.append("token")
        if not qr.qr_hash:
            qr.qr_hash = qr.code
            updates.append("qr_hash")
        if not qr.expires_at:
            qr.expires_at = qr.expiry_timestamp
            updates.append("expires_at")
        qr.is_active = not qr.is_expired
        updates.append("is_active")
        qr.save(update_fields=list(set(updates)))

    for seat in Seat.objects.all().order_by("floor", "row", "seat_number"):
        floor, _ = Floor.objects.get_or_create(
            name=seat.floor or "Ground",
            defaults={"order": Floor.objects.count()},
        )
        row, _ = SeatRow.objects.get_or_create(
            floor=floor,
            label=seat.row or "A",
            defaults={"order": SeatRow.objects.filter(floor=floor).count()},
        )
        updates = []
        if seat.row_ref_id != row.id:
            seat.row_ref = row
            updates.append("row_ref")
        active_assignment = SeatAssignment.objects.filter(seat=seat, released_date__isnull=True).order_by("-assigned_date").first()
        if active_assignment and not seat.student_id:
            seat.student_id = active_assignment.student_id
            updates.append("student")
        if seat.student_id and seat.status != "occupied":
            seat.status = "occupied"
            updates.append("status")
        if updates:
            seat.save(update_fields=list(set(updates)))

    for info in LibraryInfo.objects.all():
        for index, raw_name in enumerate(filter(None, [part.strip() for part in (info.facilities or "").replace("\n", ",").split(",")])):
            Facility.objects.get_or_create(name=raw_name[:100], defaults={"order": index, "is_active": True})

    for index, achiever in enumerate(Achiever.objects.all().order_by("-year", "id")):
        if achiever.order != index:
            achiever.order = index
            achiever.save(update_fields=["order"])


def backwards(apps, schema_editor):
    pass


class Migration(migrations.Migration):

    dependencies = [
        ("core", "0002_activitylog_admin"),
        ("students", "0002_studentprofile_created_at_studentprofile_gender_and_more"),
        ("memberships", "0002_membership_created_at_membership_created_by_and_more"),
        ("attendance", "0003_attendance_is_present_attendance_marked_at_and_more"),
        ("payments", "0002_payment_created_at_payment_method_payment_paid_at_and_more"),
        ("seats", "0002_floor_seat_assigned_at_seat_assigned_by_seat_notes_and_more"),
        ("library", "0002_facility_achiever_created_at_achiever_goal_and_more"),
        ("notifications", "0002_notification_created_at_notification_created_by_and_more"),
    ]

    operations = [
        migrations.RunPython(forwards, backwards),
    ]
