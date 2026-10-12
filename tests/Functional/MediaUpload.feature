@media
Feature: MediaUpload
    As a publisher member
    I want to upload photos for my listing
    So that prospects can see the property after the file passes a security scan

  Background:
    Given prefers English
    And creates the following client auths:
      | email                   | password       | preferredLanguage |
      | carlos.mendez@correo.mx | CarlosSecure1! | es                |
      | maria.garcia@correo.mx  | MariaSecure1!  | es                |
    And signs in the following consumers:
      | email                   |
      | carlos.mendez@correo.mx |
    And signs in the secondary consumer:
      | email                  |
      | maria.garcia@correo.mx |
    And restores the primary auth token
    And seeds a listing owner id

  @smoke @success
  Scenario: uploads a photo
    When uploads a photo to the listing
    Then they should see the media file
      | status  |
      | pending |

  @success
  Scenario: uploaded photo awaits virus scanning
    When uploads a photo to the listing
    Then they should see the media file
      | status  |
      | pending |

  @failure
  Scenario: oversized photo is rejected
    When uploads an oversized photo to the listing
    Then they should see the media error
      | code                                  |
      | MediaPolicy.MaxFileSizeBytes.TooLarge |

  @failure
  Scenario: disallowed file type is rejected
    When uploads a disallowed file to the listing
    Then they should see the media error
      | code                                    |
      | MediaPolicy.AllowedContentTypes.Invalid |

  @failure
  Scenario: photo limit is rejected
    Given seeds the maximum number of listing photos
    When uploads a photo to the listing
    Then they should see the media error
      | code                              |
      | MediaPolicy.MaxMediaCount.TooMany |

  @success
  Scenario: looks up uploaded file status
    Given uploads a photo to the listing
    When looks up the uploaded media file
    Then they should see the media file
      | status  |
      | pending |

  @failure
  Scenario: looks up unknown media file
    When looks up media file "00000000-0000-0000-0000-000000000000"
    Then they should see the media error
      | code               |
      | MediaFile.NotFound |

  @failure
  Scenario: anonymous visitor cannot subscribe to scan events

    Scan-event subscription requires the listing owner.
    Given uploads a photo to the listing
    And clears the auth token
    When subscribes to scan events for the uploaded media file
    Then they should see the media error
      | code                |
      | System.Unauthorized |

  @failure
  Scenario: subscribes to events for unknown media file
    When subscribes to scan events for media file "00000000-0000-0000-0000-000000000000"
    Then they should see the media error
      | code               |
      | MediaFile.NotFound |

  @failure
  Scenario: malformed media id is rejected for scan events
    When subscribes to scan events for media file "not-a-guid"
    Then they should see the media error
      | code                      |
      | MediaFileId.InvalidFormat |

  @failure
  Scenario: secondary consumer cannot subscribe to scan events
    Given uploads a photo to the listing
    And restores the secondary auth token
    And denies listing event access for the signed-in user
    When subscribes to scan events for the uploaded media file
    Then they should see the media error
      | code                      |
      | MediaAccess.Owner.Invalid |

  @success
  Scenario: receives fileApproved scan event
    Given uploads a photo to the listing
    And allows listing event access for the signed-in user
    And the scanner approves the uploaded photo
    When subscribes to scan events for the uploaded media file
    Then they should see the media scan event
      | name         |
      | fileApproved |
