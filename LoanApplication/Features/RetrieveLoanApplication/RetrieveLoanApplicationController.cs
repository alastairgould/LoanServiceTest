using LoanApplication.Features.ApplyForLoan;
using Microsoft.AspNetCore.Mvc;

namespace LoanApplication.Features.RetrieveLoanApplication;

[ApiController]
[Route("loan-applications")]
public class RetrieveLoanApplicationController(LoanContext loanContext) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public IActionResult Retrieve(Guid id)
    {
        var application = loanContext.LoanApplications.Find(id);
        
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
