Feature: UserAuthentication
As a user
I want to register or login for the website
So that I can access the features and services provided by the website

    Scenario: Successful user registration with email and password Then successful login
        When "person 1" sends register request with the following information:
          | Email | Password | FirstName | LastName |
          | a.z@gmail.com | #Qw1234  |     test  |    test  |
        Then the "person 1" should receive a success message confirming success
        And 'person 1' should receive email contains verify email link
        When "person 2" sends register request with the following information:
          | Email         | Password | FirstName | LastName |
          | a.z@gmail.com | #Qw1234  | test      | test     |
        Then the "person 2" should receive a failed message with "400" status and "DuplicateUserName" error massage
        When "person 1" sends valid credentials on login request
        #Then the "person 1" should receive a failed message with "401" status and "NotAllowed" error massage
        When "person 1" open verify email link that sent for 'a.z@gmail.com'
        Then the "person 1" should receive a success message confirming success
        And the "person 1" email should be confirmed
        When "person 1" sends valid credentials on login request
        Then the "person 1" should receive a success message confirming success
        And "person 1" should receive access token from login

        When "person 1" sends forget password request for 'a.z@gmail.com' user
        Then the "person 1" should receive a success message confirming success
        And 'person 1' should receive email contains code for resting user pass
        When "person 1" open link for resting user pass and send request with code and email and new password "#Qw12345"
        Then the "person 1" should receive a success message confirming success
        When "person 1" sends rested credentials on login request
        Then the "person 1" should receive a success message confirming success
        And "person 1" should receive access token from login

        When "person 1" sends invalid credentials on login request
        Then the "person 1" should receive a failed message with "401" status and "Failed" error massage
        When "person 1" sends invalid credentials on login request
        And "person 1" sends invalid credentials on login request
        And "person 1" sends invalid credentials on login request
        And "person 1" sends invalid credentials on login request
        Then the "person 1" should receive a failed message with "401" status and "LockedOut" error massage
        When "person 1" sends rested credentials on login request
        Then the "person 1" should receive a failed message with "401" status and "LockedOut" error massage
