Feature: Bank Account Funds Transfer
  In order to manage my finances
  As a bank customer
  I want to transfer funds between accounts securely and reliably

  Scenario: Successful funds transfer between active accounts
    Given an active source account "ACC-TEST-1" with balance $500.00
    And an active destination account "ACC-TEST-2" with balance $100.00
    When the customer transfers $150.00 from "ACC-TEST-1" to "ACC-TEST-2"
    Then the transfer should be successful
    And the balance of "ACC-TEST-1" should be $350.00
    And the balance of "ACC-TEST-2" should be $250.00

  Scenario: Transfer fails when sender has insufficient funds
    Given an active source account "ACC-TEST-3" with balance $50.00
    And an active destination account "ACC-TEST-4" with balance $200.00
    When the customer transfers $100.00 from "ACC-TEST-3" to "ACC-TEST-4"
    Then the transfer should fail with error containing "Insufficient funds"
    And the balance of "ACC-TEST-3" should be $50.00
    And the balance of "ACC-TEST-4" should be $200.00

  Scenario: Transfer fails when destination account is inactive
    Given an active source account "ACC-TEST-5" with balance $300.00
    And an inactive destination account "ACC-TEST-6"
    When the customer transfers $50.00 from "ACC-TEST-5" to "ACC-TEST-6"
    Then the transfer should fail with error containing "inactive"
    And the balance of "ACC-TEST-5" should be $300.00

  Scenario: Transfer fails when source and destination are the same
    Given an active source account "ACC-TEST-7" with balance $200.00
    When the customer transfers $50.00 from "ACC-TEST-7" to "ACC-TEST-7"
    Then the transfer should fail with error containing "cannot be the same"
