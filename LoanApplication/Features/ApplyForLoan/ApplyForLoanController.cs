using Microsoft.AspNetCore.Mvc;

namespace LoanApplication.Features.ApplyForLoan;

[ApiController]
[Route("loan-applications")]
public class ApplyForLoanController : ControllerBase
{
    private readonly LoanContext _loanContext;
    private readonly TimeProvider _timeProvider;
    private readonly LoanApplicationRequestValidator _validator;

    public ApplyForLoanController(
        LoanContext loanContext,
        TimeProvider timeProvider,
        LoanApplicationRequestValidator validator)
    {
        _loanContext = loanContext;
        _timeProvider = timeProvider;
        _validator = validator;
    }

    [HttpPost]
    public IActionResult Apply([FromBody] LoanApplicationRequest request)
    {
        var errors = _validator.Validate(request);
        
        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors));

        var id = Guid.NewGuid();
        var createdAt = _timeProvider.GetUtcNow().UtcDateTime;

        _loanContext.LoanApplications.Add(new LoanApplication(
            id,
            request.Name,
            request.Email,
            (int)request.MonthlyIncome,
            request.Amount,
            request.TermMonths,
            "Pending",
            createdAt,
            null));

        _loanContext.SaveChanges();
        return Ok(new LoanApplicationResult(id, LoanStatus.Pending, createdAt));
    }
}
