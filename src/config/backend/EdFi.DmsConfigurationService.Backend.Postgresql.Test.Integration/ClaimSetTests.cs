// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data.Common;
using EdFi.DmsConfigurationService.Backend.Postgresql.Repositories;
using EdFi.DmsConfigurationService.Backend.Repositories;
using EdFi.DmsConfigurationService.DataModel.Model;
using EdFi.DmsConfigurationService.DataModel.Model.Application;
using EdFi.DmsConfigurationService.DataModel.Model.ClaimSets;
using EdFi.DmsConfigurationService.DataModel.Model.Vendor;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EdFi.DmsConfigurationService.Backend.Postgresql.Test.Integration;

public class ClaimSetTests : DatabaseTest
{
    private readonly IClaimSetRepository _repository = new ClaimSetRepository(
        Configuration.DatabaseOptions,
        NullLogger<ClaimSetRepository>.Instance,
        new ClaimsHierarchyRepository(
            Configuration.DatabaseOptions,
            NullLogger<ClaimsHierarchyRepository>.Instance
        )
    );

    [TestFixture]
    public class InsertTest : ClaimSetTests
    {
        private long _id;

        [SetUp]
        public async Task Setup()
        {
            ClaimSetInsertCommand claimSet = new() { Name = "Test-ClaimSet" };

            var result = await _repository.InsertClaimSet(claimSet);
            result.Should().BeOfType<ClaimSetInsertResult.Success>();
            _id = (result as ClaimSetInsertResult.Success)!.Id;
            _id.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task Should_get_test_claimSet_from_get_all()
        {
            var getResult = await _repository.QueryClaimSet(
                new PagingQuery() { Limit = 25, Offset = 0 },
                false
            );
            getResult.Should().BeOfType<ClaimSetQueryResult.Success>();

            object claimSetFromDb = ((ClaimSetQueryResult.Success)getResult).ClaimSetResponses.First();
            claimSetFromDb.Should().NotBeNull();
            claimSetFromDb.Should().BeOfType<ClaimSetResponseReduced>();

            var reducedResponse = (ClaimSetResponseReduced)claimSetFromDb;
            reducedResponse.Name.Should().Be("Test-ClaimSet");
        }

        [Test]
        public async Task Should_get_test_claimSet_from_get_by_id()
        {
            var getByIdResult = await _repository.GetClaimSet(_id, false);
            getByIdResult.Should().BeOfType<ClaimSetGetResult.Success>();

            object claimSetFromDb = ((ClaimSetGetResult.Success)getByIdResult).ClaimSetResponse;
            claimSetFromDb.Should().BeOfType<ClaimSetResponseReduced>();

            var reducedResponse = (ClaimSetResponseReduced)claimSetFromDb;
            reducedResponse.Name.Should().Be("Test-ClaimSet");
        }

        [Test]
        public async Task Should_get_duplicate_failure()
        {
            ClaimSetInsertCommand claimSetDup = new() { Name = "Test-ClaimSet" };

            var resultDup = await _repository.InsertClaimSet(claimSetDup);
            resultDup.Should().BeOfType<ClaimSetInsertResult.FailureDuplicateClaimSetName>();
        }
    }

    [TestFixture]
    public class UpdateTests : ClaimSetTests
    {
        private ClaimSetInsertCommand _insertClaimSet = null!;
        private ClaimSetUpdateCommand _updateClaimSet = null!;
        private long _applicationId;
        private IApplicationRepository _applicationRepository;
        private IClaimsHierarchyRepository _claimsHierarchyRepository;
        private ClaimSetInsertResult _insertSystemReservedResult;

        [SetUp]
        public async Task Setup()
        {
            IVendorRepository repository = new VendorRepository(
                Configuration.DatabaseOptions,
                NullLogger<VendorRepository>.Instance
            );

            VendorInsertCommand vendor = new()
            {
                Company = "Test Company",
                ContactEmailAddress = "test@test.com",
                ContactName = "Fake Name",
                NamespacePrefixes = "FakePrefix1,FakePrefix2",
            };

            var vendorResult = await repository.InsertVendor(vendor);
            var vendorId = (vendorResult as VendorInsertResult.Success)!.Id;

            // Create the application to contain the claim set to be renamed
            _applicationRepository = new ApplicationRepository(
                Configuration.DatabaseOptions,
                NullLogger<ApplicationRepository>.Instance
            );

            ApplicationInsertCommand application = new()
            {
                ApplicationName = "Test Application",
                VendorId = vendorId,
                ClaimSetName = "Test-Insert-ClaimSet",
                EducationOrganizationIds = [1, 255911001, 255911002],
            };

            var applicationResult = await _applicationRepository.InsertApplication(
                application,
                new() { ClientId = Guid.NewGuid().ToString(), ClientUuid = Guid.NewGuid() }
            );

            _applicationId = (applicationResult as ApplicationInsertResult.Success)?.Id ?? 0;

            // Insert claim set
            _insertClaimSet = new ClaimSetInsertCommand() { Name = "Test-Insert-ClaimSet" };
            var insertResult = await _repository.InsertClaimSet(_insertClaimSet);
            insertResult.Should().BeOfType<ClaimSetInsertResult.Success>();

            // Insert system-reserved claim set
            var insertSystemReservedClaimSet = new ClaimSetInsertCommand()
            {
                Name = "Test-Insert-System-Reserved-ClaimSet",
                IsSystemReserved = true,
            };
            _insertSystemReservedResult = await _repository.InsertClaimSet(insertSystemReservedClaimSet);
            _insertSystemReservedResult.Should().BeOfType<ClaimSetInsertResult.Success>();

            // Initialize claims hierarchy
            _claimsHierarchyRepository = new ClaimsHierarchyRepository(
                Configuration.DatabaseOptions,
                NullLogger<ClaimsHierarchyRepository>.Instance
            );

            var claimsHierarchy = new List<Claim>
            {
                new Claim
                {
                    Name = "RootClaim",
                    ClaimSets = new List<ClaimSet> { new ClaimSet { Name = "Test-Insert-ClaimSet" } },
                    Claims = new List<Claim>
                    {
                        new Claim
                        {
                            Name = "ChildClaim",
                            ClaimSets = new List<ClaimSet> { new ClaimSet { Name = "Test-Insert-ClaimSet" } },
                        },
                    },
                },
            };

            var saveResult = await _claimsHierarchyRepository.SaveClaimsHierarchy(claimsHierarchy, default);
            saveResult.Should().BeOfType<ClaimsHierarchySaveResult.Success>();

            // Update the claim set
            _updateClaimSet = new ClaimSetUpdateCommand()
            {
                Id = (insertResult as ClaimSetInsertResult.Success)!.Id,
                Name = "Test-Update-ClaimSet",
            };

            var updateResult = await _repository.UpdateClaimSet(_updateClaimSet);
            updateResult.Should().BeOfType<ClaimSetUpdateResult.Success>();
        }

        [Test]
        public async Task Should_get_updated_and_system_reserved_claimSets_from_get_all()
        {
            var getResult = await _repository.QueryClaimSet(
                new PagingQuery() { Limit = 25, Offset = 0 },
                false
            );
            getResult.Should().BeOfType<ClaimSetQueryResult.Success>();

            object claimSetFromDb = ((ClaimSetQueryResult.Success)getResult).ClaimSetResponses.First();
            claimSetFromDb.Should().NotBeNull();
            claimSetFromDb.Should().BeOfType<ClaimSetResponseReduced>();

            var reducedResponse = (ClaimSetResponseReduced)claimSetFromDb;
            reducedResponse.Name.Should().Be("Test-Update-ClaimSet");
            reducedResponse.IsSystemReserved.Should().Be(false);

            object reservedClaimSetFromDb = ((ClaimSetQueryResult.Success)getResult)
                .ClaimSetResponses.Skip(1)
                .First();
            var reducedSystemReservedResponse = (ClaimSetResponseReduced)reservedClaimSetFromDb;
            reducedSystemReservedResponse.Name.Should().Be("Test-Insert-System-Reserved-ClaimSet");
            reducedSystemReservedResponse.IsSystemReserved.Should().Be(true);
        }

        [Test]
        public async Task Should_get_test_claimSet_from_get_by_id()
        {
            var getByIdResult = await _repository.GetClaimSet(_updateClaimSet.Id, false);
            getByIdResult.Should().BeOfType<ClaimSetGetResult.Success>();

            object claimSetFromDb = ((ClaimSetGetResult.Success)getByIdResult).ClaimSetResponse;
            claimSetFromDb.Should().BeOfType<ClaimSetResponseReduced>();

            var reducedResponse = (ClaimSetResponseReduced)claimSetFromDb;
            reducedResponse.Name.Should().Be("Test-Update-ClaimSet");
            reducedResponse.IsSystemReserved.Should().Be(false);
        }

        [Test]
        public async Task Should_get_renamed_claimSet_from_application_by_id()
        {
            var getByIdResult = await _applicationRepository.GetApplication(_applicationId);
            getByIdResult.Should().BeOfType<ApplicationGetResult.Success>();

            var applicationResponse = ((ApplicationGetResult.Success)getByIdResult).ApplicationResponse;

            applicationResponse.ClaimSetName.Should().Be("Test-Update-ClaimSet");
        }

        [Test]
        public async Task Should_update_all_occurrences_of_claimSet_name_in_claims_hierarchy()
        {
            // Retrieve the updated claims hierarchy
            var hierarchyResult = await _claimsHierarchyRepository.GetClaimsHierarchy();
            hierarchyResult.Should().BeOfType<ClaimsHierarchyGetResult.Success>();

            var claims = ((ClaimsHierarchyGetResult.Success)hierarchyResult).Claims;

            // Verify that "Test-Insert-ClaimSet" no longer exists
            bool containsOldClaimSet = claims.Any(c => ContainsClaimSet(c, "Test-Insert-ClaimSet"));
            containsOldClaimSet.Should().BeFalse();

            // Verify that "Test-Update-ClaimSet" exists in all appropriate places
            bool containsNewClaimSet = claims.Any(c => ContainsClaimSet(c, "Test-Update-ClaimSet"));
            containsNewClaimSet.Should().BeTrue();
        }

        [Test]
        public async Task Should_return_failure_when_attempting_to_update_system_reserved_claim_set()
        {
            // Attempt to update the system-reserved claim set
            var updateSystemReserved = new ClaimSetUpdateCommand()
            {
                Id = (_insertSystemReservedResult as ClaimSetInsertResult.Success)!.Id,
                Name = "Test-Update-System-Reserved-ClaimSet",
            };

            var updateSystemReservedResult = await _repository.UpdateClaimSet(updateSystemReserved);
            updateSystemReservedResult.Should().BeOfType<ClaimSetUpdateResult.FailureSystemReserved>();
        }

        private bool ContainsClaimSet(Claim claim, string claimSetName)
        {
            if (claim.ClaimSets.Any(cs => cs.Name == claimSetName))
            {
                return true;
            }

            if (claim.Claims.Any(c => ContainsClaimSet(c, claimSetName)))
            {
                return true;
            }

            return false;
        }
    }

    [TestFixture]
    public class UpdateRetryPolicyTests : ClaimSetTests
    {
        private ClaimSetInsertCommand _insertClaimSet = null!;
        private ClaimSetUpdateCommand _updateClaimSet = null!;

        [SetUp]
        public async Task Setup()
        {
            // Insert claim set
            _insertClaimSet = new ClaimSetInsertCommand() { Name = "Test-Retry-ClaimSet" };
            var insertResult = await _repository.InsertClaimSet(_insertClaimSet);
            insertResult.Should().BeOfType<ClaimSetInsertResult.Success>();

            await InitializeClaimsHierarchy();

            // Prepare update command
            _updateClaimSet = new ClaimSetUpdateCommand() { Name = "Test-Retry-Updated-ClaimSet" };
            _updateClaimSet.Id = (insertResult as ClaimSetInsertResult.Success)!.Id;

            async Task InitializeClaimsHierarchy()
            {
                var claimsHierarchyRepository = CreateClaimsHierarchyRepository();

                var claimsHierarchy = new List<Claim>
                {
                    new Claim
                    {
                        Name = "RootClaim",
                        ClaimSets = new List<ClaimSet> { new ClaimSet { Name = "Test-Retry-ClaimSet" } },
                        Claims = new List<Claim>
                        {
                            new Claim
                            {
                                Name = "ChildClaim",
                                ClaimSets = new List<ClaimSet>
                                {
                                    new ClaimSet { Name = "Test-Retry-ClaimSet" },
                                },
                            },
                        },
                    },
                };

                await claimsHierarchyRepository.SaveClaimsHierarchy(claimsHierarchy, default);
            }
        }

        [TestCase(4, false)]
        [TestCase(3, true)]
        public async Task Should_retry_3_times_on_lastmodifieddate_conflict(
            int multiUserConflictCount,
            bool expectSuccess
        )
        {
            // Arrange

            // Wrap the repository to introduce multi-user conflicts during save
            var claimsHierarchyRepository = new ClaimsHierarchyRepositoryMultiUserTestDecorator(
                CreateClaimsHierarchyRepository(),
                multiUserConflictCount
            );

            var claimSetRepository = new ClaimSetRepository(
                Configuration.DatabaseOptions,
                NullLogger<ClaimSetRepository>.Instance,
                claimsHierarchyRepository
            );

            // Act

            // Attempt to update the claim set, expecting retries
            var updateResult = await claimSetRepository.UpdateClaimSet(_updateClaimSet);

            // Assert

            // Ensure the correct number of attempts were made to apply the claim name change to the hierarchy
            claimsHierarchyRepository
                .SaveClaimsHierarchyInvocationCount.Should()
                .Be(Math.Min(4, multiUserConflictCount + 1));

            if (expectSuccess)
            {
                // Verify that the update succeeded after retries were attempted
                updateResult.Should().BeOfType<ClaimSetUpdateResult.Success>();
            }
            else
            {
                // Verify that the update failed after 3 retries
                updateResult.Should().BeOfType<ClaimSetUpdateResult.FailureMultiUserConflict>();
            }
        }

        private static ClaimsHierarchyRepository CreateClaimsHierarchyRepository()
        {
            // Initialize claims hierarchy
            var claimsHierarchyRepository = new ClaimsHierarchyRepository(
                Configuration.DatabaseOptions,
                NullLogger<ClaimsHierarchyRepository>.Instance
            );

            return claimsHierarchyRepository;
        }

        private class ClaimsHierarchyRepositoryMultiUserTestDecorator(
            IClaimsHierarchyRepository _claimsHierarchyRepository,
            int _conflictingUpdateCount = int.MaxValue
        ) : IClaimsHierarchyRepository
        {
            private readonly IClaimsHierarchyRepository _multiUserClaimsHierarchyRepository =
                new ClaimsHierarchyRepository(
                    Configuration.DatabaseOptions,
                    NullLogger<ClaimsHierarchyRepository>.Instance
                );

            private int _remainingConflictingUpdateCount = _conflictingUpdateCount;

            public Task<ClaimsHierarchyGetResult> GetClaimsHierarchy()
            {
                // Pass the call through unmodified
                return _claimsHierarchyRepository.GetClaimsHierarchy();
            }

            public int SaveClaimsHierarchyInvocationCount;

            public async Task<ClaimsHierarchySaveResult> SaveClaimsHierarchy(
                List<Claim> claimsHierarchy,
                DateTime existingLastModifiedDate,
                DbTransaction? transaction = null
            )
            {
                // Increment invocation counter for inspection by tests
                SaveClaimsHierarchyInvocationCount++;

                // If the call is part of an existing transaction, force a multi-user conflict on a separate connection
                if (transaction != null && _remainingConflictingUpdateCount > 0)
                {
                    _remainingConflictingUpdateCount--;

                    var hierarchyResult = GetCurrentClaimsHierarchy();

                    hierarchyResult.claims.Add(
                        new Claim() { Name = $"Test-MultiUser-Claim-{Random.Shared.Next(100000)}" }
                    );

                    var result = await _multiUserClaimsHierarchyRepository.SaveClaimsHierarchy(
                        hierarchyResult.claims,
                        hierarchyResult.lastModifiedDate
                    );

                    result.Should().BeOfType<ClaimsHierarchySaveResult.Success>();
                }

                // Pass the call through
                return await _claimsHierarchyRepository.SaveClaimsHierarchy(
                    claimsHierarchy,
                    existingLastModifiedDate,
                    transaction
                );

                (List<Claim> claims, DateTime lastModifiedDate) GetCurrentClaimsHierarchy()
                {
                    var claimsResult = _multiUserClaimsHierarchyRepository
                        .GetClaimsHierarchy()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();

                    claimsResult.Should().BeOfType<ClaimsHierarchyGetResult.Success>();

                    var success = (claimsResult as ClaimsHierarchyGetResult.Success)!;

                    return (success.Claims, success.LastModifiedDate);
                }
            }
        }
    }

    [TestFixture]
    public class DeleteTests : ClaimSetTests
    {
        private long _id1;
        private long _id2;

        [SetUp]
        public async Task Setup()
        {
            var insertResult1 = await _repository.InsertClaimSet(
                new ClaimSetInsertCommand() { Name = "Test-One" }
            );
            _id1 = ((ClaimSetInsertResult.Success)insertResult1).Id;

            var insertResult2 = await _repository.InsertClaimSet(
                new ClaimSetInsertCommand() { Name = "Test-Two", IsSystemReserved = true }
            );

            _id2 = ((ClaimSetInsertResult.Success)insertResult2).Id;

            var result = await _repository.DeleteClaimSet(_id1);
            result.Should().BeOfType<ClaimSetDeleteResult.Success>();
        }

        [Test]
        public async Task Should_get_one_test_claimSet_from_get_all()
        {
            var result = await _repository.QueryClaimSet(new PagingQuery() { Limit = 25, Offset = 0 }, false);
            result.Should().BeOfType<ClaimSetQueryResult.Success>();

            ((ClaimSetQueryResult.Success)result).ClaimSetResponses.Count().Should().Be(1);
        }

        [Test]
        public async Task Should_return_not_found_for_deleted_claim_set()
        {
            var result1 = await _repository.GetClaimSet(_id1, false);
            result1.Should().BeOfType<ClaimSetGetResult.FailureNotFound>();
        }

        [Test]
        public async Task Should_get_remaining_test_claimSet_successfully()
        {
            var result2 = await _repository.GetClaimSet(_id2, false);
            result2.Should().BeOfType<ClaimSetGetResult.Success>();
        }

        [Test]
        public async Task Should_return_not_found_when_attempting_to_delete_non_existing_claim_set_id()
        {
            var deleteResult = await _repository.DeleteClaimSet(int.MaxValue);
            deleteResult.Should().BeOfType<ClaimSetDeleteResult.FailureNotFound>();
        }

        [Test]
        public async Task Should_return_system_reserved_error_when_attempting_to_delete_system_reserved_claim_set()
        {
            var deleteResult = await _repository.DeleteClaimSet(_id2);
            deleteResult.Should().BeOfType<ClaimSetDeleteResult.FailureSystemReserved>();
        }
    }

    [TestFixture]
    public class ExportTest : ClaimSetTests
    {
        private long _id;

        [SetUp]
        public async Task Setup()
        {
            ClaimSetInsertCommand claimSet = new() { Name = "Test-Export-ClaimSet" };

            var result = await _repository.InsertClaimSet(claimSet);
            result.Should().BeOfType<ClaimSetInsertResult.Success>();
            _id = (result as ClaimSetInsertResult.Success)!.Id;
            _id.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task Should_export_claimSet()
        {
            var result = await _repository.Export(_id);
            result.Should().BeOfType<ClaimSetExportResult.Success>();

            var valueFromDb = ((ClaimSetExportResult.Success)result).ClaimSetExportResponse;
            valueFromDb.Name.Should().Be("Test-Export-ClaimSet");
        }
    }

    [TestFixture]
    public class ImportTest : ClaimSetTests
    {
        private long _id;

        [SetUp]
        public async Task Setup()
        {
            ClaimSetImportCommand claimSet = new() { Name = "Test-Import-ClaimSet" };

            var result = await _repository.Import(claimSet);
            result.Should().BeOfType<ClaimSetImportResult.Success>();
            _id = (result as ClaimSetImportResult.Success)!.Id;
            _id.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task Should_get_one_test_claimSet_from_get_all()
        {
            var result = await _repository.QueryClaimSet(new PagingQuery() { Limit = 25, Offset = 0 }, false);
            result.Should().BeOfType<ClaimSetQueryResult.Success>();

            ((ClaimSetQueryResult.Success)result).ClaimSetResponses.Count().Should().Be(1);
        }

        [Test]
        public async Task Should_get_test_claimSet_from_get_by_id()
        {
            var getByIdResult = await _repository.GetClaimSet(_id, true);
            getByIdResult.Should().BeOfType<ClaimSetGetResult.Success>();

            object claimSetFromDb = ((ClaimSetGetResult.Success)getByIdResult).ClaimSetResponse;
            claimSetFromDb.Should().BeOfType<ClaimSetResponse>();

            var response = (ClaimSetResponse)claimSetFromDb;
            response.Name.Should().Be("Test-Import-ClaimSet");
        }

        [Test]
        public async Task Should_get_duplicate_failure()
        {
            ClaimSetImportCommand claimSetDup = new() { Name = "Test-Import-ClaimSet" };

            var resultDup = await _repository.Import(claimSetDup);
            resultDup.Should().BeOfType<ClaimSetImportResult.FailureDuplicateClaimSetName>();
        }
    }

    [TestFixture]
    public class CopyTest : ClaimSetTests
    {
        private long _id;
        private long _idCopy;

        [SetUp]
        public async Task Setup()
        {
            ClaimSetInsertCommand claimSet = new() { Name = "Original-ClaimSet" };

            var result = await _repository.InsertClaimSet(claimSet);
            result.Should().BeOfType<ClaimSetInsertResult.Success>();
            _id = (result as ClaimSetInsertResult.Success)!.Id;
            _id.Should().BeGreaterThan(0);

            ClaimSetCopyCommand command = new() { OriginalId = _id, Name = "Copy-Test-ClaimSet" };

            var copy = await _repository.Copy(command);
            copy.Should().BeOfType<ClaimSetCopyResult.Success>();
            _idCopy = (copy as ClaimSetCopyResult.Success)!.Id;
            _idCopy.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task Should_get_two_claimSet_from_get_all()
        {
            var result = await _repository.QueryClaimSet(new PagingQuery() { Limit = 25, Offset = 0 }, false);
            result.Should().BeOfType<ClaimSetQueryResult.Success>();

            ((ClaimSetQueryResult.Success)result).ClaimSetResponses.Count().Should().Be(2);
        }

        [Test]
        public async Task Should_get_claimSet_from_get_by_id()
        {
            var getByIdResult1 = await _repository.GetClaimSet(_id, false);
            getByIdResult1.Should().BeOfType<ClaimSetGetResult.Success>();

            object claimSetFromDb1 = ((ClaimSetGetResult.Success)getByIdResult1).ClaimSetResponse;
            claimSetFromDb1.Should().BeOfType<ClaimSetResponseReduced>();

            var reducedResponse1 = (ClaimSetResponseReduced)claimSetFromDb1;
            reducedResponse1.Name.Should().Be("Original-ClaimSet");

            var getByIdResult2 = await _repository.GetClaimSet(_idCopy, false);
            getByIdResult2.Should().BeOfType<ClaimSetGetResult.Success>();

            object claimSetFromDb2 = ((ClaimSetGetResult.Success)getByIdResult2).ClaimSetResponse;
            claimSetFromDb2.Should().BeOfType<ClaimSetResponseReduced>();

            var reducedResponse2 = (ClaimSetResponseReduced)claimSetFromDb2;
            reducedResponse2.Name.Should().Be("Copy-Test-ClaimSet");
        }
    }
}
