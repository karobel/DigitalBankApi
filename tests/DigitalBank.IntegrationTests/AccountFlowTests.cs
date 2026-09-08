using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DigitalBank.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace DigitalBank.IntegrationTests;

public class AccountFlowTests : IClassFixture<DigitalBankWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AccountFlowTests(DigitalBankWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var register = new RegisterUserDto(
            Username: $"testuser_{Guid.NewGuid():N}".Substring(0, 20),
            Email: $"{Guid.NewGuid():N}@example.com",
            Password: "Str0ngPassw0rd!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", register);
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return auth!.Token;
    }

    [Fact]
    public async Task FullBankingFlow_RegisterCreateCustomerCreateAccountDepositAndCheckBalance_Succeeds()
    {
        // 1. Register and authenticate
        var token = await RegisterAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Create a customer
        var customerDto = new CreateCustomerDto("Jane", "Doe", $"{Guid.NewGuid():N}@example.com", "514-555-0199", new DateTime(1990, 5, 1));
        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", customerDto);
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        // 3. Open an account for that customer
        var accountDto = new CreateAccountDto(customer!.Id, Domain.Enums.AccountType.Checking, "CAD");
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        accountResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        account!.Balance.Should().Be(0m);

        // 4. Deposit funds
        var depositResponse = await _client.PostAsJsonAsync($"/api/v1/accounts/{account.Id}/deposit", new DepositDto(250m, "Initial deposit"));
        depositResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await depositResponse.Content.ReadFromJsonAsync<TransactionDto>();
        transaction!.BalanceAfter.Should().Be(250m);

        // 5. Verify balance via GET
        var getResponse = await _client.GetAsync($"/api/v1/accounts/{account.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshedAccount = await getResponse.Content.ReadFromJsonAsync<AccountDto>();
        refreshedAccount!.Balance.Should().Be(250m);
    }

    [Fact]
    public async Task Withdraw_WithInsufficientFunds_ReturnsBadRequest()
    {
        var token = await RegisterAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerDto = new CreateCustomerDto("John", "Smith", $"{Guid.NewGuid():N}@example.com", "514-555-0188", new DateTime(1988, 3, 1));
        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", customerDto);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var accountDto = new CreateAccountDto(customer!.Id, Domain.Enums.AccountType.Savings, "CAD");
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var withdrawResponse = await _client.PostAsJsonAsync($"/api/v1/accounts/{account!.Id}/withdraw", new WithdrawDto(500m, null));

        withdrawResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomers_WithoutAuthToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
