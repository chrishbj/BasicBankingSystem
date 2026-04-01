using Banking.Bff.CustomerPortal.Auth;
using Banking.Bff.CustomerPortal.Clients;
using Banking.Bff.CustomerPortal.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Bff.CustomerPortal.Controllers;

[ApiController]
[Route("api/customer-portal/dashboard")]
[Authorize]
public sealed class DashboardController(
    CustomerServiceClient customerServiceClient,
    AccountServiceClient accountServiceClient,
    DepositServiceClient depositServiceClient,
    ICurrentPortalCustomerAccessor currentPortalCustomerAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var customerId = currentPortalCustomerAccessor.GetRequiredCustomerId();
            var customer = await customerServiceClient.GetByIdAsync(customerId, cancellationToken);
            var accounts = await accountServiceClient.GetByCustomerIdAsync(customerId, 1, 50, cancellationToken);

            var accountNumbersById = accounts.Items.ToDictionary(item => item.AccountId, item => item.AccountNumber, StringComparer.Ordinal);
            var firstAccount = accounts.Items.FirstOrDefault();

            var activityCollections = await Task.WhenAll(accounts.Items.Take(5).Select(async account =>
            {
                var response = await accountServiceClient.GetActivitiesAsync(account.AccountId, "?pageNumber=1&pageSize=5", cancellationToken);
                return response.Items.Select(item => new RecentActivityResponse(
                    account.AccountNumber,
                    MapActivityType(item.PostingType),
                    item.PostingReference,
                    item.Amount,
                    item.Currency,
                    item.CreatedAt));
            }));

            var recentActivities = activityCollections
                .SelectMany(items => items)
                .OrderByDescending(item => item.CreatedAt)
                .Take(6)
                .ToArray();

            var deposits = await depositServiceClient.SearchAsync(customerId, null, 1, 6, cancellationToken);
            var recentTransactions = deposits.Items
                .OrderByDescending(item => item.RequestedAt)
                .Take(6)
                .Select(item => new TransactionStatusSummaryResponse(
                    item.TransactionNumber,
                    accountNumbersById.GetValueOrDefault(item.AccountId, "Unknown"),
                    item.Amount,
                    item.Currency,
                    item.Status,
                    item.ReferenceNumber,
                    item.RequestedAt,
                    item.PostedAt,
                    null,
                    null))
                .ToArray();

            var latestActivity = recentActivities.FirstOrDefault();

            return Ok(new CustomerDashboardResponse(
                new CustomerSnapshotResponse(customer.CustomerNumber, customer.FullName, customer.Status, customer.RiskLevel),
                new PortfolioSummaryResponse(
                    accounts.Items.Count,
                    accounts.Items.Sum(item => item.AvailableBalance),
                    accounts.Items.Sum(item => item.LedgerBalance)),
                firstAccount is null
                    ? null
                    : new AccountSnapshotResponse(
                        firstAccount.AccountNumber,
                        firstAccount.AccountType,
                        firstAccount.Status,
                        firstAccount.Currency,
                        firstAccount.AvailableBalance,
                        firstAccount.LedgerBalance),
                latestActivity is null
                    ? null
                    : new ActivitySnapshotResponse(
                        latestActivity.Type,
                        latestActivity.Reference,
                        latestActivity.Amount,
                        latestActivity.Currency,
                        latestActivity.CreatedAt),
                recentActivities,
                recentTransactions));
        }
        catch (DownstreamApiException exception)
        {
            return StatusCode(exception.StatusCode, new ProblemDetails
            {
                Title = exception.Title,
                Detail = exception.Detail,
                Status = exception.StatusCode
            });
        }
    }

    private static string MapActivityType(int postingType) => postingType switch
    {
        1 => "Deposit",
        2 => "Deposit Reversal",
        3 => "Withdrawal",
        _ => $"Activity {postingType}"
    };
}
