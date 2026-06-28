using FluentValidation;
using WebApplication1.Controllers;

namespace WebApplication1.Validators
{
    public class SeatCreateDtoValidator : AbstractValidator<AdminSeatsController.SeatCreateDto>
    {
        public SeatCreateDtoValidator()
        {
            RuleFor(x => x.Floor)
                .NotEmpty().WithMessage("Floor is required.");

            RuleFor(x => x.Row)
                .NotEmpty().WithMessage("Row is required.");

            RuleFor(x => x.SeatNumber)
                .NotEmpty().WithMessage("Seat number is required.");
        }
    }
}
