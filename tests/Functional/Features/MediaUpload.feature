Feature: Media file upload
    As a publisher member
    I want to upload photos for my listing
    So that prospects can see the property after the file passes a security scan

Scenario: A photo uploaded for a listing is accepted
    Given Carlos publishes an active listing "Casa familiar de 3 recámaras en Polanco"
    When Carlos uploads a photo to his listing
    Then the upload should be accepted

Scenario: An accepted upload awaits virus scanning before it goes live
    Given Carlos publishes an active listing "Loft frente al mar en Cancún"
    When Carlos uploads a photo to his listing
    Then the upload should be accepted
    And the uploaded file should await virus scanning

Scenario: An oversized photo is rejected
    Given Carlos publishes an active listing "Departamento amueblado en Roma Norte"
    When Carlos uploads an oversized photo to his listing
    Then the system should reject the upload because the file is too large

Scenario: A disallowed file type is rejected
    Given Carlos publishes an active listing "Local comercial en Avenida Reforma"
    When Carlos uploads a disallowed file to his listing
    Then the system should reject the upload because the file type is not allowed

Scenario: Uploading when the photo limit is reached is rejected
    Given Carlos publishes an active listing "Terreno con vista en Valle de Bravo"
    And Carlos's listing already has the maximum number of photos
    When Carlos uploads another photo to his listing
    Then the system should reject the upload because the photo limit is reached

Scenario: Carlos can check the status of an uploaded file
    Given Carlos publishes an active listing "Casa familiar de 3 recámaras en Polanco"
    And Carlos uploaded a photo to his listing
    When Carlos checks the status of his uploaded file
    Then the uploaded file should await virus scanning

Scenario: Checking a file that does not exist is not found
    Given Carlos publishes an active listing "Loft frente al mar en Cancún"
    When Carlos checks the status of a file that does not exist
    Then the media file should not be found

Scenario: Subscribing to scan events without signing in is unauthorized
    Given Carlos publishes an active listing "Casa familiar de 3 recámaras en Polanco"
    And Carlos uploaded a photo to his listing
    When an anonymous visitor subscribes to scan events for the file
    Then the visitor should be asked to sign in

Scenario: Subscribing to events for a non-existent file is not found
    Given Carlos publishes an active listing "Loft frente al mar en Cancún"
    When Carlos subscribes to scan events for a file that does not exist
    Then the media file should not be found

Scenario: Subscribing to events with a malformed id is rejected
    Given Carlos is signed in
    When Carlos subscribes to scan events for a malformed media id
    Then the request should be rejected because the id is malformed

Scenario: A prospect cannot subscribe to another publisher's scan events
    Given Carlos publishes an active listing "Local comercial en Avenida Reforma"
    And Carlos uploaded a photo to his listing
    And María is signed in
    When María subscribes to Carlos's uploaded file scan events
    Then María should not be allowed

Scenario: Carlos receives a fileApproved event when his photo is virus-scanned clean
    Given Carlos publishes an active listing "Casa familiar de 3 recámaras en Polanco"
    And Carlos uploaded a photo to his listing
    And the scanner approved Carlos's uploaded photo
    When Carlos subscribes to scan events for his uploaded file
    Then Carlos should receive a fileApproved scan event
