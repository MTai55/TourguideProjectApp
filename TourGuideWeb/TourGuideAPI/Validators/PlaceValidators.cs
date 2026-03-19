using FluentValidation;
using TourGuideAPI.DTOs.Places;

namespace TourGuideAPI.Validators;

public class CreatePlaceDtoValidator : AbstractValidator<CreatePlaceDto>
{
    public CreatePlaceDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên địa điểm không được để trống")
            .MaximumLength(200).WithMessage("Tên tối đa 200 ký tự");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Địa chỉ không được để trống")
            .MaximumLength(500);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Vĩ độ phải từ -90 đến 90");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Kinh độ phải từ -180 đến 180");

        RuleFor(x => x.PriceMin)
            .GreaterThanOrEqualTo(0).WithMessage("Giá tối thiểu không âm")
            .When(x => x.PriceMin.HasValue);

        RuleFor(x => x.PriceMax)
            .GreaterThanOrEqualTo(x => x.PriceMin ?? 0)
            .WithMessage("Giá tối đa phải lớn hơn giá tối thiểu")
            .When(x => x.PriceMax.HasValue && x.PriceMin.HasValue);
    }
}