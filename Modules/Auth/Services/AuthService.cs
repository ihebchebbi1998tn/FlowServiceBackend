using MyApi.Data;
using MyApi.Modules.Auth.DTOs;
using MyApi.Modules.Auth.Models;
using MyApi.Modules.Users.Models;
using MyApi.Modules.Users.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MyApi.Modules.Auth.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto loginDto);
        Task<AuthResponseDto> UserLoginAsync(LoginRequestDto loginDto);
        Task<AuthResponseDto> SignupAsync(SignupRequestDto signupDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<AuthResponseDto> OAuthLoginAsync(string email);
        Task<AuthResponseDto> UpdateUserAsync(int userId, UpdateUserRequestDto updateDto);
        Task<AuthResponseDto> ChangePasswordAsync(int userId, ChangePasswordRequestDto changePasswordDto);
        Task<bool> LogoutAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto loginDto)
        {
            try
            {
                // First, try to find user in MainAdminUsers table
                var adminUser = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower() && u.IsActive);

                if (adminUser != null && VerifyPassword(loginDto.Password, adminUser.PasswordHash))
                {
                    var (accessToken, refreshToken, expiresAt) = GenerateTokensAsync(adminUser);

                    // Update admin user login info
                    adminUser.LastLoginAt = DateTime.UtcNow;
                    adminUser.AccessToken = accessToken;
                    adminUser.RefreshToken = refreshToken;
                    adminUser.TokenExpiresAt = expiresAt;
                    adminUser.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    return new AuthResponseDto
                    {
                        Success = true,
                        Message = "Login successful",
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        ExpiresAt = expiresAt,
                        User = MapToUserDto(adminUser)
                    };
                }

                // If not found in MainAdminUsers, try Users table
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower() && u.IsActive && !u.IsDeleted);

                if (user != null && VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    var (accessToken, refreshToken, expiresAt) = GenerateUserTokensAsync(user);

                    // Update regular user login info
                    user.LastLoginAt = DateTime.UtcNow;
                    user.AccessToken = accessToken;
                    user.RefreshToken = refreshToken;
                    user.TokenExpiresAt = expiresAt;
                    user.ModifyDate = DateTime.UtcNow;
                    user.ModifyUser = user.Email; // Self-modified

                    await _context.SaveChangesAsync();

                    return new AuthResponseDto
                    {
                        Success = true,
                        Message = "Login successful",
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        ExpiresAt = expiresAt,
                        User = MapUserToUserDto(user)
                    };
                }

                // No valid user found in either table
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during unified login for email: {Email}", loginDto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<AuthResponseDto> UserLoginAsync(LoginRequestDto loginDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower() && u.IsActive && !u.IsDeleted);

                if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                var (accessToken, refreshToken, expiresAt) = GenerateUserTokensAsync(user);

                // Update user login info
                user.LastLoginAt = DateTime.UtcNow;
                user.AccessToken = accessToken;
                user.RefreshToken = refreshToken;
                user.TokenExpiresAt = expiresAt;
                user.ModifyDate = DateTime.UtcNow;
                user.ModifyUser = user.Email; // Self-modified

                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = MapUserToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login for email: {Email}", loginDto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<AuthResponseDto> SignupAsync(SignupRequestDto signupDto)
        {
            try
            {
                _logger.LogInformation("Starting signup process for email: {Email}", signupDto.Email);
                
                // Check if email exists in MainAdminUsers table
                _logger.LogInformation("Checking if admin user exists for email: {Email}", signupDto.Email);
                var existingAdminUser = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == signupDto.Email.ToLower());

                if (existingAdminUser != null)
                {
                    _logger.LogWarning("Admin user already exists with email: {Email}", signupDto.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "A user with this email already exists"
                    };
                }

                // Check if email exists in Users table (regular users)
                _logger.LogInformation("Checking if regular user exists for email: {Email}", signupDto.Email);
                var existingRegularUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == signupDto.Email.ToLower() && !u.IsDeleted);

                if (existingRegularUser != null)
                {
                    _logger.LogWarning("Regular user already exists with email: {Email}", signupDto.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "A user with this email already exists"
                    };
                }

                // Create new admin user
                _logger.LogInformation("Hashing password for admin user: {Email}", signupDto.Email);
                var hashedPassword = signupDto.Password == "nopassword" ? "nopassword" : HashPassword(signupDto.Password);
                
                _logger.LogInformation("Creating new admin user object for email: {Email}", signupDto.Email);
                var newUser = new MainAdminUser
                {
                    Email = signupDto.Email.ToLower(),
                    PasswordHash = hashedPassword,
                    FirstName = signupDto.FirstName,
                    LastName = signupDto.LastName,
                    PhoneNumber = signupDto.PhoneNumber,
                    Country = signupDto.Country,
                    Industry = signupDto.Industry,
                    CompanyName = signupDto.CompanyName,
                    CompanyWebsite = signupDto.CompanyWebsite,
                    Preferences = signupDto.Preferences,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _logger.LogInformation("Adding admin user to database context for email: {Email}", signupDto.Email);
                _context.MainAdminUsers.Add(newUser);
                
                _logger.LogInformation("Saving changes to database for email: {Email}", signupDto.Email);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generating tokens for new admin user: {Email}", signupDto.Email);
                var (accessToken, refreshToken, expiresAt) = GenerateTokensAsync(newUser);

                // Update user with tokens
                _logger.LogInformation("Updating admin user with tokens for email: {Email}", signupDto.Email);
                newUser.AccessToken = accessToken;
                newUser.RefreshToken = refreshToken;
                newUser.TokenExpiresAt = expiresAt;
                newUser.LastLoginAt = DateTime.UtcNow;

                _logger.LogInformation("Saving token updates to database for email: {Email}", signupDto.Email);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin user registration completed successfully for email: {Email}", signupDto.Email);
                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Account created successfully",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = MapToUserDto(newUser)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signup for email: {Email}. Exception: {Exception}", signupDto.Email, ex.ToString());
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during account creation"
                };
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.IsActive);

                if (user == null || user.TokenExpiresAt < DateTime.UtcNow)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid or expired refresh token"
                    };
                }

                var (newAccessToken, newRefreshToken, expiresAt) = GenerateTokensAsync(user);

                user.AccessToken = newAccessToken;
                user.RefreshToken = newRefreshToken;
                user.TokenExpiresAt = expiresAt;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = expiresAt,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during token refresh"
                };
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                return user != null ? MapToUserDto(user) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<AuthResponseDto> UpdateUserAsync(int userId, UpdateUserRequestDto updateDto)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Update user properties
                if (!string.IsNullOrEmpty(updateDto.FirstName))
                    user.FirstName = updateDto.FirstName;
                if (!string.IsNullOrEmpty(updateDto.LastName))
                    user.LastName = updateDto.LastName;
                if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                    user.PhoneNumber = updateDto.PhoneNumber;
                if (!string.IsNullOrEmpty(updateDto.Country))
                    user.Country = updateDto.Country;
                if (!string.IsNullOrEmpty(updateDto.Industry))
                    user.Industry = updateDto.Industry;
                if (!string.IsNullOrEmpty(updateDto.CompanyName))
                    user.CompanyName = updateDto.CompanyName;
                if (!string.IsNullOrEmpty(updateDto.CompanyWebsite))
                    user.CompanyWebsite = updateDto.CompanyWebsite;
                if (!string.IsNullOrEmpty(updateDto.Preferences))
                    user.Preferences = updateDto.Preferences;
                if (updateDto.OnboardingCompleted.HasValue)
                    user.OnboardingCompleted = updateDto.OnboardingCompleted.Value;

                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "User updated successfully",
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userId);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during user update"
                };
            }
        }

        public async Task<AuthResponseDto> ChangePasswordAsync(int userId, ChangePasswordRequestDto changePasswordDto)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Verify current password
                if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Current password is incorrect"
                    };
                }

                // Hash new password
                user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Password changed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred while changing password"
                };
            }
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user != null)
                {
                    user.AccessToken = null;
                    user.RefreshToken = null;
                    user.TokenExpiresAt = null;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
                return false;
            }
        }

        private (string accessToken, string refreshToken, DateTime expiresAt) GenerateTokensAsync(MainAdminUser user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyHere12345";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "FlowServiceBackend";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "FlowServiceFrontend";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim("UserId", user.Id.ToString()),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim("Industry", user.Industry)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(24); // 24 hours expiry

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            return (accessToken, refreshToken, expiresAt);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            // Handle OAuth users with default password
            if (hashedPassword == "nopassword" && password == "nopassword")
            {
                return true;
            }
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);

                return user != null ? MapToUserDto(user) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return null;
            }
        }

        public async Task<AuthResponseDto> OAuthLoginAsync(string email)
        {
            try
            {
                var user = await _context.MainAdminUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);

                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User not found. Please complete signup."
                    };
                }

                var (accessToken, refreshToken, expiresAt) = GenerateTokensAsync(user);

                // Update user login info
                user.LastLoginAt = DateTime.UtcNow;
                user.AccessToken = accessToken;
                user.RefreshToken = refreshToken;
                user.TokenExpiresAt = expiresAt;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "OAuth login successful",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OAuth login for email: {Email}", email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during OAuth login"
                };
            }
        }

        private UserDto MapToUserDto(MainAdminUser user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country,
                Industry = user.Industry,
                CompanyName = user.CompanyName,
                CompanyWebsite = user.CompanyWebsite,
                Preferences = user.Preferences,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                OnboardingCompleted = user.OnboardingCompleted
            };
        }

        private UserDto MapUserToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country,
                Industry = user.Role ?? "User", // Use Role as Industry for regular users
                CompanyName = "", // Regular users don't have company info
                CompanyWebsite = "",
                Preferences = "",
                CreatedAt = user.CreatedDate,
                LastLoginAt = user.LastLoginAt,
                OnboardingCompleted = true // Regular users bypass onboarding
            };
        }

        private (string accessToken, string refreshToken, DateTime expiresAt) GenerateUserTokensAsync(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyHere12345";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "FlowServiceBackend";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "FlowServiceFrontend";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim("UserId", user.Id.ToString()),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim("Role", user.Role ?? "User"),
                new Claim("UserType", "RegularUser") // Distinguish from MainAdminUser
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(24); // 24 hours expiry

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            return (accessToken, refreshToken, expiresAt);
        }
    }
}