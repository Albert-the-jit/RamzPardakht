Feature: AccessToken
As a user
I want to have access token
So that I can access the features and services provided by the website

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

        When the "person 1" send upload request containing simple image
        Then the "person 1" should receive a success message confirming success
        And the "person 1" response body should contain the uploaded file unique identifier

Scenario: Successful user access token generation
    When "person 1" send a create access token request with random date within previous month as ExpiresUtc and the following details:
    | Name       | Description | Permissions |
    | test-token | test-token  |             |
    Then the "person 1" should receive a failed message with "400" status and "ShouldBeBiggerThanNow" error massage
    When "person 1" send a create access token request with random date within next month as ExpiresUtc and the following details:
      | Name       | Description | Permissions |
      | test-token | test-token  |             |
    Then the "person 1" should receive a success message confirming success
    And the "person 1" response body should contain the created access token and item unique identifier and details
    When "person 1" use "test-token" access token on account info
    Then the "person 1" should receive a success message confirming success
    And the "person 1" response body should contain correct user info

    When "person 1" send a create access token request with random date within next month as ExpiresUtc and with uploaded image id as logo id the following details:
      | Name       | Description | Permissions |
      | test-token-2 | test-token  |             |
    Then the "person 1" should receive a success message confirming success
    And the "person 1" response body should contain the created access token and item unique identifier and details
    When "person 1" use "test-token" access token on account info
    Then the "person 1" should receive a success message confirming success
    And the "person 1" response body should contain correct user info

    When "person 1" use "test-token" access token and send a create access token request with random date within next month as ExpiresUtc and the following details:
      | Name       | Description | Permissions |
      | test-token | test-token  |             |
    Then the "person 1" should receive a failed message with "403" status
    When "person 1" send request to list created access token details expire within next month
    Then "person 1" should receive response contain the "test-token" access token
    When "person 1" send request remove "test-token" access token
    Then the "person 1" should receive a success message confirming success
    When "person 1" send request remove "test-token" access token
    Then the "person 1" should receive a failed message with "404" status and "Not Found" error massage
    When "person 1" use "test-token" access token on account info
    Then the "person 1" should receive a failed message with "403" status
