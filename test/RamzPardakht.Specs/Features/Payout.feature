Feature: Payout
As a user
I want to create Payout
So that I can receive and use my founds


    Background:
        When "person 1" sends register request with the following information:
          | Email         | Password | FirstName | LastName |
          | a.z@gmail.com | #Qw1234  |     test  |    test  |
        Then the "person 1" should receive a success message confirming success
        And 'person 1' should receive email contains verify email link
        When "person 1" open verify email link that sent for 'a.z@gmail.com'
        Then the "person 1" should receive a success message confirming success
        And the "person 1" email should be confirmed
        When "person 1" sends valid credentials on login request
        Then the "person 1" should receive a success message confirming success
        And "person 1" should receive access token from login

        When "person 1" send a create access token request with random date within next month as ExpiresUtc and the following details:
          | Name       | Description | Permissions |
          | test-token | test-token  |             |
        Then the "person 1" should receive a success message confirming success
        And the "person 1" response body should contain the created access token and item unique identifier and details
        When "person 1" use "test-token" access token on account info
        Then the "person 1" should receive a success message confirming success
        And the "person 1" response body should contain correct user info

        When "person 2" sends register request with the following information:
          | Email         | Password | FirstName | LastName |
          | a.z2@gmail.com | #Qw1234  |     test2  |    test2  |
        Then the "person 1" should receive a success message confirming success
        And 'person 2' should receive email contains verify email link
        When "person 2" open verify email link that sent for 'a.z2@gmail.com'
        Then the "person 2" should receive a success message confirming success
        And the "person 2" email should be confirmed
        When "person 2" sends valid credentials on login request
        Then the "person 2" should receive a success message confirming success
        And "person 2" should receive access token from login

        When "person 2" send a create access token request with random date within next month as ExpiresUtc and the following details:
          | Name       | Description | Permissions |
          | test-token2 | test-token  |             |
        Then the "person 2" should receive a success message confirming success
        And the "person 2" response body should contain the created access token and item unique identifier and details
        When "person 2" use "test-token2" access token on account info
        Then the "person 2" should receive a success message confirming success
        And the "person 2" response body should contain correct user info


        When "person 1" use "test-token" access token and send a create payment request with the following details:
          | UsdAmount | Description  | WebhookUrl | SuccessUrl        | CancelUrl | ClientRefId |
          | 5         | test-payment |            | http://example.ir |           |             |
        Then the "person 1" should receive a success message confirming success
        And the "person 1" response body should contain the created payment RefId and RedirectUrl and details

        When Unauthorized user "person 10" send request to get initial info of "person 1" payment
        Then the "person 10" response body of "person 1" payment should contain "test-token" access token name and "NotSelected" as currency and valid currency amount conversion

        When Unauthorized user "person 10" send request to select info of "person 1" payment with the following details:
          | Currency | PayerEmail        |
          | BTC      | invalid@email.com |
        Then the "person 10" should receive a success message confirming success

        When Unauthorized user "person 10" send request to get final info of "person 1" payment
        Then the "person 10" response body of "person 1" payment should contain "BTC" currency and valid address and valid amount and "0" paid amount and "New" status

        When Unauthorized user "person 10" has been broadcast transaction to "person 1" payment address in "BTC" blockchain with "1" confirmation and "500" as payment amount
        And Unauthorized user "person 10" send request to get final info of "person 1" payment

        Then the "person 10" response body of "person 1" payment should contain "BTC" currency and valid address and valid amount and "500" paid amount and "Pending" status

        When after user "person 1" payment paid by "person 10" and is expired and confirmed for "6" time
        And "person 1" use "test-token" access token and inquiry the "person 1" created payment info
        Then the "person 1" should receive a success message confirming success
        And the "person 1" response body should contain the created payment RefId with "Paid" Status and following details
          | Currency | SelectedCurrencyAmount | PaidAmount |
          | BTC      | 500                    | 500        |


Scenario: Successful payout creation
    When "person 1" sends user balance request
    Then the "person 1" should receive response that hase balance for "BTC" and 500 as amount

    When "person 1" sends payout request with the following information:
      | Currency | Amount | ToAddress |
      | BTC      | 250    | d         |
    Then the "person 1" should receive a failed message with "400" status and "InvalidWalletAddress" error massage

    When "person 1" sends payout request with the following information:
      | Currency | Amount | ToAddress                                  |
      | BTC      | 750    | tb1qdmzvjd0azxpntnfjm66cdr04zy0grxj6cwfy62 |
    Then the "person 1" should receive a failed message with "400" status and "NotEnoughMoney" error massage

    When "person 1" sends payout request with the following information:
      | Currency | Amount | ToAddress                                  |
      | BTC      | 250    | tb1qdmzvjd0azxpntnfjm66cdr04zy0grxj6cwfy62 |
    Then the "person 1" should receive a success message confirming success
    And the "person 1" response body should contain the created payout and details
    And the "person 1" payout transaction should broadcast

    When "person 1" sends user balance request
    Then the "person 1" should receive response that hase balance for "BTC" and 250 as amount

    When "person 1" send list request for payouts
    Then the "person 1" response body should contain the created payout with transaction id and network fee and "Unconfirmed" status

    When after user "person 1" payout request broadcast and confirmed for "5" time

    And "person 1" send list request for payouts
    Then the "person 1" response body should contain the created payout with transaction id and network fee and "Unconfirmed" status

    When "person 1" sends user balance request
    Then the "person 1" should receive response that hase balance for "BTC" and 250 as amount

    When "person 1" sends payout request with the following information:
      | Currency | Amount | ToAddress                                  |
      | BTC      | 400 | tb1qdmzvjd0azxpntnfjm66cdr04zy0grxj6cwfy62 |
    Then the "person 1" should receive a failed message with "400" status and "NotEnoughMoney" error massage

    When after user "person 1" payout request broadcast and confirmed for "6" time

    And "person 1" send list request for payouts
    Then the "person 1" response body should contain the created payout with transaction id and network fee and "Done" status

    When "person 1" sends user balance request
    Then the "person 1" should receive response that hase balance for "BTC" and 250 as amount

    When "person 1" sends payout request with the following information:
      | Currency | Amount | ToAddress                                  |
      | BTC      | 200    | tb1qdmzvjd0azxpntnfjm66cdr04zy0grxj6cwfy62 |
    Then the "person 1" should receive a success message confirming success
    And the "person 1" response body should contain the created payout and details
    And the "person 1" payout transaction should broadcast

    When "person 1" sends user balance request
    Then the "person 1" should receive response that hase balance for "BTC" and 50 as amount

    When after user "person 1" payout request broadcast and confirmed for "5" time

    And "person 1" send list request for payouts
    Then the "person 1" response body should contain the created payout with transaction id and network fee and "Unconfirmed" status

    When after user "person 1" payout request broadcast and confirmed for "6" time

    And "person 1" send list request for payouts
    Then the "person 1" response body should contain the created payout with transaction id and network fee and "Done" status

    When "person 1" sends user balance request
    Then the "person 1" should receive response that hase balance for "BTC" and 50 as amount

    When "person 1" sends payout request with the following information:
      | Currency | Amount | ToAddress                                  |
      | BTC      | 51     | tb1qdmzvjd0azxpntnfjm66cdr04zy0grxj6cwfy62 |
    Then the "person 1" should receive a failed message with "400" status and "NotEnoughMoney" error massage

    When "person 1" sends payout request with the following information:
      | Currency | Amount | ToAddress                                  |
      | BTC      | 50    | tb1qdmzvjd0azxpntnfjm66cdr04zy0grxj6cwfy62 |
    Then the "person 1" should receive a success message confirming success
    And the "person 1" response body should contain the created payout and details
    And the "person 1" payout transaction should broadcast

    When "person 1" sends user balance request
    Then the "person 1" should receive response that hase balance for "BTC" and 0 as amount

    When after user "person 1" payout request broadcast and confirmed for "5" time

    And "person 1" send list request for payouts
    Then the "person 1" response body should contain the created payout with transaction id and network fee and "Unconfirmed" status

    When after user "person 1" payout request broadcast and confirmed for "6" time

    And "person 1" send list request for payouts
    Then the "person 1" response body should contain the created payout with transaction id and network fee and "Done" status

    When "person 1" sends user balance request
    Then the "person 1" should receive response that hase balance for "BTC" and 0 as amount
