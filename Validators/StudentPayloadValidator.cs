using FluentValidation;
using WebApplication1.Controllers;

namespace WebApplication1.Validators
{
    public class StudentPayloadValidator : AbstractValidator<AdminStudentsController.StudentPayload>
    {
        public StudentPayloadValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

            RuleFor(x => x.LastName)
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("A valid email is required.")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Mobile)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("A valid mobile number is required.")
                .When(x => !string.IsNullOrEmpty(x.Mobile));

            RuleFor(x => x.Gender)
                .Must(x => new[] { "Male", "Female", "Other" }.Contains(x))
                .WithMessage("Gender must be Male, Female, or Other.")
                .When(x => !string.IsNullOrEmpty(x.Gender));
        }
    }
}
