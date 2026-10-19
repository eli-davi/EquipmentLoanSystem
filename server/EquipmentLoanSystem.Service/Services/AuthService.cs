using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EquipmentLoanSystem.Core.Constants;
using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Interfaces;
using EquipmentLoanSystem.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EquipmentLoanSystem.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IMemberRepository memberRepository,
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IOptions<JwtSettings> jwtOptions,
            ILogger<AuthService> logger)
        {
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtSettings = jwtOptions.Value;
            _logger = logger;
        }

        public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            if (await _memberRepository.EmailExistsAsync(email, cancellationToken))
                return Result<AuthResponseDto>.Conflict("כתובת האימייל כבר רשומה במערכת");

            var member = new Member
            {
                Email = email,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Role = Roles.User, // הרשמה עצמית תמיד כ-User
                PasswordHash = _passwordHasher.Hash(dto.Password)
            };

            await _memberRepository.AddAsync(member, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // לא כותבים ללוג סיסמה, token או תוכן בקשת ההרשמה - רק מזהה
            _logger.LogInformation("New member registered: {MemberId}", member.Id);
            return Result<AuthResponseDto>.Success(BuildResponse(member));
        }

        public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            var member = await _memberRepository.GetByEmailAsync(dto.Email, cancellationToken);

            if (member is null || !_passwordHasher.Verify(dto.Password, member.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt");
                return Result<AuthResponseDto>.Unauthorized("אימייל או סיסמה שגויים");
            }

            _logger.LogInformation("Member {MemberId} logged in", member.Id);
            return Result<AuthResponseDto>.Success(BuildResponse(member));
        }

        private AuthResponseDto BuildResponse(Member member)
        {
            var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes);

            var claims = new List<Claim>
            {
                new(AppClaimTypes.MemberId, member.Id.ToString()),
                new(AppClaimTypes.Email, member.Email),
                new(AppClaimTypes.Name, $"{member.FirstName} {member.LastName}"),
                new(AppClaimTypes.Role, member.Role),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                MemberId = member.Id,
                Email = member.Email,
                FullName = $"{member.FirstName} {member.LastName}",
                Role = member.Role,
                ExpiresAt = expires
            };
        }
    }
}
