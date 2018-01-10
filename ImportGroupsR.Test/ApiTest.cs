using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Exceptions;
using System;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace ImportGroupsR.Test
{
    public class ApiTest
    {
        /// <summary>
        /// This is a saftey precausion. These tests automatically remove all devices, rules, groups, users, zones, custom reports and trailer from the target database. Do not run against a production database. Register a new database @ https://my112.geotab.com/registration.html with the "_test" postfix to run the tests against.
        /// </summary>
        const string ExpectedDatabasePostFix = "_test";
        const string envUserName = "api.username";
        const string envPassword = "api.password";
        const string envDatabase = "api.database";
        const string envServer = "api.server";

        static string sessionId;

        readonly string username;
        readonly string password;
        readonly string database;
        readonly string server;

        protected readonly API api;
        protected ITestOutputHelper Log;

        public ApiTest(ITestOutputHelper log)
        {
            username = Environment.GetEnvironmentVariable(envUserName) ?? throw new ArgumentNullException($"{envUserName} environment variable not set");
            password = Environment.GetEnvironmentVariable(envPassword) ?? throw new ArgumentNullException($"{envPassword} environment variable not set");
            database = Environment.GetEnvironmentVariable(envDatabase) ?? throw new ArgumentNullException($"{envDatabase} environment variable not set");
            server = Environment.GetEnvironmentVariable(envServer) ?? throw new ArgumentNullException($"{envServer} environment variable not set");

            if (!database.EndsWith(ExpectedDatabasePostFix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("WARNING: DO NOT RUN THESE TESTS AGAINST A PRODUCTION DATABASE");
            }

            Log = log;

            api = new API(username, password, sessionId, database, server);
            if (sessionId == null)
            {
                api.Authenticate();
                sessionId = api.LoginResult.Credentials.SessionId;
            }

            Log.WriteLine("Cleaning database...");
            CleanDatabase(api);
            Log.WriteLine("Cleaning database complete");
        }

        void CleanDatabase(API api)
        {
            CleanEntity<Rule>(api);
            CleanEntity<Device>(api);
            CleanEntity<Zone>(api);
            CleanEntity<Trailer>(api);
            CleanEntity<CustomReportSchedule>(api);
            CleanUsers(api);
            CleanEntity<Group>(api);
        }

        void CleanEntity<T>(API api) where T : Entity
        {
            var entities = api.Call<object>("RemoveAll", typeof(T));
        }

        void CleanUsers(API api)
        {
            var entities = api.Call<List<User>>("Get", typeof(User));
            foreach (var entity in entities)
            {
                if (entity.IsSystemEntity())
                {
                    continue;
                }

                // don't delete our test runner user
                var user = entity as User;
                if (user != null && user.Name.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                api.Call<object>("Remove", typeof(User), new { entity });
            }
        }
    }
}
