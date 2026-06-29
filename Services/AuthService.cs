using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Threading.Tasks;
using System.Net;
using System.Security.Claims;

namespace WebApplication1.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<IActionResult> RegisterAsync(UserRegisterRequest request, string ipAddress, CancellationToken ct = default)
        {
            if (request.Password != request.ConfirmPassword)
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Passwords do not match.", errors = new { password = new[] { "Passwords do not match." } } });
            }
            if (await _context.AccountsCustomusers.AnyAsync(u => u.Mobile == request.Mobile, ct))
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Mobile number already registered.", errors = new { mobile = new[] { "Mobile number already registered." } } });
            }
            if (await _context.AccountsCustomusers.AnyAsync(u => u.Email == request.Email, ct))
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Email already registered.", errors = new { email = new[] { "Email already registered." } } });
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
            {
                var user = new AccountsCustomuser
                {
                    Username = request.Mobile, // Mobile as username
                    Email = request.Email,
                    Mobile = request.Mobile,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Role = WebApplication1.Utils.Constants.Roles.Student,
                    Password = WebApplication1.Utils.PasswordHasher.HashDjangoPassword(request.Password),
                    DateJoined = System.DateTime.UtcNow,
                    IsActive = true
                };

                _context.AccountsCustomusers.Add(user);
                await _context.SaveChangesAsync(ct);

                // Generate Student ID atomically using the uniquely generated user.Id
                string nextStudentId = $"SHR-{user.Id:D4}";

                var profile = new StudentsStudentprofile
                {
                    UserId = user.Id,
                    Goal = request.Goal ?? "Other",
                    Dob = System.DateOnly.FromDateTime(request.Dob),
                    Caste = request.Caste ?? "",
                    Address = request.Address ?? "",
                    ParentMobile = request.ParentMobile ?? "",
                    Gender = "Other", // Default from Django
                    Status = WebApplication1.Utils.Constants.StudentStatus.Pending,
                    PreferredLanguage = "en",
                    JoiningDate = System.DateOnly.FromDateTime(System.DateTime.UtcNow),
                    StudentId = nextStudentId,
                    CreatedAt = System.DateTime.UtcNow,
                    UpdatedAt = System.DateTime.UtcNow
                };

                _context.StudentsStudentprofiles.Add(profile);

                // Add Notification
                var notification = new NotificationsAdmininboxnotification
                {
                    Type = "NEW_STUDENT",
                    Title = "New Student Registered",
                    Message = $"Student {user.Username} has just registered.",
                    RelatedId = user.Id.ToString(),
                    StudentId = user.Id,
                    CreatedAt = System.DateTime.UtcNow,
                    IsRead = false
                };
                _context.NotificationsAdmininboxnotifications.Add(notification);

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return await GenerateStudentTokensAndLogAsync(user, "Registered new account", ipAddress, "", "", ct);
            }
            catch (System.Exception)
            {
                await transaction.RollbackAsync(ct);
                return new ObjectResult(new { success = false, status = "error", message = "Internal server error during registration.", errors = new { non_field_errors = new[] { "Internal server error during registration." } } }) { StatusCode = 500 };
            }
            });
        }

        public async Task<IActionResult> SendOtpAsync(SendOtpRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Mobile))
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Validation failed.", errors = new { mobile = new[] { "This field is required." } } });
            }

            var user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Mobile == request.Mobile, ct);
            if (user == null)
            {
                return new NotFoundObjectResult(new { success = false, status = "error", message = "Mobile number not registered.", errors = new { mobile = new[] { "Mobile number not registered." } } });
            }

            string rawOtp = new System.Random().Next(0, 999999).ToString("D6");
            user.Otp = WebApplication1.Utils.PasswordHasher.HashDjangoPassword(rawOtp);
            user.OtpExpiry = System.DateTime.UtcNow.AddMinutes(5);
            user.OtpAttempts = 0;

            var ip = ipAddress;
            _context.CoreActivitylogs.Add(new CoreActivitylog
            {
                Action = "Sent login OTP",
                Timestamp = System.DateTime.UtcNow,
                IpAddress = System.Net.IPAddress.TryParse(ip, out var parsedIp) ? parsedIp : null,
                Details = $"{{\"path\": \"{path}\", \"method\": \"{method}\"}}",
                UserId = user.Id
            });

            await _context.SaveChangesAsync(ct);

            // In production, integrate SMS gateway here
            System.Console.WriteLine($"OTP for {request.Mobile}: {rawOtp}");

            return new OkObjectResult(new { success = true, status = "success", message = "OTP sent successfully." });
        }

        public async Task<IActionResult> VerifyOtpAsync(VerifyOtpRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Mobile) || string.IsNullOrWhiteSpace(request.Otp))
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Validation failed.", errors = new { mobile = new[] { "This field is required." }, otp = new[] { "This field is required." } } });
            }

            var user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Mobile == request.Mobile, ct);
            if (user == null)
            {
                return new NotFoundObjectResult(new { success = false, status = "error", message = "Mobile number not registered.", errors = new { mobile = new[] { "Mobile number not registered." } } });
            }

            if (user.OtpAttempts >= 5)
            {
                return new ObjectResult(new { success = false, status = "error", message = "Too many failed attempts. Please request a new OTP." }) { StatusCode = 403 };
            }

            if (user.OtpExpiry < System.DateTime.UtcNow)
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "OTP has expired.", errors = new { otp = new[] { "OTP has expired." } } });
            }

            if (!string.IsNullOrEmpty(user.Otp) && WebApplication1.Utils.PasswordHasher.VerifyDjangoPassword(request.Otp, user.Otp))
            {
                await _context.AccountsCustomusers
                    .Where(u => u.Id == user.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.Otp, (string?)null)
                        .SetProperty(u => u.OtpAttempts, 0), ct);

                return await GenerateStudentTokensAndLogAsync(user, "Logged in via OTP", ipAddress, path, method, ct);
            }
            else
            {
                await _context.AccountsCustomusers
                    .Where(u => u.Id == user.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.OtpAttempts, u => u.OtpAttempts + 1), ct);

                return new BadRequestObjectResult(new { success = false, status = "error", message = "Invalid OTP.", errors = new { otp = new[] { "Invalid OTP." } } });
            }
        }

        public async Task<IActionResult> LoginEmailAsync(LoginEmailRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Validation failed.", errors = new { email = new[] { "This field is required." }, password = new[] { "This field is required." } } });
            }

            // Fallback: DRF auth checks username=email, then if not found, checks email=email
            var user = await _context.AccountsCustomusers
                .FirstOrDefaultAsync(u => u.Username == request.Email, ct) ??
                await _context.AccountsCustomusers
                .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

            bool isValidPassword = false;
            if (user != null && user.Role == "student")
            {
                isValidPassword = WebApplication1.Utils.PasswordHasher.VerifyDjangoPassword(request.Password, user.Password);
            }
            else
            {
                // Dummy hash to prevent timing attacks
                WebApplication1.Utils.PasswordHasher.VerifyDjangoPassword(request.Password, "pbkdf2_sha256$260000$dummy$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
            }

            if (!isValidPassword || user == null || !user.IsActive)
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Invalid credentials or not a student.", errors = new { non_field_errors = new[] { "Invalid credentials or not a student." } } });
            }

            return await GenerateStudentTokensAndLogAsync(user, "Logged in via Email/Password", ipAddress, path, method, ct);
        }

        public async Task<IActionResult> LoginMobileAsync(LoginMobileRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Mobile) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Validation failed.", errors = new { mobile = new[] { "This field is required." }, password = new[] { "This field is required." } } });
            }

            var user = await _context.AccountsCustomusers
                .FirstOrDefaultAsync(u => u.Username == request.Mobile, ct);

            bool isValidPassword = false;
            if (user != null && user.Role == "student")
            {
                isValidPassword = WebApplication1.Utils.PasswordHasher.VerifyDjangoPassword(request.Password, user.Password);
            }
            else
            {
                // Dummy hash to prevent timing attacks
                WebApplication1.Utils.PasswordHasher.VerifyDjangoPassword(request.Password, "pbkdf2_sha256$260000$dummy$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
            }

            if (!isValidPassword || user == null || !user.IsActive)
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Invalid credentials or not a student.", errors = new { non_field_errors = new[] { "Invalid credentials or not a student." } } });
            }

            return await GenerateStudentTokensAndLogAsync(user, "Logged in via Mobile/Password", ipAddress, path, method, ct);
        }

        private async Task<IActionResult> GenerateStudentTokensAndLogAsync(AccountsCustomuser user, string actionDesc, string ipAddress, string path, string method, CancellationToken ct)
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtSettings = _config.GetSection("Jwt");
            var secret = jwtSettings["Secret"];
            if (string.IsNullOrEmpty(secret) || secret.Length < 32)
            {
                throw new System.InvalidOperationException("JWT Secret is missing or insufficiently secure.");
            }
            var key = System.Text.Encoding.UTF8.GetBytes(secret);

            var subClaim = user.SupabaseUid?.ToString() ?? user.Id.ToString();

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("user_id", user.Id.ToString()),
                    new System.Security.Claims.Claim("sub", subClaim),
                    new System.Security.Claims.Claim("role", user.Role),
                    new System.Security.Claims.Claim("token_type", "access"),
                }),
                Expires = System.DateTime.UtcNow.AddHours(1),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var refreshDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("user_id", user.Id.ToString()),
                    new System.Security.Claims.Claim("sub", subClaim),
                    new System.Security.Claims.Claim("role", user.Role),
                    new System.Security.Claims.Claim("token_type", "refresh"),
                }),
                Expires = System.DateTime.UtcNow.AddDays(30),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
            var refreshToken = tokenHandler.WriteToken(tokenHandler.CreateToken(refreshDescriptor));

            var ip = ipAddress;
            _context.CoreActivitylogs.Add(new CoreActivitylog
            {
                Action = actionDesc,
                Timestamp = System.DateTime.UtcNow,
                IpAddress = System.Net.IPAddress.TryParse(ip, out var parsedIp) ? parsedIp : null,
                Details = $"{{\"path\": \"{path}\", \"method\": \"{method}\"}}",
                UserId = user.Id
            });
            await _context.SaveChangesAsync(ct);

            return new OkObjectResult(new
            {
                success = true,
                status = "success",
                message = "Login successful.",
                data = new
                {
                    tokens = new { refresh = refreshToken, access = accessToken },
                    user = new
                    {
                        id = user.Id,
                        username = user.Username,
                        email = user.Email,
                        mobile = user.Mobile,
                        role = user.Role,
                        is_active = user.IsActive
                    }
                }
            });
        }



        private object ParsePermissions(string permissionsJson)
        {
            if (string.IsNullOrWhiteSpace(permissionsJson)) return new { };
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<object>(permissionsJson) ?? new { };
            }
            catch
            {
                return new { }; // Return empty object if JSON parsing fails
            }
        }

        public async Task<IActionResult> AdminLoginAsync(AdminLoginRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            Console.WriteLine($"[AdminLogin] Received Username: '{request.Username}', Password: '{request.Password}'");
            // Fallback checking logic matching DRF (username -> email -> mobile)
            var user = await _context.AccountsAdminusers
                .FirstOrDefaultAsync(u => u.Username == request.Username, ct) ??
                await _context.AccountsAdminusers
                .FirstOrDefaultAsync(u => u.Email == request.Username, ct) ??
                await _context.AccountsAdminusers
                .FirstOrDefaultAsync(u => u.Mobile == request.Username, ct);

            bool isValidPassword = false;
            if (user != null && (user.Role == "admin" || user.Role == "super_admin"))
            {
                isValidPassword = WebApplication1.Utils.PasswordHasher.VerifyDjangoPassword(request.Password, user.Password);
            }
            else
            {
                // Dummy hash to prevent timing attacks
                WebApplication1.Utils.PasswordHasher.VerifyDjangoPassword(request.Password, "pbkdf2_sha256$260000$dummy$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
            }

            if (!isValidPassword || user == null || !user.IsActive)
            {
                Console.WriteLine("[AdminLogin] Password verification failed or user invalid/inactive!");
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Invalid credentials or not an admin.", errors = new { non_field_errors = new[] { "Invalid credentials or not an admin." } } });
            }

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtSettings = _config.GetSection("Jwt");
            var secret = jwtSettings["Secret"];
            if (string.IsNullOrEmpty(secret) || secret.Length < 32)
            {
                throw new System.InvalidOperationException("JWT Secret is missing or insufficiently secure.");
            }
            var key = System.Text.Encoding.UTF8.GetBytes(secret);

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("user_id", user.Id.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
                    new System.Security.Claims.Claim("role", user.Role),
                    new System.Security.Claims.Claim("token_type", "access"),
                }),
                Expires = System.DateTime.UtcNow.AddHours(1),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var refreshDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("user_id", user.Id.ToString()),
                    new System.Security.Claims.Claim("role", user.Role),
                    new System.Security.Claims.Claim("token_type", "refresh"),
                }),
                Expires = System.DateTime.UtcNow.AddDays(30),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
            var refreshToken = tokenHandler.WriteToken(tokenHandler.CreateToken(refreshDescriptor));

            // Log activity
            var ip = ipAddress;
            _context.CoreActivitylogs.Add(new CoreActivitylog
            {
                Action = "Admin logged in",
                Timestamp = System.DateTime.UtcNow,
                IpAddress = System.Net.IPAddress.TryParse(ip, out var parsedIp) ? parsedIp : null,
                Details = $"{{\"path\": \"{path}\", \"method\": \"{method}\"}}",
                AdminId = user.Id
            });
            await _context.SaveChangesAsync(ct);

            return new OkObjectResult(new
            {
                success = true,
                status = "success",
                message = "Admin login successful.",
                data = new
                {
                    tokens = new { access = accessToken, refresh = refreshToken },
                    user = new
                    {
                        id = user.Id,
                        username = user.Username,
                        first_name = user.FirstName,
                        last_name = user.LastName,
                        email = user.Email,
                        mobile = user.Mobile,
                        role = user.Role,
                        permissions = ParsePermissions(user.Permissions),
                        profile_image = user.ProfileImage != null ? $"/media/{user.ProfileImage}" : null,
                        is_active = user.IsActive,
                        date_joined = user.DateJoined.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
                        last_login = user.LastLogin?.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ")
                    }
                }
            });
        }

        public async Task<IActionResult> ForgotPasswordAsync(ForgotPasswordRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Validation failed.", errors = new { email = new[] { "This field is required." } } });
            }

            var user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            if (user != null)
            {
                string resetToken = $"reset-{System.Guid.NewGuid()}";
                user.Otp = resetToken; // Store as plain text or hash depending on original DRF logic (DRF stores plain text for forgot password token: user.otp = reset_token)
                user.OtpExpiry = System.DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync(ct);

                var ip = ipAddress;
                _context.CoreActivitylogs.Add(new CoreActivitylog
                {
                    Action = "Requested password reset link",
                    Timestamp = System.DateTime.UtcNow,
                    IpAddress = System.Net.IPAddress.TryParse(ip, out var parsedIp) ? parsedIp : null,
                    Details = $"{{\"path\": \"{path}\", \"method\": \"{method}\"}}",
                    UserId = user.Id
                });
                await _context.SaveChangesAsync(ct);

                // In production, integrate email sending here
                System.Console.WriteLine($"Reset link: https://shreshtlibrary.onrender.com/reset-password?token={resetToken}");

                return new OkObjectResult(new { success = true, status = "success", message = "Password reset link sent to your email." });
            }

            return new NotFoundObjectResult(new { success = false, status = "error", message = "User not found.", errors = new { detail = new[] { "User not found." } } });
        }

        public async Task<IActionResult> ResetPasswordAsync(ResetPasswordRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Validation failed.", errors = new { token = new[] { "This field is required." }, new_password = new[] { "This field is required." } } });
            }

            var user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Otp == request.Token && u.OtpExpiry > System.DateTime.UtcNow, ct);
            if (user != null)
            {
                user.Password = WebApplication1.Utils.PasswordHasher.HashDjangoPassword(request.NewPassword);
                user.Otp = null;
                await _context.SaveChangesAsync(ct);

                var ip = ipAddress;
                _context.CoreActivitylogs.Add(new CoreActivitylog
                {
                    Action = "Reset password",
                    Timestamp = System.DateTime.UtcNow,
                    IpAddress = System.Net.IPAddress.TryParse(ip, out var parsedIp) ? parsedIp : null,
                    Details = $"{{\"path\": \"{path}\", \"method\": \"{method}\"}}",
                    UserId = user.Id
                });
                await _context.SaveChangesAsync(ct);

                return new OkObjectResult(new { success = true, status = "success", message = "Password reset successfully." });
            }

            return new BadRequestObjectResult(new { success = false, status = "error", message = "Invalid or expired reset token.", errors = new { token = new[] { "Invalid or expired reset token." } } });
        }

        
        public async Task<IActionResult> LogoutAsync(LogoutRequest request, string authHeader, string currentUserIdStr, string role, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            if (request != null && !string.IsNullOrWhiteSpace(request.Refresh))
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                try
                {
                    var token = handler.ReadJwtToken(request.Refresh);
                    var jti = token.Id ?? "";
                    var userId = token.Claims.FirstOrDefault(c => c.Type == "user_id" || c.Type == "sub")?.Value ?? "";
                    
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(request.Refresh);
                        var hashBytes = sha256.ComputeHash(bytes);
                        var hash = Convert.ToHexString(hashBytes).ToLower();

                        _context.AccountsAuthtokenrevocations.Add(new AccountsAuthtokenrevocation
                        {
                            TokenHash = hash,
                            Jti = jti,
                            UserIdentifier = userId,
                            RevokedAt = System.DateTime.UtcNow,
                            ExpiresAt = token.ValidTo
                        });
                    }
                }
                catch (Exception ex) 
                { 
                    System.Console.WriteLine($"[Logout] Token decode error for refresh token: {ex.Message}");
                }
            }

            // Access token revocation
            /* authHeader passed */
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var accessToken = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                try
                {
                    var token = handler.ReadJwtToken(accessToken);
                    var jti = token.Id ?? "";
                    var userId = token.Claims.FirstOrDefault(c => c.Type == "user_id" || c.Type == "sub")?.Value ?? "";
                    
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(accessToken);
                        var hashBytes = sha256.ComputeHash(bytes);
                        var hash = Convert.ToHexString(hashBytes).ToLower();

                        _context.AccountsAuthtokenrevocations.Add(new AccountsAuthtokenrevocation
                        {
                            TokenHash = hash,
                            Jti = jti,
                            UserIdentifier = userId,
                            RevokedAt = System.DateTime.UtcNow,
                            ExpiresAt = token.ValidTo
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[Logout] Token decode error for access token: {ex.Message}");
                }
            }

            /* currentUserIdStr passed */
            long.TryParse(currentUserIdStr, out long currentUserId);
            /* role passed */

            var ip = ipAddress;
            var log = new CoreActivitylog
            {
                Action = "Logged out",
                Timestamp = System.DateTime.UtcNow,
                IpAddress = System.Net.IPAddress.TryParse(ip, out var parsedIp) ? parsedIp : null,
                Details = $"{{\"path\": \"{path}\", \"method\": \"{method}\"}}"
            };

            if (role == "admin" || role == "super_admin") log.AdminId = currentUserId;
            else log.UserId = currentUserId;

            _context.CoreActivitylogs.Add(log);
            await _context.SaveChangesAsync(ct);

            return new OkObjectResult(new { success = true, status = "success", message = "Logged out successfully." });
        }

        public async Task<IActionResult> RefreshTokenAsync(TokenRefreshRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Refresh))
            {
                return new BadRequestObjectResult(new { success = false, status = "error", message = "Refresh token is required.", errors = new { refresh = new[] { "Refresh token is required." } } });
            }

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            try
            {
                var token = handler.ReadJwtToken(request.Refresh);
                var tokenType = token.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;
                if (tokenType != "refresh")
                {
                    return new UnauthorizedObjectResult(new { success = false, status = "error", message = "Invalid refresh token.", errors = new { detail = new[] { "Invalid refresh token." } } });
                }

                // Check revocation
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(request.Refresh);
                    var hashBytes = sha256.ComputeHash(bytes);
                    var hash = System.BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    
                    var jti = token.Id ?? "";
                    bool isRevoked = await _context.AccountsAuthtokenrevocations.AnyAsync(r => 
                        (r.TokenHash == hash || (jti != "" && r.Jti == jti)) && 
                        (r.ExpiresAt == null || r.ExpiresAt > System.DateTime.UtcNow), ct);
                        
                    if (isRevoked)
                    {
                        return new UnauthorizedObjectResult(new { success = false, status = "error", message = "Refresh token has been revoked.", errors = new { detail = new[] { "Refresh token has been revoked." } } });
                    }
                }

                var userIdStr = token.Claims.FirstOrDefault(c => c.Type == "user_id" || c.Type == "sub")?.Value;
                long.TryParse(userIdStr, out long userId);
                var role = token.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

                bool isActive = false;
                string fetchedUsername = "";
                if (role == "admin" || role == "super_admin")
                {
                    var adminUser = await _context.AccountsAdminusers.FirstOrDefaultAsync(u => u.Id == userId, ct);
                    if (adminUser != null) { isActive = adminUser.IsActive; fetchedUsername = adminUser.Username; }
                }
                else
                {
                    var customUser = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Id == userId, ct);
                    if (customUser != null) { isActive = customUser.IsActive; fetchedUsername = customUser.Username; }
                }

                if (!isActive)
                {
                    return new UnauthorizedObjectResult(new { success = false, status = "error", message = "User account is inactive.", errors = new { detail = new[] { "User account is inactive." } } });
                }

                var jwtSettings = _config.GetSection("Jwt");
                var secret = jwtSettings["Secret"];
                if (string.IsNullOrEmpty(secret) || secret.Length < 32)
                {
                    throw new System.InvalidOperationException("JWT Secret is missing or insufficiently secure.");
                }
                var key = System.Text.Encoding.UTF8.GetBytes(secret);

                var claims = token.Claims.Where(c => c.Type != "exp" && c.Type != "iss" && c.Type != "aud" && c.Type != "token_type" && c.Type != "nbf" && c.Type != "iat").ToList();
                claims.Add(new System.Security.Claims.Claim("token_type", "access"));
                if (!string.IsNullOrEmpty(fetchedUsername) && !claims.Any(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                {
                    claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, fetchedUsername));
                }

                var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
                {
                    Subject = new System.Security.Claims.ClaimsIdentity(claims),
                    Expires = System.DateTime.UtcNow.AddHours(1),
                    Issuer = jwtSettings["Issuer"],
                    Audience = jwtSettings["Audience"],
                    SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
                };

                var newAccessToken = handler.WriteToken(handler.CreateToken(tokenDescriptor));

                return new OkObjectResult(new
                {
                    access = newAccessToken
                });
            }
            catch
            {
                return new UnauthorizedObjectResult(new { success = false, status = "error", message = "Invalid refresh token.", errors = new { detail = new[] { "Invalid refresh token." } } });
            }
        }
        public class FcmTokenUpdateDto
        {
            public string Token { get; set; }
        }

        
        public async Task<IActionResult> UpdateFcmTokenAsync(AuthController.FcmTokenUpdateDto dto, long userId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(dto?.Token))
            {
                return new BadRequestObjectResult(new { success = false, message = "Token is required." });
            }

            

            var existingToken = await _context.NotificationsDevicetokens
                .FirstOrDefaultAsync(dt => dt.Token == dto.Token, ct);

            if (existingToken != null)
            {
                if (existingToken.StudentId != userId)
                {
                    existingToken.StudentId = userId;
                }
            }
            else
            {
                _context.NotificationsDevicetokens.Add(new NotificationsDevicetoken
                {
                    StudentId = userId,
                    Token = dto.Token,
                    CreatedAt = System.DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(ct);
            return new OkObjectResult(new { success = true, status = "success" });
        }

        public async Task<IActionResult> ChangePasswordAsync(ChangePasswordRequest request, string userIdStr, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new BadRequestObjectResult(WebApplication1.Models.Responses.ApiResponse<object>.Fail("Old password and new password are required."));
            }

            if (!long.TryParse(userIdStr, out var userId))
            {
                return new UnauthorizedObjectResult(WebApplication1.Models.Responses.ApiResponse<object>.Fail("Invalid user."));
            }

            var adminUser = await _context.AccountsAdminusers.FindAsync(new object[] { userId }, ct);
            if (adminUser != null)
            {
                if (!WebApplication1.Utils.PasswordHasher.VerifyDjangoPassword(request.OldPassword, adminUser.Password))
                {
                    return new BadRequestObjectResult(WebApplication1.Models.Responses.ApiResponse<object>.Fail("Incorrect old password."));
                }
                adminUser.Password = WebApplication1.Utils.PasswordHasher.HashDjangoPassword(request.NewPassword);
                await _context.SaveChangesAsync(ct);
                return new OkObjectResult(WebApplication1.Models.Responses.ApiResponse<object>.Ok(null, "Password changed successfully."));
            }

            var customUser = await _context.AccountsCustomusers.FindAsync(new object[] { userId }, ct);
            if (customUser != null)
            {
                if (!WebApplication1.Utils.PasswordHasher.VerifyDjangoPassword(request.OldPassword, customUser.Password))
                {
                    return new BadRequestObjectResult(WebApplication1.Models.Responses.ApiResponse<object>.Fail("Incorrect old password."));
                }
                customUser.Password = WebApplication1.Utils.PasswordHasher.HashDjangoPassword(request.NewPassword);
                await _context.SaveChangesAsync(ct);
                return new OkObjectResult(WebApplication1.Models.Responses.ApiResponse<object>.Ok(null, "Password changed successfully."));
            }

            return new NotFoundObjectResult(WebApplication1.Models.Responses.ApiResponse<object>.Fail("User not found."));
        }
    }
}
