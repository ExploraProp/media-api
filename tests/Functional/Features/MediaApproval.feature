Feature: Media file approval and variant generation
    As the scan worker
    I want to approve a scanned media file
    So that the system stores it with optimized image variants

Scenario: A clean file is approved by the scan worker with four image variants
    Given Carlos publishes an active listing "Casa familiar de 3 recámaras en Polanco"
    And Carlos uploaded a photo to his listing
    When the scanner approves Carlos's uploaded photo
    Then the media file should be approved
    And the approved media file should have 4 image variants

Scenario: Approving a file that does not exist is not found
    Given Carlos publishes an active listing "Loft frente al mar en Cancún"
    When the scanner approves a file that does not exist
    Then the media file should not be found

Scenario: Deleting a media file removes it from the system
    Given Carlos publishes an active listing "Departamento amueblado en Roma Norte"
    And Carlos uploaded a photo to his listing
    And the scanner approved Carlos's uploaded photo
    When Carlos deletes his uploaded photo
    Then the media file should be deleted

Scenario: Deleting a file that does not exist is not found
    Given Carlos publishes an active listing "Local comercial en Avenida Reforma"
    When Carlos deletes a file that does not exist
    Then the media file should not be found

Scenario: A publisher cannot delete another publisher's photo
    Given Carlos publishes an active listing "Terreno con vista en Valle de Bravo"
    And Carlos uploaded a photo to his listing
    And Sofía is signed in as a different publisher member
    When Sofía tries to delete Carlos's photo
    Then Sofía should not be allowed

Scenario: A photo that is being scanned cannot be deleted
    Given Carlos publishes an active listing "Casa familiar de 3 recámaras en Polanco"
    And Carlos uploaded a photo to his listing
    And the scanner has started scanning Carlos's photo
    When Carlos deletes his uploaded photo
    Then the system should reject the deletion because the file is being scanned
