@media
Feature: MediaApproval
    As the scan worker
    I want to approve a scanned media file
    So that the system stores it with optimized image variants

  Background:
    Given prefers English
    And creates the following client auths:
      | email                   | password       | preferredLanguage |
      | carlos.mendez@correo.mx | CarlosSecure1! | es                |
      | sofia.rivera@correo.mx  | SofiaSecure1!  | es                |
    And signs in the following consumers:
      | email                   |
      | carlos.mendez@correo.mx |
    And signs in the secondary consumer:
      | email                  |
      | sofia.rivera@correo.mx |
    And restores the primary auth token
    And seeds a listing owner id

  @smoke @success
  Scenario: approves a clean file with image variants
    Given uploads a photo to the listing
    When the scanner approves the uploaded photo
    Then they should see the media file
      | status   | variantCount |
      | approved |            4 |

  @failure
  Scenario: approving unknown media file is not found
    When the scanner approves an unknown media file
    Then they should see the media error
      | code               |
      | MediaFile.NotFound |

  @success
  Scenario: deletes a media file
    Given uploads a photo to the listing
    And the scanner approves the uploaded photo
    When deletes the uploaded media file
    And looks up the uploaded media file
    Then they should see the media error
      | code               |
      | MediaFile.NotFound |

  @failure
  Scenario: deleting unknown media file is not found
    When deletes an unknown media file
    Then they should see the media error
      | code               |
      | MediaFile.NotFound |

  @failure
  Scenario: secondary consumer cannot delete media file
    Only the owning publisher may delete media.

    Given uploads a photo to the listing
    And restores the secondary auth token
    When deletes the uploaded media file
    Then they should see the media error
      | code                      |
      | MediaAccess.Owner.Invalid |

  @failure
  Scenario: scanning media file cannot be deleted
    Given uploads a photo to the listing
    And the scanner has started scanning the uploaded photo
    When deletes the uploaded media file
    Then they should see the media error
      | code                        |
      | MediaUpload.Status.Scanning |
