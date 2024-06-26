﻿using Geotab.Checkmate.ObjectModel;

namespace Geotab.SDK.ImportUsers
{
    /// <summary>
    /// Object representing user details.
    /// </summary>
    /// <remarks>
    /// Fields include user information such as email, password, groups, security clearance, first name, and last name.
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="UserDetails"/> class.
    /// </remarks>
    /// <param name="user">The user.</param>
    /// <param name="password">The password.</param>
    /// <param name="groups">The groups.</param>
    /// <param name="securityClearance">The security clearance.</param>
    /// <param name="firstName">The first name.</param>
    /// <param name="lastName">The last name.</param>
    class UserDetails(User user, string password, string groups, string securityClearance, string firstName, string lastName)
    {
        /// <summary>
        /// The user.
        /// </summary>
        public readonly User UserNode = user;

        /// <summary>
        /// The password.
        /// </summary>
        public readonly string PasswordNode = password;

        /// <summary>
        /// The groups.
        /// </summary>
        public readonly string GroupsNode = groups;

        /// <summary>
        /// The security clearance.
        /// </summary>
        public readonly string SecurityClearanceNode = securityClearance;

        /// <summary>
        /// The first name.
        /// </summary>
        public readonly string FirstNameNode = firstName;

        /// <summary>
        /// The last name.
        /// </summary>
        public readonly string LastNameNode = lastName;
    }
}
