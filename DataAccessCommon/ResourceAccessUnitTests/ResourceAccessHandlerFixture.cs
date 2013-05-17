//-----------------------------------------------------------------------
// <copyright file="ResourceAccessHandlerFixture.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResourceAccess;
using Rhino.Mocks;

namespace ResourceAccessUnitTests
{
    /// <summary>
    /// Fixture to test ResourceAccessHandler
    /// </summary>
    [TestClass]
    public class ResourceAccessHandlerFixture
    {
        /// <summary>Uri string to get a single company by entityId.</summary>
        private readonly string getCompanyUri = "http://localhost/api/company/00000000000000000000000000000001";

        /// <summary>ResourceAccessHandler for testing.</summary>
        private ResourceAccessHandler accessHandler;

        /// <summary>List of access descriptors for granted user.</summary>
        private List<string> accessList;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.SetupUserRepositoryMock(new List<string>());
        }

        /// <summary>Access denied if user is not supplied.</summary>
        [TestMethod]
        public void CheckAccessUserRequired()
        {
            var canonicalResource = this.BuildCanonicalResource(this.getCompanyUri, "GET");
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, null));
        }

        /// <summary>User access list empty.</summary>
        [TestMethod]
        public void CheckAccessNoAccess()
        {
            var canonicalResource = this.BuildCanonicalResource(this.getCompanyUri, "GET");

            // Empty access list for user
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, new EntityId()));
        }

        /// <summary>Action denied.</summary>
        [TestMethod]
        public void CheckAccessActionDenied()
        {
            var canonicalResource = this.BuildCanonicalResource(this.getCompanyUri, "POST");
            this.SetupUserRepositoryMock("COMPANY:00000000000000000000000000000001:#:GET");

            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, new EntityId()));
        }

        /// <summary>Super-user access granted.</summary>
        [TestMethod]
        public void CheckAccessSuperUserGrant()
        {
            this.SetupUserRepositoryMock("*:#:*:*");
            var userEntityId = new EntityId();

            // Namespace/Id
            var canonicalResource = this.BuildCanonicalResource("http://localhost/api/company/00000000000000000000000000000001", "GET");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
            
            // Namespace only non api
            canonicalResource = this.BuildCanonicalResource("http://localhost/company.html", "GET");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Namespace only POST
            canonicalResource = this.BuildCanonicalResource("http://localhost/api/company", "POST");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Namespace and subnamespace/id
            canonicalResource = this.BuildCanonicalResource(
                "http://localhost/api/company/00000000000000000000000000000001/campaign/00000000000000000000000000000002",
                "PUT");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Namespace/id + message
            canonicalResource = this.BuildCanonicalResource(
                "http://localhost/api/company/00000000000000000000000000000001?MESSAGEFOO",
                "GET");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
            
            // No Action
            canonicalResource = this.BuildCanonicalResource("http://localhost/company", string.Empty);
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Empty query
            canonicalResource = this.BuildCanonicalResource("http://localhost/api/company?", "POST");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Empty resource action and query (dumb but no reason it should fail because of authZ)
            canonicalResource = this.BuildCanonicalResource("http://localhost/api?", string.Empty);
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Root path (which is not a meaningful scenario)
            canonicalResource = this.BuildCanonicalResource("http://localhost/", "GET");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
        }

        /// <summary>Grant in exact match scenarios.</summary>
        [TestMethod]
        public void CheckAccessExactMatchGrant()
        {
            var userEntityId = new EntityId();

            // Namespace/Id
            var canonicalResource = this.BuildCanonicalResource("http://localhost/api/company/00000000000000000000000000000001", "GET");
            this.SetupUserRepositoryMock("COMPANY:00000000000000000000000000000001:#:GET");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Namespace/id + subnamespace/id + message
            canonicalResource = this.BuildCanonicalResource(
                "http://localhost/api/company/00000000000000000000000000000001/campaign/00000000000000000000000000000002?MESSAGE=FOO",
                "GET");
            this.SetupUserRepositoryMock("COMPANY:00000000000000000000000000000001:CAMPAIGN:00000000000000000000000000000002:#:GET:FOO");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
        }

        /// <summary>Grant in partial wildcard scenarios.</summary>
        [TestMethod]
        public void CheckAccessWildcardGrant()
        {
            var userEntityId = new EntityId();
            
            // Namespace/id + subnamespace/id + message
            var canonicalResource = this.BuildCanonicalResource(
                "http://localhost/api/company/00000000000000000000000000000001/campaign/00000000000000000000000000000002?MESSAGEFOO",
                "GET");

            // Wildcard at subnamespace level works
            this.SetupUserRepositoryMock("COMPANY:00000000000000000000000000000001:CAMPAIGN:*:#:GET:*");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Wildcard at namespace level works
            this.SetupUserRepositoryMock("COMPANY:00000000000000000000000000000001:*:#:GET:*");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
        }

        /// <summary>Deny in exact match scenarios.</summary>
        [TestMethod]
        public void CheckAccessExactMatchDeny()
        {
            this.SetupUserRepositoryMock("COMPANY:90000000000000000000000000000000:*:#:GET");
            var userEntityId = new EntityId();

            // First make sure a matching resource is granted
            var goodCanonicalResource = this.BuildCanonicalResource("http://localhost/api/company/90000000000000000000000000000000", "GET");
            Assert.IsTrue(this.accessHandler.CheckAccess(goodCanonicalResource, userEntityId));

            // Namespace/Id mismatch denied
            var canonicalResource = this.BuildCanonicalResource("http://localhost/api/company/00000000000000000000000000000001", "GET");
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Namespace only denied
            canonicalResource = this.BuildCanonicalResource("http://localhost/api/company", "GET");
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Action mismatch denied
            canonicalResource = this.BuildCanonicalResource("http://localhost/api/company/90000000000000000000000000000000", "POST");
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // message denied
            canonicalResource = this.BuildCanonicalResource("http://localhost/api/company/90000000000000000000000000000000?MESSAGE=FOO", "GET");
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // No Action denied
            canonicalResource = this.BuildCanonicalResource("http://localhost/api/company/90000000000000000000000000000000", string.Empty);
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // File does not match.
            canonicalResource = this.BuildCanonicalResource("http://localhost/foo.html", "GET");
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Root does not match resources off root.
            this.SetupUserRepositoryMock("ROOT:#:GET");
            canonicalResource = this.BuildCanonicalResource("http://localhost/foo.html", "GET");
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Empty string for access descriptor fails for non-empty resource
            this.SetupUserRepositoryMock(":#:");
            Assert.IsFalse(this.accessHandler.CheckAccess(goodCanonicalResource, userEntityId));
        }

        /// <summary>Deny wildcard because of parent mismatch.</summary>
        [TestMethod]
        public void CheckAccessDenyParentMismatchWithWildcard()
        {
            var userEntityId = new EntityId();

            // Namespace/id + subnamespace/id + message
            var canonicalResource = this.BuildCanonicalResource(
                "http://localhost/api/company/00000000000000000000000000000001/campaign/00000000000000000000000000000002?MESSAGEFOO",
                "GET");
            this.SetupUserRepositoryMock("COMPANY:00000000000000000000000000000009:CAMPAIGN:*:#:GET:*");
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
        }

        /// <summary>Grant wildcard access to files in a directory.</summary>
        [TestMethod]
        public void CheckAccessGrantDirectory()
        {
            // File not match resources off root.
            this.SetupUserRepositoryMock("STATIC:*:#:GET");
            var canonicalResource = this.BuildCanonicalResource("http://localhost/static/foo.html", "GET");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, new EntityId()));
        }
        
        /// <summary>Succeed with multiple access descriptors.</summary>
        [TestMethod]
        public void CheckAccessMultiple()
        {
            var userEntityId = new EntityId();

            // Namespace/id + subnamespace/id + message
            var canonicalResource = this.BuildCanonicalResource(
                "http://localhost/api/company/00000000000000000000000000000001/campaign/00000000000000000000000000000002?MESSAGEFOO",
                "GET");

            // Should be denied by this accessDescriptor
            var accessListIn = new List<string>
                {
                    "COMPANY:90000000000000000000000000000000:CAMPAIGN:*:#:GET:*",
                };
            this.SetupUserRepositoryMock(accessListIn);
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // Should be granted by the second access descriptor in the list
            accessListIn = new List<string>
                {
                    "COMPANY:90000000000000000000000000000000:CAMPAIGN:*:#:GET:*",
                    "COMPANY:00000000000000000000000000000001:CAMPAIGN:*:#:GET:*",
                };
            this.SetupUserRepositoryMock(accessListIn);
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
        }

        /// <summary>Degenerate succeess scenarios - no action.</summary>
        [TestMethod]
        public void CheckAccessDegenerateNoActionOk()
        {
            this.SetupUserRepositoryMock("COMPANY:*:#:");
            var canonicalResource = this.BuildCanonicalResource("http://localhost/company", string.Empty);
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, new EntityId()));
        }

        /// <summary>Degenerate success scenarios - no query.</summary>
        [TestMethod]
        public void CheckAccessDegenerateNoQueryOk()
        {
            this.SetupUserRepositoryMock("COMPANY:*:#:POST");
            var canonicalResource = this.BuildCanonicalResource("http://localhost/api/company?", "POST");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, new EntityId()));
        }

        /// <summary>
        /// Degenerate success scenarios - no resource with access descriptor that correctly describes "nothing".
        /// Dumb but AuthZ isn't shouldn't blow up.
        /// </summary>
        [TestMethod]
        public void CheckAccessDegenerateEmptyRequestAndAccessDescriptorOk()
        {
            var canonicalResource = this.BuildCanonicalResource("http://localhost/api?", string.Empty);
            var userEntityId = new EntityId();

            // Empty delimiter access descriptor
            this.SetupUserRepositoryMock("*::#::");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // root access descriptor
            this.SetupUserRepositoryMock("*:#:");
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));

            // However, this should not work for an empty access list
            this.SetupUserRepositoryMock(new List<string>());
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
        }

        /// <summary>Deny if subnamespace resource is not a child association of the namespace resource.</summary>
        [TestMethod]
        public void CheckAccessFailSubNamespaceResourceNotChildOfNamespaceResource()
        {
            var userEntityId = new EntityId();

            // Namespace/id + subnamespace/id + message
            var canonicalResource = this.BuildCanonicalResource(
                "http://localhost/api/company/00000000000000000000000000000001/campaign/00000000000000000000000000000004", "GET");
            this.SetupUserRepositoryMock("COMPANY:00000000000000000000000000000001:CAMPAIGN:*:#:GET:*");
            Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
        }
        
        /// <summary>Deny if subnamespace resource is not of association type child.</summary>
        [TestMethod]
        public void CheckAccessSubNamespaceResourceMustBeAssociationOfTypeChild()
        {
            var userEntityId = new EntityId();

            // Namespace/id + subnamespace/id + message
            var canonicalResource = this.BuildCanonicalResource(
                "http://localhost/api/company/00000000000000000000000000000001/campaign/00000000000000000000000000000003", "GET");
            this.SetupUserRepositoryMock("COMPANY:00000000000000000000000000000001:CAMPAIGN:*:#:GET:*");

            // TODO: Change this back when we use child associations correctly.
            // Assert.IsFalse(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
        }
        
        /// <summary>Agency User scenario. The authorizing parent is not in the uri and we must search from the
        /// access list.</summary>
        [TestMethod]
        public void CheckAccessParentAuthorityNotInUri()
        {
            var userEntityId = new EntityId();

            // Namespace/id + subnamespace/id + message
            var canonicalResource = this.BuildCanonicalResource(
                "http://localhost/api/company/00000000000000000000000000000001/campaign/00000000000000000000000000000004", "GET");

            var agencyId = new EntityId("90000000000000000000000000000000");
            var agencyEntity = this.BuildCompanyEntityWithAssociations(
                            agencyId,
                            "00000000000000000000000000000001",
                            CompanyEntity.CompanyEntityCategory,
                            AssociationType.Child,
                            "00000000000000000000000000000003",
                            PartnerEntity.PartnerEntityCategory,
                            AssociationType.Relationship);

            var advertiserId = new EntityId("00000000000000000000000000000001");
            var advertiserEntity = this.BuildCompanyEntityWithAssociations(
                            advertiserId,
                            "00000000000000000000000000000004",
                            CampaignEntity.CampaignEntityCategory,
                            AssociationType.Child,
                            "00000000000000000000000000000003",
                            PartnerEntity.PartnerEntityCategory,
                            AssociationType.Relationship);

            var repositoryStub = MockRepository.GenerateStub<IEntityRepository>();
            repositoryStub.Stub(f => f.GetEntity(Arg<RequestContext>.Is.Anything, Arg<EntityId>.Matches(a => a == agencyId))).Return(agencyEntity);
            repositoryStub.Stub(f => f.GetEntity(Arg<RequestContext>.Is.Anything, Arg<EntityId>.Matches(a => a == advertiserId))).Return(advertiserEntity);

            // Set up access descriptor for an agency user
            var parentAccessList = new List<string>
                {
                    "COMPANY:90000000000000000000000000000000:*:#:*:*"
                };
            this.SetupUserRepositoryMock(parentAccessList, repositoryStub);

            Assert.IsTrue(this.accessHandler.CheckAccess(canonicalResource, userEntityId));
        }

        /// <summary>File granted</summary>
        [TestMethod]
        public void CheckGlobalAccessFile()
        {
            var canonicalResource = this.BuildCanonicalResource("http://localhost/userverification.html?00000000000000000000000000000001", "GET");
            Assert.IsTrue(this.accessHandler.CheckGlobalAccess(canonicalResource));
        }

        /// <summary>Subdirectory granted</summary>
        [TestMethod]
        public void CheckGlobalAccessSuccess()
        {
            var canonicalResource = this.BuildCanonicalResource("http://localhost/css/foo", "GET");
            Assert.IsTrue(this.accessHandler.CheckGlobalAccess(canonicalResource));
        }

        /// <summary>Non-Root uri denied for root access.</summary>
        [TestMethod]
        public void CheckGlobalAccessRootAccessDenyForNonRootUri()
        {
            // now we allow access to all *.html so the user doesn't need every web page added to the user's access list.
            var canonicalResource = this.BuildCanonicalResource("http://localhost/foo.html", "GET");
            Assert.IsTrue(this.accessHandler.CheckGlobalAccess(canonicalResource));
        }

        /// <summary>Root uri granted</summary>
        [TestMethod]
        public void CheckGlobalAccessRootUri()
        {
            var canonicalResource = this.BuildCanonicalResource("http://localhost/", "GET");
            Assert.IsTrue(this.accessHandler.CheckGlobalAccess(canonicalResource));
        }

        /// <summary>Setup the user access repository stub with a modified access list.</summary>
        /// <param name="accessListIn">The access list.</param>
        /// <param name="repositoryStub">The repository stub.</param>
        private void SetupUserRepositoryMock(List<string> accessListIn, IEntityRepository repositoryStub)
        {
            // user access repository stub
            this.accessList = accessListIn;
            var userAccessStub = MockRepository.GenerateStub<IUserAccessRepository>();
            userAccessStub.Stub(f => f.GetUserAccessList(null)).IgnoreArguments().Return(this.accessList);
            this.accessHandler = new ResourceAccessHandler(userAccessStub, repositoryStub);
        }

        /// <summary>Setup the user access repository stub with a modified access list.</summary>
        /// <param name="accessListIn">The access list.</param>
        private void SetupUserRepositoryMock(List<string> accessListIn)
        {
            var companyEntity = this.BuildCompanyEntityWithAssociations(
                            new EntityId(),
                            "00000000000000000000000000000002",
                            CampaignEntity.CampaignEntityCategory,
                            AssociationType.Child,
                            "00000000000000000000000000000003", 
                            PartnerEntity.PartnerEntityCategory, 
                            AssociationType.Relationship);

            var repositoryStub = MockRepository.GenerateStub<IEntityRepository>();
            repositoryStub.Stub(f => f.GetEntity(null, null)).IgnoreArguments().Return(companyEntity);

            this.SetupUserRepositoryMock(accessListIn, repositoryStub);
        }

        /// <summary>Setup the user access repository stub with a modified access list.</summary>
        /// <param name="accessDescriptor">The access Descriptor.</param>
        private void SetupUserRepositoryMock(string accessDescriptor)
        {
            var accessListIn = new List<string>
                {
                    accessDescriptor
                };

            this.SetupUserRepositoryMock(accessListIn);
        }

        /// <summary>Build a company entity with two associations.</summary>
        /// <param name="entityId">The entity id.</param>
        /// <param name="target1Id">The target 1 id.</param>
        /// <param name="target1Category">The target 1 category.</param>
        /// <param name="target1AssociationType">The target 1 association type.</param>
        /// <param name="target2Id">The target 2 id.</param>
        /// <param name="target2Category">The target 2 category.</param>
        /// <param name="target2AssociationType">The target 2 association type.</param>
        /// <returns>The company entity.</returns>
        private CompanyEntity BuildCompanyEntityWithAssociations(
            string entityId, 
            string target1Id,
            string target1Category,
            AssociationType target1AssociationType,
            string target2Id,
            string target2Category,
            AssociationType target2AssociationType)
        {
            return new CompanyEntity(
                            entityId,
                            new Entity
                            {
                                Associations = 
                                {
                                    new Association
                                        {
                                            TargetEntityId = new EntityId(target1Id),
                                            TargetEntityCategory = target1Category,
                                            AssociationType = target1AssociationType
                                        },
                                    new Association
                                        {
                                            TargetEntityId = new EntityId(target2Id), 
                                            TargetEntityCategory = target2Category, 
                                            AssociationType = target2AssociationType
                                        },
                                }
                            });
        }

        /// <summary>Helper to build a canonical resource for testing.</summary>
        /// <param name="resource">The resource uri string.</param>
        /// <param name="action">The action.</param>
        /// <returns>A canonical resource object.</returns>
        private CanonicalResource BuildCanonicalResource(string resource, string action)
        {
            // TODO: Remove this once service namespaces are removed
            resource = resource.Replace("/api", "/api/entity");
            var resourceUri = new Uri(resource, UriKind.Absolute);

            return new CanonicalResource(resourceUri, action);
        }
    }
}
