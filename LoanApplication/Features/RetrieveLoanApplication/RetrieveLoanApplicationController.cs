using LoanApplication.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Features.RetrieveLoanApplication;

[ApiController]
[Route("loan-applications")]
public class RetrieveLoanApplicationController(LoanContext loanContext) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public IActionResult Retrieve(Guid id)
    {
        var application = loanContext.LoanApplications
            .Include(la => la.DecisionLogEntries)
            .AsNoTracking()
            .FirstOrDefault(la => la.Id == id);

        if (application is null)
            return NotFound();

        var decisionLog = application.DecisionLogEntries
            .OrderBy(d => d.EvaluatedAt)
            .Select(d => new DecisionLogEntryDetails(d.RuleName, d.Passed, d.Message, d.EvaluatedAt))
            .ToList();

        return Ok(new LoanApplicationDetails(
            application.Id,
            application.Name,
            application.Email,
            application.MonthlyIncome,
            application.RequestedAmount,
            application.TermMonths,
            application.Status,
            application.CreatedAt,
            application.ReviewedAt,
            decisionLog));
    }
}
