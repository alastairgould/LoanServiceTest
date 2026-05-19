using LoanApplication.Features.ApplyForLoan;
using Microsoft.AspNetCore.Mvc;

namespace LoanApplication.Features.RetrieveLoanApplication;

[ApiController]
[Route("loan-applications")]
public class RetrieveLoanApplicationController : ControllerBase
{
    private readonly LoanContext _loanContext;

    public RetrieveLoanApplicationController(LoanContext loanContext)
    {
        _loanContext = loanContext;
    }

    [HttpGet("{id:guid}")]
    public IActionResult Retrieve(Guid id)
    {
        var application = _loanContext.LoanApplications.Find(id);
        
        if (application is null)
            return NotFound();

        return Ok(new LoanApplicationDetails(
            application.Id,
            application.Name,
            application.Email,
            application.MonthlyIncome,
            application.RequestedAmount,
            application.TermMonths,
            application.Status,
            application.CreatedAt,
            application.ReviewedAt));
    }
}
