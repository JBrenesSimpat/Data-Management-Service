Feature: Resources "Update" Operation validations

        Background:
            Given the Data Management Service must receive a token issued by "http://localhost"
              And user is already authorized
              And a POST request is made to "ed-fi/absenceEventCategoryDescriptors" with
                  """
                  {
                    "codeValue": "Sick Leave",
                    "description": "Sick Leave",
                    "effectiveBeginDate": "2024-05-14",
                    "effectiveEndDate": "2024-05-14",
                    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
                    "shortDescription": "Sick Leave"
                  }
                  """
             Then it should respond with 201 or 200

        Scenario: 01 Verify that existing resources can be updated successfully
            # The id value should be replaced with the resource created in the Background section
             When a PUT request is made to "ed-fi/absenceEventCategoryDescriptors/{id}" with
                  """
                  {
                    "id": "{id}",
                    "codeValue": "Sick Leave",
                    "description": "Sick Leave Edited",
                    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
                    "shortDescription": "Sick Leave"
                  }
                  """
             Then it should respond with 204

        Scenario: 02 Verify updating a resource with valid data
             # The id value should be replaced with the resource created in the Background section
             When a PUT request is made to "ed-fi/absenceEventCategoryDescriptors/{id}" with
                  """
                  {
                    "id": "{id}",
                    "codeValue": "Sick Leave",
                    "description": "Sick Leave Edited",
                    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
                    "shortDescription": "Sick Leave"
                  }
                  """
             Then it should respond with 204
             When a GET request is made to "ed-fi/absenceEventCategoryDescriptors/{id}"
             Then it should respond with 200
              And the response body is
                  """
                  {
                    "id": "{id}",
                    "codeValue": "Sick Leave",
                    "description": "Sick Leave Edited",
                    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
                    "shortDescription": "Sick Leave"
                  }
                  """

        Scenario: 03 Verify updating a non existing resource with valid data
             # The id value should be replaced with a non existing resource
             When a PUT request is made to "ed-fi/absenceEventCategoryDescriptors/00000000-0000-4000-a000-000000000000" with
                  """
                  {
                    "id": "00000000-0000-4000-a000-000000000000",
                    "codeValue": "Sick Leave",
                    "description": "Sick Leave Edited",
                    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
                    "shortDescription": "Sick Leave"
                  }
                  """
             Then it should respond with 404
              And the response body is
                  """
                  {
                      "detail": "Resource to update was not found",
                      "type": "urn:ed-fi:api:not-found",
                      "title": "Not Found",
                      "status": 404,
                      "correlationId": null,
                      "validationErrors": null,
                      "errors": null
                  }
                  """

        Scenario: 04 Verify error handling updating the document identity
            # The id value should be replaced with the resource created in the Background section
             When a PUT request is made to "ed-fi/absenceEventCategoryDescriptors/{id}" with
                  """
                  {
                    "id": "{id}",
                    "codeValue": "Sick Leave",
                    "description": "Sick Leave Edited",
                    "namespace": "AbsenceEventCategoryDescriptor",
                    "shortDescription": "Sick Leave Edited"
                  }
                  """
             Then it should respond with 400
              And the response body is
                  """
                  {
                    "detail": "Identifying values for the AbsenceEventCategoryDescriptor resource cannot be changed. Delete and recreate the resource item instead.",
                    "type": "urn:ed-fi:api:bad-request:data-validation-failed:key-change-not-supported",
                    "title": "Key Change Not Supported",
                    "status": 400,
                    "correlationId": null,
                    "validationErrors": null,
                    "errors": null
                    }
                  """

        Scenario: 05 Verify that response contains the updated resource ID and data
            # The id value should be replaced with the resource created in the Background section
             When a PUT request is made to "ed-fi/absenceEventCategoryDescriptors/{id}" with
                  """
                  {
                    "id": "{id}",
                    "codeValue": "Sick Leave",
                    "description": "Sick Leave Edited",
                    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
                    "shortDescription": "Sick Leave"
                  }
                  """
             Then it should respond with 204
              And the response headers includes
              #replace header {id} with the correct value
                  """
                  {
                      "location": "/ed-fi/absenceEventCategoryDescriptors/{id}"
                  }
                  """

        Scenario: 06 Verify error handling when updating a resource with empty body
             # The id value should be replaced with the resource created in the Background section
             When a PUT request is made to "ed-fi/absenceEventCategoryDescriptors/{id}" with
                  """
                  {
                  }
                  """
             Then it should respond with 400
              And the response body is
                  """
                  {
                    "detail": "Data validation failed. See 'validationErrors' for details.",
                    "type": "urn:ed-fi:api:bad-request:data",
                    "title": "Data Validation Failed",
                    "status": 400,
                    "correlationId": null,
                    "validationErrors": {
                        "$.namespace": [
                        "namespace is required."
                        ],
                        "$.codeValue": [
                        "codeValue is required."
                        ],
                        "$.shortDescription": [
                        "shortDescription is required."
                        ],
                        "$.id": [
                        "id is required."
                        ]
                    },
                    "errors": []
                  }
                  """

        Scenario: 07 Verify error handling when resource ID is different in body on PUT
             # The id value should be replaced with the resource created in the Background section
             When a PUT request is made to "ed-fi/absenceEventCategoryDescriptors/{id}" with
                  """
                  {
                    "id": "00000000-0000-0000-0000-000000000000",
                    "codeValue": "Sick Leave",
                    "description": "Sick Leave Edited",
                    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
                    "shortDescription": "Sick Leave"
                  }
                  """
             Then it should respond with 400
              And the response body is
                  """
                  {
                    "detail": "The request could not be processed. See 'errors' for details.",
                    "type": "urn:ed-fi:api:bad-request",
                    "title": "Bad Request",
                    "status": 400,
                    "correlationId": null,
                    "validationErrors": null,
                    "errors": [
                        "Request body id must match the id in the url."
                    ]
                  }
                  """

        Scenario: 08 Verify error handling when resource ID is not included in body on PUT
            # The id value should be replaced with the resource created in the Background section
             When a PUT request is made to "ed-fi/absenceEventCategoryDescriptors/{id}" with
                  """
                  {
                    "id": "",
                    "codeValue": "Sick Leave",
                    "description": "Sick Leave Edited",
                    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
                    "shortDescription": "Sick Leave"
                  }
                  """
             Then it should respond with 400
              And the response body is
                 """
                  {
                    "detail": "The request could not be processed. See 'errors' for details.",
                    "type": "urn:ed-fi:api:bad-request",
                    "title": "Bad Request",
                    "status": 400,
                    "correlationId": null,
                    "validationErrors": null,
                    "errors": [
                        "Request body id must match the id in the url."
                    ]
                  }
                  """

          Scenario: 09 Verify error handling when resource ID is invalid Guid in body on PUT
             # The id value should be replaced with the resource created in the Background section
             When a PUT request is made to "ed-fi/absenceEventCategoryDescriptors/{id}" with
                  """
                  {
                    "id": "invalid-id",
                    "codeValue": "Sick Leave",
                    "description": "Sick Leave Edited",
                    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
                    "shortDescription": "Sick Leave"
                  }
                  """
             Then it should respond with 400
              And the response body is
                  """
                  {
                    "detail": "The request could not be processed. See 'errors' for details.",
                    "type": "urn:ed-fi:api:bad-request",
                    "title": "Bad Request",
                    "status": 400,
                    "correlationId": null,
                    "validationErrors": null,
                    "errors": [
                        "Request body id must match the id in the url."
                    ]
                  }
                  """

    Rule: Test with overposted values
        Background:
            Given the Data Management Service must receive a token issued by "http://localhost"
              And user is already authorized
              And a POST request is made to "ed-fi/educationContents" with
                  """
                  {
                    "contentIdentifier": "Testing",
                    "namespace": "Testing",
                    "shortDescription": "Testing",
                    "contentClassDescriptor": "uri://ed-fi.org/ContentClassDescriptor#Testing",
                    "learningResourceMetadataURI": "Testing",
                       "objectOverpost": {
                         "x": 1
                      }
                  }
                  """
             Then it should respond with 201 or 200

        Scenario: 09 Verify Put when adding a overposting object
             When a PUT request is made to "ed-fi/educationContents/{id}" with
                  """
                  {
                    "id": "{id}",
                    "contentIdentifier": "Testing",
                    "namespace": "Testing",
                    "shortDescription": "Testing",
                    "contentClassDescriptor": "uri://ed-fi.org/ContentClassDescriptor#Testing",
                    "learningResourceMetadataURI": "Testing",
                    "scalarOverpost": "x",
                    "objectOverpost": {
                       "x": 1
                    }
                  }
                  """
             Then it should respond with 204
              And the record can be retrieved with a GET request
                  """
                  {
                    "id": "{id}",
                    "contentIdentifier": "Testing",
                    "namespace": "Testing",
                    "shortDescription": "Testing",
                    "contentClassDescriptor": "uri://ed-fi.org/ContentClassDescriptor#Testing",
                    "learningResourceMetadataURI": "Testing"
                  }
                  """
