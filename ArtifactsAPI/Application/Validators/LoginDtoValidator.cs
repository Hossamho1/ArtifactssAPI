using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Domain.Models;
using FluentValidation;

namespace ArtifactsAPI.Application.Validators;

public class LoginDtoValidator : AbstractValidator<LoginDTOs>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.");

        RuleFor(x => x.PasswordHash)
            .NotEmpty().WithMessage("Password is required.");
    }
}