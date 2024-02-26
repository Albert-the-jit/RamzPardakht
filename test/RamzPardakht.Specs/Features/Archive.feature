Feature: Archive
As a user
I want to upload files
So that I can use them and show them

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

Scenario: Successful user file upload and retrieve
    When the "person 1" send upload request containing simple image
    Then the "person 1" should receive a success message confirming success
    And the "person 1" response body should contain the uploaded file unique identifier
    When the "person 1" send request to view uploaded file
    #Then the "person 1" should receive a failed message with "404" status and "Not Found" error massage
    When the "person 1" use uploaded file in "Public" usage
    And the "person 1" send request to view uploaded file
    Then the "person 1" should receive a success message confirming success
    And the "person 1" response should contain uploaded file
    When the anonymous user send request to view "person 1" uploaded file
    Then the "person 1" should receive a success message confirming success
    And the "person 1" response should contain uploaded file
    When the "person 1" use uploaded file in "Internal" usage
    And the "person 1" send request to view uploaded file
    Then the "person 1" should receive a success message confirming success
    And the "person 1" response should contain uploaded file
    When the anonymous user send request to view "person 1" uploaded file
    Then the "person 1" should receive a failed message with "404" status and "Not Found" error massage
