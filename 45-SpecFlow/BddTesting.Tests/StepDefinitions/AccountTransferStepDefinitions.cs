using BddTesting.Api.Models;
using BddTesting.Api.Services;
using TechTalk.SpecFlow;
using Xunit;

namespace BddTesting.Tests.StepDefinitions;

[Binding]
public class AccountTransferStepDefinitions
{
    private readonly BankService _bankService = new();
    private TransferResult? _transferResult;
    private Exception? _caughtException;

    [Given(@"an active source account ""(.*)"" with balance \$(.*)")]
    public void GivenAnActiveSourceAccountWithBalance(string accountNumber, decimal balance)
    {
        _bankService.CreateAccount(new CreateAccountRequest(accountNumber, "Source User", balance));
    }

    [Given(@"an active destination account ""(.*)"" with balance \$(.*)")]
    public void GivenAnActiveDestinationAccountWithBalance(string accountNumber, decimal balance)
    {
        _bankService.CreateAccount(new CreateAccountRequest(accountNumber, "Destination User", balance));
    }

    [Given(@"an inactive destination account ""(.*)""")]
    public void GivenAnInactiveDestinationAccount(string accountNumber)
    {
        // Use Charlie Brown (pre-seeded as inactive ACC-1003) or create inactive account
        // Let's create account then simulate inactive or use ACC-1003 if matching
        if (accountNumber == "ACC-1003")
        {
            return;
        }

        // We can create and let's make sure it's inactive or create an inactive account in service
        var acc = _bankService.CreateAccount(new CreateAccountRequest(accountNumber, "Inactive User", 100m));
        // Use reflection to set IsActive to false for test scenario
        var field = typeof(BankService).GetField("_accounts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field?.GetValue(_bankService) is System.Collections.Concurrent.ConcurrentDictionary<string, BankAccount> dict)
        {
            dict[accountNumber] = acc with { IsActive = false };
        }
    }

    [When(@"the customer transfers \$(.*) from ""(.*)"" to ""(.*)""")]
    public void WhenTheCustomerTransfersFromTo(string amountStr, string fromAccount, string toAccount)
    {
        decimal amount = decimal.Parse(amountStr, System.Globalization.CultureInfo.InvariantCulture);
        try
        {
            _transferResult = _bankService.Transfer(new TransferRequest(fromAccount, toAccount, amount));
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }
    }

    [Then(@"the transfer should be successful")]
    public void ThenTheTransferShouldBeSuccessful()
    {
        Assert.Null(_caughtException);
        Assert.NotNull(_transferResult);
        Assert.Equal("Completed", _transferResult.Status);
    }

    [Then(@"the balance of ""(.*)"" should be \$(.*)")]
    public void ThenTheBalanceOfShouldBe(string accountNumber, decimal expectedBalance)
    {
        var account = _bankService.GetAccount(accountNumber);
        Assert.NotNull(account);
        Assert.Equal(expectedBalance, account.Balance);
    }

    [Then(@"the transfer should fail with error containing ""(.*)""")]
    public void ThenTheTransferShouldFailWithErrorContaining(string expectedError)
    {
        Assert.NotNull(_caughtException);
        Assert.Contains(expectedError, _caughtException.Message, StringComparison.OrdinalIgnoreCase);
    }
}
