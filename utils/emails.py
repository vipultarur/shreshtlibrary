import os
from django.core.mail import send_mail, EmailMultiAlternatives
from django.template.loader import render_to_string
from django.utils.html import strip_tags
from django.conf import settings

def send_transactional_email(email_type, profile, context=None):
    if context is None:
        context = {}
        
    template_data = {
        "title": "Notification",
        "subtitle": "",
        "actionText": "View Dashboard",
        "footer": "Thanks for being awesome!",
    }
    
    primary_color = "#4f46e5" # default indigo
    image_url = "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/notification.png"
    subject = "Shresht Library Update"
    action_url = "https://shreshtlibrary.vercel.app"
    
    if email_type == "SUSPEND_STUDENT":
        subject = "Action Required: Account Suspended ⚠️"
        primary_color = "#ef4444" # red-500
        image_url = "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/suspended.png"
        template_data.update({
            "title": "Account Suspended",
            "subtitle": "Your library account has been suspended due to a policy violation or unpaid dues.",
            "actionText": "Contact Support",
            "footer": "Please reach out to resolve this issue.",
        })
        # Add the dynamic reason to subtitle or as a stat
        if getattr(profile, 'suspension_reason', None):
            template_data["stats"] = [{"label": "Reason", "value": profile.suspension_reason}]
            
    elif email_type == "ACTIVATE_STUDENT":
        subject = "Your Subscription Details 📚" # Or something like "Account Reactivated"
        primary_color = "#10b981" # emerald-500
        image_url = "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/congratulations.png" # No specific 'reactivate' in React, maybe 'congratulations' or 'receipt'
        template_data.update({
            "title": "Account Reactivated!",
            "subtitle": "Good news! Your Shresht Library account has been successfully reactivated. You can now access library facilities again.",
            "actionText": "Go to Dashboard",
            "footer": "We're glad to have you back!",
        })
        
    elif email_type == "NEW_PLAN":
        plan = context.get("plan")
        start_date = context.get("start_date")
        end_date = context.get("end_date")
        
        subject = "Membership Plan Assigned 🚀"
        primary_color = "#3b82f6" # blue-500
        image_url = "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/plan_details.png"
        template_data.update({
            "title": "New Plan Active",
            "subtitle": f"A new library membership plan '{plan.name}' has been successfully assigned to your account.",
            "actionText": "View Membership Details",
            "footer": "Thank you for choosing Shresht Library!",
            "stats": [
                {"label": "Plan Name", "value": plan.name},
                {"label": "Valid From", "value": start_date.strftime("%d %b, %Y") if hasattr(start_date, 'strftime') else str(start_date)},
                {"label": "Valid Until", "value": end_date.strftime("%d %b, %Y") if hasattr(end_date, 'strftime') else str(end_date)}
            ]
        })
    else:
        print(f"Unknown email type: {email_type}")
        return False
        
    render_context = {
        'user': profile.user,
        'profile': profile,
        'content': template_data,
        'primary_color': primary_color,
        'image_url': image_url,
        'action_url': action_url,
    }
        
    try:
        html_message = render_to_string("emails/base_email.html", render_context)
        plain_message = strip_tags(html_message)
        
        recipient_list = [profile.user.email] if profile.user.email else []

        if not recipient_list:
            return False
            
        email = EmailMultiAlternatives(
            subject=subject,
            body=plain_message,
            from_email=settings.EMAIL_HOST_USER,
            to=recipient_list
        )
        email.attach_alternative(html_message, "text/html")
        email.send(fail_silently=False)
        return True
    except Exception as e:
        print(f"Error sending email {email_type}: {e}")
        return False
