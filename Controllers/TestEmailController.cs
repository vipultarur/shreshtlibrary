using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/test-emails")]
    public class TestEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public TestEmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet("send-all")]
        public async Task<IActionResult> SendAllEmails([FromQuery] string email = "dddtarur@gmail.com")
        {
            try
            {
                // 1. Welcome
                await _emailService.SendWelcomeEmailAsync(email, "Test", "Student");

                // 2. Suspended
                await _emailService.SendSuspendedEmailAsync(email, "Violation of library rules");

                // 3. Activated
                await _emailService.SendActivatedEmailAsync(email);

                // 4. Congratulations
                await _emailService.SendCongratulationsEmailAsync(email, "1 Month Premium Plan");

                // 5. Reminder
                await _emailService.SendReminderEmailAsync(email, "15", "120", "500");

                // 6. Notification
                await _emailService.SendNotificationEmailAsync(email, "New Medical Journals", "Diwali Celebration");

                // 7. Plan Details
                await _emailService.SendPlanDetailsEmailAsync(email, "Premium Plus", "31 Dec 2026", "A12");

                // 8. OTP
                await _emailService.SendOtpEmailAsync(email, "Test Student", "123456");

                // 9. Forgot Password
                await _emailService.SendForgotPasswordEmailAsync(email, "Test Student", "https://shreshtlibrary.onrender.com/reset-password?token=test");

                // 10. Receipt
                await _emailService.SendReceiptEmailAsync(email, "₹500", "Premium Plan", "31 Dec 2026");

                // 11. Seat Allocated
                await _emailService.SendSeatAllocatedEmailAsync(email, "B15", "Quiet Zone", "Morning Shift");

                // 12. Holiday Announcement
                await _emailService.SendHolidayAnnouncementEmailAsync(email, "Diwali Festival", "12 Nov 2026");

                // 13. Holiday Cancelled
                await _emailService.SendHolidayCancelledEmailAsync(email, "Diwali Festival", "12 Nov 2026");

                // 14. Seat Released
                await _emailService.SendSeatReleasedEmailAsync(email, "B15", "Administrative reassignment");

                return Ok(new { success = true, message = $"All 14 test emails sent successfully to {email}" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, details = ex.StackTrace });
            }
        }
    }
}
