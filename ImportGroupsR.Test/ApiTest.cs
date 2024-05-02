using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ImportGroupsR.Test
{
    public class ApiTest
    {
        /// <summary>
        /// This is a safety precaution. These tests automatically remove all devices, rules, groups, users, zones, custom reports and trailer from the target database. Do not run against a production database. Register a new database @ https://my112.geotab.com/registration.html with the "_test" postfix to run the tests against.
        /// </summary>
        const string ExpectedDatabasePostFix = "_tests";
        const string envUserName = "api_username";
        const string envPassword = "api_password";
        const string envDatabase = "api_database";
        const string envServer = "api_server";

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
        }

        protected async Task SetupAsync()
        {
            if (sessionId == null)
            {
                await api.AuthenticateAsync();
                sessionId = api.LoginResult.Credentials.SessionId;
            }

            Log.WriteLine("Cleaning database...");
            await CleanDatabaseAsync(api);
            Log.WriteLine("Cleaning database complete");
        }

        async Task CleanDatabaseAsync(API api)
        {
            await CleanEntityAsync<Rule>(api);
            await CleanEntityAsync<Device>(api);
            await CleanEntityAsync<Zone>(api);
            await CleanEntityAsync<CustomReportSchedule>(api);
            await CleanUsersAsync(api);
            await CleanEntityAsync<Group>(api);
        }

        async Task CleanEntityAsync<T>(API api) where T : Entity
        {
            await api.CallAsync<object>("RemoveAll", typeof(T));
        }

        async Task CleanUsersAsync(API api)
        {
            var entities = await api.CallAsync<List<User>>("Get", typeof(User));
            foreach (var entity in entities)
            {
                if (entity.IsSystemEntity())
                {
                    continue;
                }

                // don't delete our test runner user
                var user = entity;
                if (user != null && user.Name.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                await api.CallAsync<object>("Remove", typeof(User), new { entity });
            }
        }
    }
}
