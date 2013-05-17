// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserEntity.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>
    /// Wrapped Entity with validating members for Users.
    /// No Default construction - use Enity or JSON string
    /// to initialize.
    /// </summary>
    public class UserEntity : EntityWrapperBase
    {
        // TODO: AuthZ members

        /// <summary>User Id property name.</summary>
        public const string UserIdPropertyName = "UserId";

        /// <summary>Full name property name.</summary>
        public const string FullNamePropertyName = "FullName";

        /// <summary>Contact Email property name.</summary>
        public const string ContactEmailPropertyName = "ContactEmail";

        /// <summary>First Name property name.</summary>
        public const string FirstNamePropertyName = "FirstName";

        /// <summary>Last Name property name.</summary>
        public const string LastNamePropertyName = "LastName";

        /// <summary>Contact Phone property name.</summary>
        public const string ContactPhonePropertyName = "ContactPhone";

        /// <summary>Category Name for User Entities.</summary>
        public const string UserEntityCategory = "User";

        /// <summary>Access List property name.</summary>
        public const string AccessListPropertyName = "AccessList";

        /// <summary>Initializes a new instance of the <see cref="UserEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity id to assign the entity.</param>
        /// <param name="rawEntity">The raw entity from which to construct.</param>
        public UserEntity(EntityId externalEntityId, IRawEntity rawEntity) 
        {
            this.Initialize(externalEntityId, UserEntityCategory, rawEntity);
        }

        /// <summary>Initializes a new instance of the <see cref="UserEntity"/> class.</summary>
        /// <param name="entity">The IEntity object from which to construct.</param>
        public UserEntity(IRawEntity entity)
        {
            this.Initialize(entity);
        }

        /// <summary>Gets or sets UserName. Property passed on set can be un-named and name will be set.</summary>
        public EntityProperty UserId
        {
            get { return this.TryGetEntityPropertyByName(UserIdPropertyName, string.Empty); }
            set { this.SetEntityProperty(new EntityProperty { Name = UserIdPropertyName, Value = value.Value }); }
        }

        /// <summary>Gets or sets FullName.</summary>
        public EntityProperty FullName
        {
            get { return this.TryGetEntityPropertyByName(FullNamePropertyName, string.Empty); }
            set { this.SetEntityProperty(new EntityProperty { Name = FullNamePropertyName, Value = value.Value }); }
        }

        /// <summary>Gets or sets FirstName.</summary>
        public EntityProperty FirstName
        {
            get { return this.TryGetEntityPropertyByName(FirstNamePropertyName, string.Empty); }
            set { this.SetEntityProperty(new EntityProperty { Name = FirstNamePropertyName, Value = value.Value }); }
        }

        /// <summary>Gets or sets LastName.</summary>
        public EntityProperty LastName
        {
            get { return this.TryGetEntityPropertyByName(LastNamePropertyName, string.Empty); }
            set { this.SetEntityProperty(new EntityProperty { Name = LastNamePropertyName, Value = value.Value }); }
        }

        /// <summary>Gets or sets ContactEmail.</summary>
        public EntityProperty ContactEmail
        {
            get { return this.TryGetEntityPropertyByName(ContactEmailPropertyName, string.Empty); }
            set { this.SetEntityProperty(new EntityProperty { Name = ContactEmailPropertyName, Value = value.Value }); }
        }

        /// <summary>Gets or sets ContactPhone.</summary>
        public EntityProperty ContactPhone
        {
            get { return this.TryGetEntityPropertyByName(ContactPhonePropertyName, string.Empty); }
            set { this.SetEntityProperty(new EntityProperty { Name = ContactPhonePropertyName, Value = value.Value }); }
        }

        /// <summary>Gets or sets AcessList.</summary>
        public EntityProperty AccessList
        {
            get { return this.TryGetEntityPropertyByName(AccessListPropertyName, string.Empty); }
            set { this.SetEntityProperty(new EntityProperty { Name = AccessListPropertyName, Value = value.Value }); }
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The wrapped entity.</param>
        public override sealed void ValidateEntityType(IRawEntity entity)
        {
            ThrowIfCategoryMismatch(entity, UserEntityCategory);

            // TODO: Determine the minimum set of properties a User Entity must have
        }
    }
}