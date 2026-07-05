using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebApplication1.Services;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models.DTOs.Admin;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/test-emails")]
    public class TestEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IStudentAdminService _studentAdminService;
        private readonly ApplicationDbContext _context;

        public TestEmailController(IEmailService emailService, IStudentAdminService studentAdminService, ApplicationDbContext context)
        {
            _emailService = emailService;
            _studentAdminService = studentAdminService;
            _context = context;
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

                return Ok(new { success = true, message = $"Test emails sent successfully to {email}" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, details = ex.StackTrace });
            }
        }

        [HttpGet("trigger-suspend")]
        public async Task<IActionResult> TriggerSuspend([FromQuery] string email)
        {
            var user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound("User not found");

            var result = await _studentAdminService.SuspendStudentAsync(user.Id.ToString(), "Test Suspension");
            return Ok(result);
        }

        [HttpGet("trigger-activate")]
        public async Task<IActionResult> TriggerActivate([FromQuery] string email)
        {
            var user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound("User not found");

            var result = await _studentAdminService.ActivateStudentAsync(user.Id.ToString());
            return Ok(result);
        }
    }
}
