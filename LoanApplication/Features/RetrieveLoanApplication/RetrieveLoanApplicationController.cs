using LoanApplication.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Features.RetrieveLoanApplication;

[ApiController]
[Route("loan-applications")]
public class RetrieveLoanApplicationController(LoanContext loanContext) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Retrieve(Guid id, CancellationToken cancellationToken)
    {
        var application = await loanContext.LoanApplications
            .Include(la => la.DecisionLogEntries)
            .AsNoTracking()
            .FirstOrDefaultAsync(la => la.Id == id, cancellationToken);

        if (application is null)
        {
            return NotFound();
        }

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
