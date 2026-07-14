
using WebApplication1.Controllers;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Threading.Tasks;
using System.Net;
using System.Security.Claims;
using WebApplication1.Models.DTOs.Auth;
using Microsoft.Extensions.Caching.Memory;

namespace WebApplication1.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly Microsoft.Extensions.Logging.ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;

        public AuthService(ApplicationDbContext context, IConfiguration config, Microsoft.Extensions.Logging.ILogger<AuthService> logger, IEmailService emailService, IServiceScopeFactory scopeFactory, IMemoryCache cache)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _emailService = emailService;
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        public async Task<ServiceResult<object>> CheckAvailabilityAsync(CheckAvailabilityRequest request, CancellationToken ct = default)
        {
            var errors = new Dictionary<string, string[]>();
            if (!string.IsNullOrWhiteSpace(request.Email) && await _context.AccountsCustomusers.AnyAsync(u => EF.Functions.ILike(u.Email, request.Email.Trim()), ct))
            {
                errors["email"] = new[] { "Email already exists." };
            }
            if (!string.IsNullOrWhiteSpace(request.Mobile) && await _context.AccountsCustomusers.AnyAsync(u => u.Mobile == request.Mobile.Trim(), ct))
            {
                errors["mobile"] = new[] { "Mobile number already exists." };
            }
            
            if (errors.Any())
            {
                return ServiceResult<object>.Fail("Validation failed", errors);
            }
            
            var appConfig = await _context.LibraryAppconfigs.OrderBy(a => a.Id).FirstOrDefaultAsync(ct);
            bool requireOtp = appConfig?.EnableWhatsappService ?? false;

            return ServiceResult<object>.Ok(new { require_otp = requireOtp }, "Available");
        }

        public async Task<ServiceResult<object>> RegisterAsync(UserRegisterRequest request, string ipAddress, CancellationToken ct = default)
        {
            // Trim inputs
            request.FirstName = request.FirstName?.Trim();
            request.LastName = request.LastName?.Trim();
            request.Email = request.Email?.Trim();
            request.Mobile = request.Mobile?.Trim();
            request.Goal = request.Goal?.Trim();
            request.Address = request.Address?.Trim();
            request.ParentMobile = request.ParentMobile?.Trim();

            // Validate required fields
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(request.FirstName)) errors["first_name"] = new[] { "First name is required." };
            if (string.IsNullOrWhiteSpace(request.LastName)) errors["last_name"] = new[] { "Last name is required." };
            if (string.IsNullOrWhiteSpace(request.Email)) {
                errors["email"] = new[] { "Email is required." };
            } else if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$")) {
                errors["email"] = new[] { "Please enter a valid email address." };
            }

            if (string.IsNullOrWhiteSpace(request.Mobile) || !System.Text.RegularExpressions.Regex.IsMatch(request.Mobile, @"^[0-9]{10}$"))
            {
                errors["mobile"] = new[] { "Enter a valid 10-digit mobile number." };
            }

            var appConfig = await _context.LibraryAppconfigs.OrderBy(a => a.Id).FirstOrDefaultAsync(ct);
            bool requireOtp = appConfig?.EnableWhatsappService ?? false;

            if (requireOtp)
            {
                if (string.IsNullOrWhiteSpace(request.Otp))
                {
                    errors["otp"] = new[] { "OTP is required for verification." };
                }
                else
                {
                    if (!_cache.TryGetValue($"reg_otp_{request.Mobile}", out string expectedOtp) || expectedOtp != request.Otp)
                    {
                        errors["otp"] = new[] { "Invalid or expired OTP." };
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(request.Password)) errors["password"] = new[] { "Password is required." };
            if (request.Dob == default) errors["dob"] = new[] { "Date of birth is required." };

            if (errors.Any())
            {
                return ServiceResult<object>.Fail("Validation failed.", errors);
            }

            if (request.Password != request.ConfirmPassword)
            {
                return ServiceResult<object>.Fail("Passwords do not match.", new Dictionary<string, string[]> { { "password", new[] { "Passwords do not match." } } });
            }
            if (await _context.AccountsCustomusers.AnyAsync(u => u.Mobile == request.Mobile, ct))
            {
                return ServiceResult<object>.Fail("Validation failed", new Dictionary<string, string[]> { { "mobile", new[] { "Mobile number already exists." } } });
            }
            if (await _context.AccountsCustomusers.AnyAsync(u => EF.Functions.ILike(u.Email, request.Email), ct))
            {
                return ServiceResult<object>.Fail("Validation failed", new Dictionary<string, string[]> { { "email", new[] { "Email already exists." } } });
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                var isRelational = _context.Database.IsRelational();
                var transaction = isRelational ? await _context.Database.BeginTransactionAsync(ct) : null;
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
                        Address = request.Address ?? "",
                        ParentMobile = request.ParentMobile ?? "",
                        Gender = request.Gender ?? "Other",
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
                    if (transaction != null) await transaction.CommitAsync(ct);
                    
                    _ = Task.Run(async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        
                        var emailTask = Task.Run(async () => 
                        {
                            try 
                            {
                                var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                                await emailSvc.SendWelcomeEmailAsync(user.Email, user.FirstName, user.LastName);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error sending welcome email: {ex}");
                            }
                        });

                        var waTask = Task.Run(async () => 
                        {
                            if (!string.IsNullOrEmpty(user.Mobile))
                            {
                                try
                                {
                                    string msg = $"Congratulations {user.FirstName}! You have successfully registered with Shresht Library. Your Student ID is {nextStudentId}. Welcome aboard!";
                                    var whatsapp = scope.ServiceProvider.GetRequiredService<WhatsAppNotificationService>();
                                    await whatsapp.SendTextMessageAsync(user.Mobile, msg);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error sending WhatsApp welcome message: {ex}");
                                }
                            }
                        });
                        
                        await Task.WhenAll(emailTask, waTask);
                    });

                    _cache.Remove($"reg_otp_{request.Mobile}"); // clear otp after success

                    return await GenerateStudentTokensAndLogAsync(user, "Registered new account", ipAddress, "", "", ct);
                }
                catch (System.Exception)
                {
                    if (transaction != null) await transaction.RollbackAsync(ct);
                    return ServiceResult<object>.Fail("Internal server error during registration.", new Dictionary<string, string[]> { { "non_field_errors", new[] { "Internal server error during registration." } } });
                }
            });
        }

        public async Task<ServiceResult<object>> SendRegisterOtpAsync(SendOtpRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Mobile) || !System.Text.RegularExpressions.Regex.IsMatch(request.Mobile, @"^[0-9]{10}$"))
            {
                return ServiceResult<object>.Fail("Validation failed.", new Dictionary<string, string[]> { { "mobile", new[] { "Enter a valid 10-digit mobile number." } } });
            }

            if (await _context.AccountsCustomusers.AnyAsync(u => u.Mobile == request.Mobile, ct))
            {
                return ServiceResult<object>.Fail("Validation failed.", new Dictionary<string, string[]> { { "mobile", new[] { "Mobile number already exists." } } });
            }

            string rawOtp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
            _cache.Set($"reg_otp_{request.Mobile}", rawOtp, TimeSpan.FromMinutes(10));

            try 
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsapp = scope.ServiceProvider.GetRequiredService<WhatsAppNotificationService>();
                string msg = $"Your Shresht Library registration code is {rawOtp}. It will expire in 10 minutes.";
                await whatsapp.SendTextMessageAsync(request.Mobile, msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending registration OTP WhatsApp: {ex}");
            }

            return ServiceResult<object>.Ok(null, "Registration OTP sent successfully to your WhatsApp.");
        }

        public Task<ServiceResult<object>> VerifyRegisterOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Mobile) || string.IsNullOrWhiteSpace(request.Otp))
            {
                return Task.FromResult(ServiceResult<object>.Fail("Validation failed.", new Dictionary<string, string[]> { { "mobile", new[] { "Mobile and OTP are required." } } }));
            }

            if (!_cache.TryGetValue($"reg_otp_{request.Mobile}", out string? cachedOtp) || cachedOtp != request.Otp)
            {
                return Task.FromResult(ServiceResult<object>.Fail("Invalid OTP.", new Dictionary<string, string[]> { { "otp", new[] { "Invalid OTP or OTP has expired." } } }));
            }

            // We do not remove the OTP here, because it needs to be verified again on final registration submit
            return Task.FromResult(ServiceResult<object>.Ok(new { verified = true }, "OTP verified successfully."));
        }

        public async Task<ServiceResult<object>> SendOtpAsync(SendOtpRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Mobile))
            {
                return ServiceResult<object>.Fail("Validation failed.", new Dictionary<string, string[]> { { "mobile", new[] { "This field is required." } } });
            }

            var user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Mobile == request.Mobile, ct);
            if (user == null)
            {
                return ServiceResult<object>.NotFound("Mobile number not registered.");
            }

            string rawOtp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
            user.Otp = WebApplication1.Utils.PasswordHasher.HashDjangoPassword(rawOtp);
            user.OtpExpiry = System.DateTime.UtcNow.AddSeconds(45);
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
            // OTP must never be logged.
            
            if (!string.IsNullOrEmpty(user.Email))
            {
                _ = Task.Run(async () =>
                {
                    try {
                        using var scope = _scopeFactory.CreateScope();
                        var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        await emailSvc.SendOtpEmailAsync(user.Email, user.FirstName ?? "Student", rawOtp);
                    } catch (Exception ex) { 
                        Console.WriteLine($"Error sending OTP email: {ex}");
                    }
                });
            }

            if (!string.IsNullOrEmpty(user.Mobile))
            {
                try {
                    using var scope = _scopeFactory.CreateScope();
                    var whatsapp = scope.ServiceProvider.GetRequiredService<WhatsAppNotificationService>();
                    string msg = $"Your Shresht Library verification code is {rawOtp}. It will expire in 5 minutes.";
                    await whatsapp.SendTextMessageAsync(user.Mobile, msg);
                } catch (Exception ex) { 
                    Console.WriteLine($"Error sending OTP WhatsApp: {ex}");
                }
            }

            return ServiceResult<object>.Ok(null, "OTP sent successfully.");
        }

        public async Task<ServiceResult<object>> VerifyOtpAsync(VerifyOtpRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Mobile) || string.IsNullOrWhiteSpace(request.Otp))
            {
                return ServiceResult<object>.Fail("Validation failed.", new Dictionary<string, string[]> { { "mobile", new[] { "This field is required." } }, { "otp", new[] { "This field is required." } } });
            }

            var user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Mobile == request.Mobile, ct);
            if (user == null)
            {
                return ServiceResult<object>.NotFound("Mobile number not registered.");
            }

            if (user.OtpAttempts >= 5)
            {
                return ServiceResult<object>.Fail("Too many failed attempts. Please request a new OTP.");
            }

            if (user.OtpExpiry < System.DateTime.UtcNow)
            {
                return ServiceResult<object>.Fail("OTP has expired.", new Dictionary<string, string[]> { { "otp", new[] { "OTP has expired." } } });
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

                return ServiceResult<object>.Fail("Invalid OTP.", new Dictionary<string, string[]> { { "otp", new[] { "Invalid OTP." } } });
            }
        }

        public async Task<ServiceResult<object>> LoginEmailAsync(LoginEmailRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            request.Email = request.Email?.Trim();
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ServiceResult<object>.Fail("Validation failed.", new Dictionary<string, string[]> { { "email", new[] { "This field is required." } }, { "password", new[] { "This field is required." } } });
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
                return ServiceResult<object>.Fail("Invalid credentials or not a student.", new Dictionary<string, string[]> { { "non_field_errors", new[] { "Invalid credentials or not a student." } } });
            }

            return await GenerateStudentTokensAndLogAsync(user, "Logged in via Email/Password", ipAddress, path, method, ct);
        }

        public async Task<ServiceResult<object>> LoginMobileAsync(LoginMobileRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            request.Mobile = request.Mobile?.Trim();
            if (string.IsNullOrWhiteSpace(request.Mobile) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ServiceResult<object>.Fail("Validation failed.", new Dictionary<string, string[]> { { "mobile", new[] { "This field is required." } }, { "password", new[] { "This field is required." } } });
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
                return ServiceResult<object>.Fail("Invalid credentials or not a student.", new Dictionary<string, string[]> { { "non_field_errors", new[] { "Invalid credentials or not a student." } } });
            }

            return await GenerateStudentTokensAndLogAsync(user, "Logged in via Mobile/Password", ipAddress, path, method, ct);
        }

        private async Task<ServiceResult<object>> GenerateStudentTokensAndLogAsync(AccountsCustomuser user, string actionDesc, string ipAddress, string path, string method, CancellationToken ct)
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

            return ServiceResult<object>.Ok(new
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
            }, "Login successful.");
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

        public async Task<ServiceResult<object>> AdminLoginAsync(AdminLoginRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            // Credential logging removed - security violation
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
                _logger.LogWarning("Admin login failed: password verification failed or user invalid/inactive.");
                return ServiceResult<object>.Fail("Invalid credentials or not an admin.", new Dictionary<string, string[]> { { "non_field_errors", new[] { "Invalid credentials or not an admin." } } });
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
                    new System.Security.Claims.Claim("permissions", user.Permissions ?? "[]")
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

            return ServiceResult<object>.Ok(new { tokens = new { access = accessToken, refresh = refreshToken }, user = new { id = user.Id, username = user.Username, first_name = user.FirstName, last_name = user.LastName, email = user.Email, mobile = user.Mobile, role = user.Role, permissions = ParsePermissions(user.Permissions), profile_image = user.ProfileImage != null ? $"/media/{user.ProfileImage}" : null, is_active = user.IsActive, date_joined = user.DateJoined.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"), last_login = user.LastLogin?.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") } }, "Admin login successful.");
        }

        public async Task<ServiceResult<object>> ForgotPasswordAsync(ForgotPasswordRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Identifier))
            {
                return ServiceResult<object>.Fail("Validation failed.", new Dictionary<string, string[]> { { "identifier", new[] { "This field is required." } } });
            }

            bool isEmail = request.Identifier.Contains("@");
            var user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => 
                (isEmail && u.Email == request.Identifier) || (!isEmail && u.Mobile == request.Identifier), ct);

            if (user != null)
            {
                string rawToken = new System.Random().Next(100000, 999999).ToString();
                
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                string hashedToken = System.Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawToken))).ToLower();
                user.Otp = hashedToken;
                user.OtpExpiry = System.DateTime.UtcNow.AddSeconds(45);
                await _context.SaveChangesAsync(ct);

                var ip = ipAddress;
                _context.CoreActivitylogs.Add(new CoreActivitylog
                {
                    Action = "Requested password reset link/OTP",
                    Timestamp = System.DateTime.UtcNow,
                    IpAddress = System.Net.IPAddress.TryParse(ip, out var parsedIp) ? parsedIp : null,
                    Details = $"{{\"path\": \"{path}\", \"method\": \"{method}\", \"type\": \"{(isEmail ? "email" : "mobile")}\"}}",
                    UserId = user.Id
                });
                await _context.SaveChangesAsync(ct);

                if (isEmail)
                {
                    try {
                        using var scope = _scopeFactory.CreateScope();
                        var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        await emailSvc.SendOtpEmailAsync(user.Email ?? "", user.FirstName ?? "Student", rawToken);
                        return ServiceResult<object>.Ok(null, "Password reset OTP sent to your email.");
                    } catch (Exception ex) { 
                        Console.WriteLine($"Error sending forgot password OTP email: {ex}");
                        return ServiceResult<object>.Fail($"Failed to send email: {ex.Message}");
                    }
                }
                else
                {
                    string msg = $"Your Shresht Library password reset OTP is {rawToken}. It is valid for 15 minutes. Do not share this with anyone.";
                    _ = Task.Run(async () =>
                    {
                        try 
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var whatsapp = scope.ServiceProvider.GetRequiredService<WhatsAppNotificationService>();
                            await whatsapp.SendTextMessageAsync(user.Mobile, msg);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending password reset OTP via WhatsApp: {ex}");
                        }
                    });
                    return ServiceResult<object>.Ok(null, "Password reset OTP sent to your WhatsApp number.");
                }
            }

            return ServiceResult<object>.NotFound("User not found.");
        }

        public async Task<ServiceResult<object>> VerifyForgotPasswordOtpAsync(VerifyResetOtpRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Token))
            {
                return ServiceResult<object>.Fail("Validation failed.", new Dictionary<string, string[]> { { "identifier", new[] { "This field is required." } }, { "token", new[] { "This field is required." } } });
            }

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            string hashedToken = System.Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Token))).ToLower();
            
            bool isEmail = request.Identifier.Contains("@");
            var user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => 
                (isEmail ? u.Email == request.Identifier : u.Mobile == request.Identifier) 
                && u.Otp == hashedToken && u.OtpExpiry > System.DateTime.UtcNow, ct);

            if (user != null)
            {
                // Extend OTP expiry to 15 minutes to allow them time to change password without a strict limit
                user.OtpExpiry = System.DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync(ct);
                return ServiceResult<object>.Ok(null, "OTP verified successfully.");
            }

            return ServiceResult<object>.Fail("Invalid or expired OTP.", new Dictionary<string, string[]> { { "token", new[] { "Invalid or expired OTP." } } });
        }

        public async Task<ServiceResult<object>> ResetPasswordAsync(ResetPasswordRequest request, string ipAddress, string path, string method, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return ServiceResult<object>.Fail("Validation failed.", new Dictionary<string, string[]> { { "token", new[] { "This field is required." } }, { "new_password", new[] { "This field is required." } } });
            }

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            string hashedToken = System.Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Token))).ToLower();
            
            Models.AccountsCustomuser user = null;
            if (!string.IsNullOrWhiteSpace(request.Identifier))
            {
                bool isEmail = request.Identifier.Contains("@");
                user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => 
                    (isEmail ? u.Email == request.Identifier : u.Mobile == request.Identifier) 
                    && u.Otp == hashedToken && u.OtpExpiry > System.DateTime.UtcNow, ct);
            }
            else
            {
                // Fallback for older apps that only send token
                user = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Otp == hashedToken && u.OtpExpiry > System.DateTime.UtcNow, ct);
            }

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

                return ServiceResult<object>.Ok(null, "Password reset successfully.");
            }

            return ServiceResult<object>.Fail("Invalid or expired reset token.", new Dictionary<string, string[]> { { "token", new[] { "Invalid or expired reset token." } } });
        }

        
        public async Task<ServiceResult<object>> LogoutAsync(LogoutRequest request, string authHeader, string currentUserIdStr, string role, string ipAddress, string path, string method, CancellationToken ct = default)
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
                    _logger.LogWarning(ex, "Token decode error for refresh token during logout.");
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
                    _logger.LogWarning(ex, "Token decode error for access token during logout.");
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

            return ServiceResult<object>.Ok(null, "Logged out successfully.");
        }

        public async Task<ServiceResult<object>> RefreshTokenAsync(TokenRefreshRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Refresh))
            {
                return ServiceResult<object>.Fail("Refresh token is required.", new Dictionary<string, string[]> { { "refresh", new[] { "Refresh token is required." } } });
            }

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            try
            {
                var token = handler.ReadJwtToken(request.Refresh);
                var tokenType = token.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;
                if (tokenType != "refresh")
                {
                    return ServiceResult<object>.Fail("Invalid refresh token.", new Dictionary<string, string[]> { { "detail", new[] { "Invalid refresh token." } } });
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
                        return ServiceResult<object>.Fail("Refresh token has been revoked.", new Dictionary<string, string[]> { { "detail", new[] { "Refresh token has been revoked." } } });
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
                    return ServiceResult<object>.Fail("User account is inactive.", new Dictionary<string, string[]> { { "detail", new[] { "User account is inactive." } } });
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

                return ServiceResult<object>.Ok(new
                {
                    access = newAccessToken
                });
            }
            catch
            {
                return ServiceResult<object>.Fail("Invalid refresh token.", new Dictionary<string, string[]> { { "detail", new[] { "Invalid refresh token." } } });
            }
        }
        public async Task<ServiceResult<object>> UpdateFcmTokenAsync(FcmTokenUpdateDto dto, long userId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(dto?.Token))
            {
                return ServiceResult<object>.Fail("Token is required.");
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
            return ServiceResult<object>.Ok(null, "Success");
        }

        public async Task<ServiceResult<object>> ChangePasswordAsync(ChangePasswordRequest request, string userIdStr, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return ServiceResult<object>.Fail("Old password and new password are required.");
            }

            if (!long.TryParse(userIdStr, out var userId))
            {
                return ServiceResult<object>.Fail("Invalid user.");
            }

            var adminUser = await _context.AccountsAdminusers.FindAsync(new object[] { userId }, ct);
            if (adminUser != null)
            {
                if (!WebApplication1.Utils.PasswordHasher.VerifyDjangoPassword(request.OldPassword, adminUser.Password))
                {
                    return ServiceResult<object>.Fail("Incorrect old password.");
                }
                adminUser.Password = WebApplication1.Utils.PasswordHasher.HashDjangoPassword(request.NewPassword);
                await _context.SaveChangesAsync(ct);
                return ServiceResult<object>.Ok(null, "Password changed successfully.");
            }

            var customUser = await _context.AccountsCustomusers.FindAsync(new object[] { userId }, ct);
            if (customUser != null)
            {
                if (!WebApplication1.Utils.PasswordHasher.VerifyDjangoPassword(request.OldPassword, customUser.Password))
                {
                    return ServiceResult<object>.Fail("Incorrect old password.");
                }
                customUser.Password = WebApplication1.Utils.PasswordHasher.HashDjangoPassword(request.NewPassword);
                await _context.SaveChangesAsync(ct);
                return ServiceResult<object>.Ok(null, "Password changed successfully.");
            }

            return ServiceResult<object>.NotFound("User not found.");
        }
    }
}
