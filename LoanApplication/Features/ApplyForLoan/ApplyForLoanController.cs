using LoanApplication.Domain;
using Microsoft.AspNetCore.Mvc;

namespace LoanApplication.Features.ApplyForLoan;

[ApiController]
[Route("loan-applications")]
public class ApplyForLoanController(
    LoanContext loanContext,
    TimeProvider timeProvider,
    LoanApplicationRequestValidator validator)
    : ControllerBase
{
    [HttpPost]
    public IActionResult Apply([FromBody] LoanApplicationRequest request)
    {
        var errors = validator.Validate(request);

        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors));

        var loan = new Domain.LoanApplication(
            Guid.NewGuid(),
            request.Name,
            request.Email,
            request.MonthlyIncome,
            request.RequestedAmount,
            request.TermMonths,
            LoanStatus.Pending,
            timeProvider.GetUtcNow().UtcDateTime,
            null);

        loanContext.LoanApplications.Add(loan);
        loanContext.SaveChanges();
        var result = new LoanApplicationResult(loan.Id, loan.Status, loan.CreatedAt);
        return Created($"/loan-applications/{loan.Id}", result);
    }
}
