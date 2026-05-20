using System.Net.Mail;

namespace LoanApplication.Features.ApplyForLoan;

public class LoanApplicationRequestValidator
{
    public Dictionary<string, string[]> Validate(LoanApplicationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors[nameof(request.Name)] = ["Name is required."];
        if (string.IsNullOrWhiteSpace(request.Email) || !MailAddress.TryCreate(request.Email, out _))
            errors[nameof(request.Email)] = ["A valid email is required."];
        if (request.RequestedAmount <= 0)
            errors[nameof(request.RequestedAmount)] = ["Requested amount must be greater than zero."];
        if (request.MonthlyIncome <= 0)
            errors[nameof(request.MonthlyIncome)] = ["Monthly income must be greater than zero."];
        if (request.TermMonths <= 0)
            errors[nameof(request.TermMonths)] = ["Term months must be greater than zero."];

        return errors;
    }
}
