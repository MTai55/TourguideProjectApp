using FluentValidation;
using TourGuideAPI.DTOs.Auth;

namespace TourGuideAPI.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống")
            .MaximumLength(150).WithMessage("Họ tên tối đa 150 ký tự");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không đúng định dạng")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống")
            .MinimumLength(6).WithMessage("Mật khẩu tối thiểu 6 ký tự")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .Matches(@"^[0-9]{10,11}$").WithMessage("Số điện thoại không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Role)
            .Must(r => r is "User" or "Owner")
            .WithMessage("Role phải là User hoặc Owner");
    }
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không đúng định dạng");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống");
    }
}