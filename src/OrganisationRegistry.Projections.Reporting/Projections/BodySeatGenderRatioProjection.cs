namespace OrganisationRegistry.Projections.Reporting.Projections
{
    using Autofac.Features.OwnedInstances;
    using Body.Events;
    using Infrastructure;
    using LifecyclePhaseType;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Organisation.Events;
    using Person.Events;
    using SqlServer.Infrastructure;
    using SqlServer.Reporting;
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using OrganisationRegistry.Infrastructure.Events;

    public class BodySeatGenderRatioProjection :
        Projection<BodySeatGenderRatioProjection>,

        IEventHandler<OrganisationCreated>,
        IEventHandler<OrganisationCreatedFromKbo>,
        IEventHandler<OrganisationInfoUpdated>,
        IEventHandler<OrganisationInfoUpdatedFromKbo>,

        IEventHandler<OrganisationBecameActive>,
        IEventHandler<OrganisationBecameInactive>,

        IEventHandler<PersonCreated>,
        IEventHandler<PersonUpdated>,

        IEventHandler<BodyRegistered>,
        IEventHandler<BodyInfoChanged>,

        IEventHandler<BodyLifecyclePhaseAdded>,
        IEventHandler<BodyLifecyclePhaseUpdated>,

        IEventHandler<BodySeatAdded>,
        IEventHandler<BodySeatUpdated>,

        IEventHandler<AssignedPersonToBodySeat>,
        IEventHandler<ReassignedPersonToBodySeat>,

        IEventHandler<AssignedOrganisationToBodySeat>,
        IEventHandler<ReassignedOrganisationToBodySeat>,

        IEventHandler<AssignedFunctionTypeToBodySeat>,
        IEventHandler<ReassignedFunctionTypeToBodySeat>,

        IEventHandler<PersonAssignedToDelegation>,
        IEventHandler<PersonAssignedToDelegationUpdated>,
        IEventHandler<PersonAssignedToDelegationRemoved>,

        IEventHandler<BodyAssignedToOrganisation>,
        IEventHandler<BodyClearedFromOrganisation>,

        IEventHandler<OrganisationOrganisationClassificationAdded>,
        IEventHandler<KboLegalFormOrganisationOrganisationClassificationAdded>,
        IEventHandler<OrganisationOrganisationClassificationUpdated>,

        IEventHandler<InitialiseProjection>
    {
        private readonly Func<Owned<OrganisationRegistryContext>> _contextFactory;

        public BodySeatGenderRatioProjection(
            ILogger<BodySeatGenderRatioProjection> logger,
            Func<Owned<OrganisationRegistryContext>> contextFactory) : base(logger)
        {
            _contextFactory = contextFactory;
        }

        public override string[] ProjectionTableNames =>
            new[]
            {
                BodySeatGenderRatioAssignmentListConfiguration.TableName,
                BodySeatGenderRatioBodyListConfiguration.TableName,
                BodySeatGenderRatioLifecyclePhaseValidityListConfiguration.TableName,
                BodySeatGenderRatioBodyMandateListConfiguration.TableName,
                BodySeatGenderRatioOrganisationClassificationListConfiguration.TableName,
                BodySeatGenderRatioOrganisationListConfiguration.TableName,
                BodySeatGenderRatioOrganisationPerBodyListConfiguration.TableName,
                BodySeatGenderRatioPersonListConfiguration.TableName,
                BodySeatGenderRatioPostsPerTypeListConfiguration.TableName
            };

        public override void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<RebuildProjection> message)
        {
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationCreated> message)
        {
            CacheOrganisationName(message.Body.OrganisationId, message.Body.Name);
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationCreatedFromKbo> message)
        {
            CacheOrganisationName(message.Body.OrganisationId, message.Body.Name);
        }

        private void CacheOrganisationName(Guid organisationId, string organisationName)
        {
            using (var context = _contextFactory().Value)
            {
                //organisation cache

                var body = new BodySeatGenderRatioOrganisationListItem
                {
                    OrganisationId = organisationId,
                    OrganisationName = organisationName
                };

                context.BodySeatGenderRatioOrganisationList.Add(body);

                context.SaveChanges();
            }
        }


        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationInfoUpdated> message)
        {
            UpdateOrganisationName(message.Body.OrganisationId, message.Body.Name);
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationInfoUpdatedFromKbo> message)
        {
            UpdateOrganisationName(message.Body.OrganisationId, message.Body.Name);
        }

        /// <summary>
        /// Update organisation cache, organisation per body cache and affected records in projection (organisation name)
        /// </summary>
        private void UpdateOrganisationName(Guid organisationId, string organisationName)
        {
            using (var context = _contextFactory().Value)
            {
                context
                    .BodySeatGenderRatioOrganisationList
                    .Where(item => item.OrganisationId == organisationId)
                    .ToList()
                    .ForEach(item => { item.OrganisationName = organisationName; });

                context
                    .BodySeatGenderRatioOrganisationPerBodyList
                    .Where(item => item.OrganisationId == organisationId)
                    .ToList()
                    .ForEach(item => { item.OrganisationName = organisationName; });

                context
                    .BodySeatGenderRatioBodyList
                    .Where(item => item.OrganisationId == organisationId)
                    .ToList()
                    .ForEach(item => { item.OrganisationName = organisationName; });

                context.SaveChanges();
            }
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationBecameActive> message)
        {
            using (var context = _contextFactory().Value)
            {
                context.BodySeatGenderRatioOrganisationList
                    .Where(item => item.OrganisationId == message.Body.OrganisationId)
                    .ToList()
                    .ForEach(item =>
                    {
                        item.OrganisationActive = true;
                    });

                context.BodySeatGenderRatioOrganisationPerBodyList
                    .Where(item => item.OrganisationId == message.Body.OrganisationId)
                    .ToList()
                    .ForEach(item =>
                    {
                        item.OrganisationActive = true;
                    });

                context.BodySeatGenderRatioBodyList
                    .Where(item => item.OrganisationId == message.Body.OrganisationId)
                    .ToList()
                    .ForEach(item =>
                    {
                        item.OrganisationIsActive = true;
                    });

                context.SaveChanges();
            }
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationBecameInactive> message)
        {
            using (var context = _contextFactory().Value)
            {
                context.BodySeatGenderRatioOrganisationList
                    .Where(item => item.OrganisationId == message.Body.OrganisationId)
                    .ToList()
                    .ForEach(item =>
                    {
                        item.OrganisationActive = false;
                    });

                context.BodySeatGenderRatioOrganisationPerBodyList
                    .Where(item => item.OrganisationId == message.Body.OrganisationId)
                    .ToList()
                    .ForEach(item =>
                    {
                        item.OrganisationActive = false;
                    });

                context.BodySeatGenderRatioBodyList
                    .Where(item => item.OrganisationId == message.Body.OrganisationId)
                    .ToList()
                    .ForEach(item =>
                    {
                        item.OrganisationIsActive = false;
                    });

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Cache person (person id, person fullname, person sex)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<PersonCreated> message)
        {
            using (var context = _contextFactory().Value)
            {
                var person = new BodySeatGenderRatioPersonListItem
                {
                    PersonId = message.Body.PersonId,
                    PersonSex = message.Body.Sex
                };

                context.BodySeatGenderRatioPersonList.Add(person);

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update person cache and affected records in projection (person fullname, person sex)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<PersonUpdated> message)
        {
            using (var context = _contextFactory().Value)
            {
                var person =
                    context
                        .BodySeatGenderRatioPersonList
                        .Single(x => x.PersonId == message.Body.PersonId);

                person.PersonSex = message.Body.Sex;

                context
                    .BodySeatGenderRatioBodyMandateList
                    .Include(mandate => mandate.Assignments)
                    .SelectMany(mandate => mandate.Assignments)
                    .Where(x => x.PersonId == message.Body.PersonId)
                    .ToList()
                    .ForEach(item =>
                    {
                        item.Sex = message.Body.Sex;
                    });

                context.SaveChanges();
            }
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<BodyRegistered> message)
        {
            using (var context = _contextFactory().Value)
            {
                var organisation = GetOrganisationForBodyFromCache(context, message.Body.BodyId);

                var item = new BodySeatGenderRatioBodyItem
                {
                    BodyId = message.Body.BodyId,
                    BodyName = message.Body.Name,

                    OrganisationId = organisation?.OrganisationId,
                    OrganisationName = organisation?.OrganisationName ?? string.Empty,
                    OrganisationIsActive = organisation?.OrganisationId != null &&
                                           (GetOrganisationFromCache(context, organisation.OrganisationId.Value)
                                                ?.OrganisationActive ?? false),

                    LifecyclePhaseValidities = new List<BodySeatGenderRatioBodyLifecyclePhaseValidityItem>(),
                    PostsPerType = new List<BodySeatGenderRatioPostsPerTypeItem>(),
                };

                context.BodySeatGenderRatioBodyList.Add(item);

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (body name)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<BodyInfoChanged> message)
        {
            using (var context = _contextFactory().Value)
            {
                context
                    .BodySeatGenderRatioBodyList
                    .Where(x => x.BodyId == message.Body.BodyId)
                    .ToList()
                    .ForEach(item =>
                    {
                        item.BodyName = message.Body.Name;
                    });

                context.SaveChanges();
            }
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<BodyLifecyclePhaseAdded> message)
        {
            using (var context = _contextFactory().Value)
            {
                var items = context
                    .BodySeatGenderRatioBodyList
                    .Include(item => item.LifecyclePhaseValidities)
                    .Where(item => item.BodyId == message.Body.BodyId)
                    .ToList();

                foreach (var item in items)
                {
                    var lifecycle = item.LifecyclePhaseValidities.SingleOrDefault(x =>
                        x.LifecyclePhaseId == message.Body.BodyLifecyclePhaseId);
                    if (lifecycle != null)
                    {
                        item.LifecyclePhaseValidities.Remove(lifecycle);

                        context.SaveChanges();
                    }

                    var newLifecycle = new BodySeatGenderRatioBodyLifecyclePhaseValidityItem
                    {
                        LifecyclePhaseId = message.Body.BodyLifecyclePhaseId,
                        BodyId = message.Body.BodyId,
                        ValidFrom = message.Body.ValidFrom,
                        ValidTo = message.Body.ValidTo,
                        RepresentsActivePhase = message.Body.LifecyclePhaseTypeIsRepresentativeFor ==
                                                LifecyclePhaseTypeIsRepresentativeFor.ActivePhase
                    };

                    item.LifecyclePhaseValidities.Add(newLifecycle);

                    context.SaveChanges();
                }
            }
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<BodyLifecyclePhaseUpdated> message)
        {
            using (var context = _contextFactory().Value)
            {
                var bodySeatGenderRatioBodyItem = context
                    .BodySeatGenderRatioBodyList
                    .Include(item => item.LifecyclePhaseValidities)
                    .Single(item => item.BodyId == message.Body.BodyId);

                var lifecyclePhaseValidity =
                    bodySeatGenderRatioBodyItem
                    .LifecyclePhaseValidities
                    .Single(validity => validity.LifecyclePhaseId == message.Body.BodyLifecyclePhaseId);

                lifecyclePhaseValidity.ValidFrom = message.Body.ValidFrom;
                lifecyclePhaseValidity.ValidTo = message.Body.ValidTo;
                lifecyclePhaseValidity.RepresentsActivePhase =
                    message.Body.LifecyclePhaseTypeIsRepresentativeFor == LifecyclePhaseTypeIsRepresentativeFor.ActivePhase;

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Create new projection record (body, bodyseat, bodyseattype, entitledtovote)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<BodySeatAdded> message)
        {
            using (var context = _contextFactory().Value)
            {
                var body =
                    context.BodySeatGenderRatioBodyList
                        .Include(x => x.PostsPerType)
                        .Single(bodyItem => bodyItem.BodyId == message.Body.BodyId);

                var item = new BodySeatGenderRatioPostsPerTypeItem()
                {
                    BodyId = message.Body.BodyId,
                    BodySeatId = message.Body.BodySeatId,
                    EntitledToVote = message.Body.EntitledToVote,
                    BodySeatValidFrom = message.Body.ValidFrom,
                    BodySeatValidTo = message.Body.ValidTo,

                    BodySeatTypeId = message.Body.SeatTypeId,
                    BodySeatTypeName = message.Body.SeatTypeName,
                };

                body.PostsPerType.Add(item);

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (bodyseat status, bodyseattype, entitledtovote)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<BodySeatUpdated> message)
        {
            using (var context = _contextFactory().Value)
            {
                var postsPerType = context
                    .BodySeatGenderRatioBodyList
                    .Include(item => item.PostsPerType)
                    .Single(item => item.BodyId == message.Body.BodyId)
                    .PostsPerType;

                var posts = postsPerType.Where(x => x.BodySeatId == message.Body.BodySeatId);

                foreach (var post in posts)
                {
                    post.EntitledToVote = message.Body.EntitledToVote;
                    post.BodySeatValidFrom = message.Body.ValidFrom;
                    post.BodySeatValidTo = message.Body.ValidTo;

                    post.BodySeatTypeId = message.Body.SeatTypeId;
                    post.BodySeatTypeName = message.Body.SeatTypeName;

                    context
                        .BodySeatGenderRatioBodyMandateList
                        .Where(mandate => mandate.BodySeatId == message.Body.BodySeatId)
                        .ToList()
                        .ForEach(mandate =>
                            mandate.BodySeatTypeId = message.Body.SeatTypeId);
                }

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (person id, person fullname, person sex, person assigned status + assigned status + refresh person from cached data)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<AssignedPersonToBodySeat> message)
        {
            using (var context = _contextFactory().Value)
            {
                var bodySeatTypeId =
                    context
                        .BodySeatGenderRatioBodyList
                        .Include(item => item.PostsPerType)
                        .Single(item => item.BodyId == message.Body.BodyId)
                        .PostsPerType
                        .First(item => item.BodySeatId == message.Body.BodySeatId)
                        .BodySeatTypeId;

                var bodyMandate = new BodySeatGenderRatioBodyMandateItem
                {
                    BodyMandateId = message.Body.BodyMandateId,

                    BodyMandateValidFrom = message.Body.ValidFrom,
                    BodyMandateValidTo = message.Body.ValidTo,

                    BodyId = message.Body.BodyId,

                    BodySeatId = message.Body.BodySeatId,

                    BodySeatTypeId = bodySeatTypeId,

                    Assignments = new List<BodySeatGenderRatioAssignmentItem>()
                };

                var personFromCache = GetPersonFromCache(context, message.Body.PersonId);
                var assignment = new BodySeatGenderRatioAssignmentItem
                {
                    BodyMandateId = message.Body.BodyMandateId,

                    DelegationAssignmentId = null,

                    AssignmentValidFrom = message.Body.ValidFrom,
                    AssignmentValidTo = message.Body.ValidTo,

                    PersonId = message.Body.PersonId,
                    Sex = personFromCache.Sex
                };

                bodyMandate.Assignments.Add(assignment);

                context.BodySeatGenderRatioBodyMandateList.Add(bodyMandate);

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (person id, person fullname, person sex, person assigned statusassigned status +  + refresh person from cached data)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<ReassignedPersonToBodySeat> message)
        {
            //called on update mandate
            using (var context = _contextFactory().Value)
            {
                var bodyMandate =
                    context
                        .BodySeatGenderRatioBodyMandateList
                        .Include(mandate => mandate.Assignments)
                        .Single(x => x.BodyMandateId == message.Body.BodyMandateId);

                if (!message.Body.BodySeatId.Equals(message.Body.PreviousBodySeatId))
                {
                    var bodySeatTypeId =
                        context
                            .BodySeatGenderRatioBodyList
                            .Include(item => item.PostsPerType)
                            .Single(item => item.BodyId == message.Body.BodyId)
                            .PostsPerType
                            .First(item => item.BodySeatId == message.Body.BodySeatId)
                            .BodySeatTypeId;

                    bodyMandate.BodySeatId = message.Body.BodySeatId;
                    bodyMandate.BodySeatTypeId = bodySeatTypeId;
                }

                bodyMandate.BodyMandateValidFrom = message.Body.ValidFrom;
                bodyMandate.BodyMandateValidTo = message.Body.ValidTo;

                var assignment = bodyMandate.Assignments.First();
                assignment.AssignmentValidFrom = message.Body.ValidFrom;
                assignment.AssignmentValidTo = message.Body.ValidTo;
                assignment.PersonId = message.Body.PersonId;
                assignment.Sex = GetPersonFromCache(context, message.Body.PersonId).Sex;

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (organisation assigned status + assigned status + refresh organisation from cached data)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<AssignedOrganisationToBodySeat> message)
        {
            using (var context = _contextFactory().Value)
            {
                var bodySeatTypeId =
                    context
                        .BodySeatGenderRatioBodyList
                        .Include(item => item.PostsPerType)
                        .Single(item => item.BodyId == message.Body.BodyId)
                        .PostsPerType
                        .First(item => item.BodySeatId == message.Body.BodySeatId)
                        .BodySeatTypeId;

                var bodyMandate = new BodySeatGenderRatioBodyMandateItem
                {
                    BodyMandateId = message.Body.BodyMandateId,

                    BodyMandateValidFrom = message.Body.ValidFrom,
                    BodyMandateValidTo = message.Body.ValidTo,

                    BodyId = message.Body.BodyId,

                    BodySeatId = message.Body.BodySeatId,

                    BodySeatTypeId = bodySeatTypeId,

                    Assignments = new List<BodySeatGenderRatioAssignmentItem>()
                };

                context.BodySeatGenderRatioBodyMandateList.Add(bodyMandate);

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (organisation assigned status + assigned status + refresh organisation from cached data)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<ReassignedOrganisationToBodySeat> message)
        {
            using (var context = _contextFactory().Value)
            {
                var item =
                    context
                        .BodySeatGenderRatioBodyMandateList
                        .Single(x => x.BodyMandateId == message.Body.BodyMandateId);

                if (!message.Body.BodySeatId.Equals(message.Body.PreviousBodySeatId))
                {
                    var bodySeatTypeId =
                        context
                            .BodySeatGenderRatioBodyList
                            .Include(x => x.PostsPerType)
                            .Single(x => x.BodyId == message.Body.BodyId)
                            .PostsPerType
                            .First(x => x.BodySeatId == message.Body.BodySeatId)
                            .BodySeatTypeId;

                    item.BodySeatId = message.Body.BodySeatId;
                    item.BodySeatTypeId = bodySeatTypeId;
                }

                item.BodyMandateValidFrom = message.Body.ValidFrom;
                item.BodyMandateValidTo = message.Body.ValidTo;

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (function assigned status + assigned status + refresh organisation from cached data)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<AssignedFunctionTypeToBodySeat> message)
        {
            using (var context = _contextFactory().Value)
            {
                var bodySeatTypeId =
                    context
                        .BodySeatGenderRatioBodyList
                        .Include(item => item.PostsPerType)
                        .Single(item => item.BodyId == message.Body.BodyId)
                        .PostsPerType
                        .First(item => item.BodySeatId == message.Body.BodySeatId)
                        .BodySeatTypeId;

                var bodyMandate = new BodySeatGenderRatioBodyMandateItem
                {
                    BodyMandateId = message.Body.BodyMandateId,

                    BodyMandateValidFrom = message.Body.ValidFrom,
                    BodyMandateValidTo = message.Body.ValidTo,

                    BodyId = message.Body.BodyId,

                    BodySeatId = message.Body.BodySeatId,
                    BodySeatTypeId = bodySeatTypeId,

                    Assignments = new List<BodySeatGenderRatioAssignmentItem>()
                };

                context.BodySeatGenderRatioBodyMandateList.Add(bodyMandate);

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (function assigned status + assigned status + refresh organisation from cached data)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<ReassignedFunctionTypeToBodySeat> message)
        {
            using (var context = _contextFactory().Value)
            {
                var item =
                    context
                        .BodySeatGenderRatioBodyMandateList
                        .Single(x => x.BodyMandateId == message.Body.BodyMandateId);

                if (!message.Body.BodySeatId.Equals(message.Body.PreviousBodySeatId))
                {
                    var bodySeatTypeId =
                        context
                            .BodySeatGenderRatioBodyList
                            .Include(x => x.PostsPerType)
                            .Single(x => x.BodyId == message.Body.BodyId)
                            .PostsPerType
                            .First(x => x.BodySeatId == message.Body.BodySeatId)
                            .BodySeatTypeId;

                    item.BodySeatId = message.Body.BodySeatId;
                    item.BodySeatTypeId = bodySeatTypeId;
                }

                item.BodyMandateValidFrom = message.Body.ValidFrom;
                item.BodyMandateValidTo = message.Body.ValidTo;

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (person id, person fullname, person sex, person assigned status + assigned status)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<PersonAssignedToDelegation> message)
        {
            using (var context = _contextFactory().Value)
            {
                var bodyMandate =
                    context
                        .BodySeatGenderRatioBodyMandateList
                        .Include(mandate => mandate.Assignments)
                        .Single(item => item.BodyMandateId == message.Body.BodyMandateId);

                var personFromCache = GetPersonFromCache(context, message.Body.PersonId);
                bodyMandate.Assignments.Add(new BodySeatGenderRatioAssignmentItem
                {
                    BodyMandateId = message.Body.BodyMandateId,

                    DelegationAssignmentId = message.Body.DelegationAssignmentId,

                    AssignmentValidFrom = message.Body.ValidFrom,
                    AssignmentValidTo = message.Body.ValidTo,

                    PersonId = message.Body.PersonId,
                    Sex = personFromCache.Sex
                });

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (person id, person fullname, person sex, person assigned status + assigned status)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<PersonAssignedToDelegationUpdated> message)
        {
            using (var context = _contextFactory().Value)
            {
                var bodyMandate =
                    context
                        .BodySeatGenderRatioBodyMandateList
                        .Include(mandate => mandate.Assignments)
                        .Single(item => item.BodyMandateId == message.Body.BodyMandateId);

                var assignment =
                    bodyMandate
                        .Assignments
                        .Single(item => item.DelegationAssignmentId == message.Body.DelegationAssignmentId);

                assignment.AssignmentValidFrom = message.Body.ValidFrom;
                assignment.AssignmentValidTo = message.Body.ValidTo;

                assignment.PersonId = message.Body.PersonId;
                assignment.Sex = GetPersonFromCache(context, message.Body.PersonId).Sex;

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (person assigned status + assigned status)
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<PersonAssignedToDelegationRemoved> message)
        {
            using (var context = _contextFactory().Value)
            {
                var item = context
                    .BodySeatGenderRatioBodyMandateList
                    .Include(mandate => mandate.Assignments)
                    .Single(x =>
                        x.BodyMandateId == message.Body.BodyMandateId);

                var assignment = item
                    .Assignments
                    .Single(assignmentItem =>
                        assignmentItem.DelegationAssignmentId == message.Body.DelegationAssignmentId);

                item.Assignments.Remove(assignment);

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection (organisation id, organisation name, organisation assigned status) + assigned status
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<BodyAssignedToOrganisation> message)
        {
            using (var context = _contextFactory().Value)
            {
                var cachedOrganisation = GetOrganisationFromCache(context, message.Body.OrganisationId);

                //organisation per body cache
                var body = new BodySeatGenderRatioOrganisationPerBodyListItem
                {
                    BodyId = message.Body.BodyId,
                    BodyOrganisationId = message.Body.BodyOrganisationId,
                    OrganisationId = message.Body.OrganisationId,
                    OrganisationName = message.Body.OrganisationName,
                    OrganisationActive = cachedOrganisation?.OrganisationActive ?? false
                };

                context.BodySeatGenderRatioOrganisationPerBodyList.Add(body);

                context.BodySeatGenderRatioBodyList
                    .Where(post => post.BodyId == message.Body.BodyId)
                    .ToList()
                    .ForEach(post =>
                    {
                        post.OrganisationId = message.Body.OrganisationId;
                        post.OrganisationName = message.Body.OrganisationName;
                        post.OrganisationIsActive = cachedOrganisation?.OrganisationActive ?? false;
                    });

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Update affected records in projection organisation assigned status + assigned status
        /// </summary>
        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<BodyClearedFromOrganisation> message)
        {
            using (var context = _contextFactory().Value)
            {
                var body = context
                    .BodySeatGenderRatioOrganisationPerBodyList
                    .SingleOrDefault(x => x.BodyId == message.Body.BodyId);

                if (body == null)
                    return;

                context.BodySeatGenderRatioOrganisationPerBodyList.Remove(body);

                context.BodySeatGenderRatioBodyList
                    .Where(post => post.BodyId == message.Body.BodyId)
                    .ToList()
                    .ForEach(post =>
                    {
                        post.OrganisationId = null;
                        post.OrganisationName = string.Empty;
                        post.OrganisationIsActive = false;
                    });

                context.SaveChanges();
            }
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationOrganisationClassificationAdded> message)
        {
            AddOrganisationClassification(message.Body.OrganisationOrganisationClassificationId, message.Body.OrganisationId, message.Body.OrganisationClassificationId, message.Body.OrganisationClassificationTypeId, message.Body.ValidFrom, message.Body.ValidTo);
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<KboLegalFormOrganisationOrganisationClassificationAdded> message)
        {
            AddOrganisationClassification(message.Body.OrganisationOrganisationClassificationId, message.Body.OrganisationId, message.Body.OrganisationClassificationId, message.Body.OrganisationClassificationTypeId, message.Body.ValidFrom, message.Body.ValidTo);
        }

        private void AddOrganisationClassification(Guid organisationOrganisationClassificationId, Guid organisationId, Guid organisationClassificationId, Guid organisationClassificationTypeId, DateTime? validFrom, DateTime? validTo)
        {
            using (var context = _contextFactory().Value)
            {
                context.BodySeatGenderRatioOrganisationClassificationList.Add(
                    new BodySeatGenderRatioOrganisationClassificationItem
                    {
                        OrganisationOrganisationClassificationId = organisationOrganisationClassificationId,

                        OrganisationId = organisationId,
                        OrganisationClassificationId = organisationClassificationId,
                        OrganisationClassificationTypeId = organisationClassificationTypeId,

                        ClassificationValidFrom = validFrom,
                        ClassificationValidTo = validTo
                    });

                context.SaveChanges();
            }
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationOrganisationClassificationUpdated> message)
        {
            using (var context = _contextFactory().Value)
            {
                var item = context.BodySeatGenderRatioOrganisationClassificationList.Single(x =>
                    x.OrganisationOrganisationClassificationId == message.Body.OrganisationOrganisationClassificationId);

                item.OrganisationId = message.Body.OrganisationId;
                item.OrganisationClassificationId = message.Body.OrganisationClassificationId;
                item.OrganisationClassificationTypeId = message.Body.OrganisationClassificationTypeId;

                item.ClassificationValidFrom = message.Body.ValidFrom;
                item.ClassificationValidTo = message.Body.ValidTo;

                context.SaveChanges();
            }
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<InitialiseProjection> message)
        {
            if (message.Body.ProjectionName != typeof(BodySeatGenderRatioProjection).FullName)
                return;

            Logger.LogInformation("Clearing tables for {ProjectionName}.", message.Body.ProjectionName);

            using (var context = _contextFactory().Value)
                context.Database.ExecuteSqlCommand(
                    string.Concat(ProjectionTableNames.Select(tableName => $"DELETE FROM [OrganisationRegistry].[{tableName}];")));
        }

        private static CachedPerson GetPersonFromCache(OrganisationRegistryContext context, Guid personId)
        {
            var person = context
                .BodySeatGenderRatioPersonList
                .SingleOrDefault(x => x.PersonId == personId);

            return person != null
                ? CachedPerson.FromCache(person)
                : CachedPerson.Empty();
        }

        private static CachedOrganisation GetOrganisationFromCache(OrganisationRegistryContext context, Guid organisationId)
        {
            var organisation = context
                .BodySeatGenderRatioOrganisationList
                .SingleOrDefault(x => x.OrganisationId == organisationId);

            return organisation != null
                ? CachedOrganisation.FromCache(organisation)
                : CachedOrganisation.Empty();
        }

        private static CachedOrganisationForBody GetOrganisationForBodyFromCache(OrganisationRegistryContext context, Guid bodyId)
        {
            var organisationPerBody = context
                .BodySeatGenderRatioOrganisationPerBodyList
                .SingleOrDefault(x => x.BodyId == bodyId);

            return organisationPerBody != null
                ? CachedOrganisationForBody.FromCache(organisationPerBody)
                : CachedOrganisationForBody.Empty();
        }
    }
}
