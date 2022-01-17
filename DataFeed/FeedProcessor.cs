﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using Exception = System.Exception;
using Thread = System.Threading.Thread;

namespace Geotab.SDK.DataFeed
{
    /// <summary>
    /// An class that queries Geotab's servers for vehicle data.
    /// </summary>
    class FeedProcessor
    {
        const int RepopulatePeroid = 12; // In hours
        readonly API api;
        IDictionary<Id, Controller> controllerCache;
        IDictionary<Id, Device> deviceCache;
        IDictionary<Id, Diagnostic> diagnosticCache;
        IDictionary<Id, Driver> driverCache;
        IDictionary<Id, FailureMode> failureModeCache;
        DateTime repopulateCaches = DateTime.MinValue;
        IDictionary<Id, Rule> ruleCache;
        IDictionary<Id, UnitOfMeasure> unitOfMeasureCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedProcessor"/> class.
        /// </summary>
        /// <param name="server">The Geotab server address.</param>
        /// <param name="database">The database.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        public FeedProcessor(string server, string database, string user, string password)
            : this(new API(user, password, null, database, server))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedProcessor"/> class.
        /// </summary>
        /// <param name="api">The API.</param>
        public FeedProcessor(API api)
        {
            this.api = api;
        }

        /// <summary>
        /// Requests the feed data, populates feedParams data collections
        /// </summary>
        /// <param name="feedParams">Contains latest data token and collections to populate during this call.</param>
        /// <returns><see cref="FeedResultData"/></returns>
        public async Task<FeedResultData> GetAsync(FeedParameters feedParams)
        {
            FeedResultData feedResults = new FeedResultData(new List<LogRecord>(), new List<StatusData>(), new List<FaultData>(), new List<Trip>(), new List<ExceptionEvent>());
            try
            {
                if (DateTime.UtcNow > repopulateCaches)
                {
                    await PopulateCachesAsync();
                    repopulateCaches = DateTime.UtcNow.AddHours(RepopulatePeroid);
                }
                FeedResult<LogRecord> feedLogRecordData = await MakeFeedCallAsync<LogRecord>(feedParams.LastGpsDataToken);
                FeedResult<StatusData> feedStatusData = await MakeFeedCallAsync<StatusData>(feedParams.LastStatusDataToken);
                FeedResult<FaultData> feedFaultData = await MakeFeedCallAsync<FaultData>(feedParams.LastFaultDataToken);
                FeedResult<Trip> feedTripData = await MakeFeedCallAsync<Trip>(feedParams.LastTripToken);
                FeedResult<ExceptionEvent> feedExceptionData = await MakeFeedCallAsync<ExceptionEvent>(feedParams.LastExceptionToken);
                feedParams.LastGpsDataToken = feedLogRecordData.ToVersion;
                foreach (LogRecord log in feedLogRecordData.Data)
                {
                    // Populate relevant LogRecord fields.
                    log.Device = await GetDeviceAsync(log.Device);
                    feedResults.GpsRecords.Add(log);
                }
                feedParams.LastStatusDataToken = feedStatusData.ToVersion;
                foreach (StatusData log in feedStatusData.Data)
                {
                    // Populate relevant StatusData fields.
                    log.Device = await GetDeviceAsync(log.Device);
                    log.Diagnostic = await GetDiagnosticAsync(log.Diagnostic);
                    feedResults.StatusData.Add(log);
                }
                feedParams.LastFaultDataToken = feedFaultData.ToVersion;
                foreach (FaultData log in feedFaultData.Data)
                {
                    // Populate relevant FaultData fields.
                    log.Device = await GetDeviceAsync(log.Device);
                    log.Diagnostic = await GetDiagnosticAsync(log.Diagnostic);
                    log.Controller = await GetControllerAsync(log.Controller);
                    log.FailureMode = await GetFailureModeAsync(log.FailureMode);
                    feedResults.FaultData.Add(log);
                }
                feedParams.LastTripToken = feedTripData.ToVersion;
                foreach (Trip trip in feedTripData.Data)
                {
                    trip.Device = await GetDeviceAsync(trip.Device);
                    trip.Driver = await GetDriver(trip.Driver);
                    feedResults.Trips.Add(trip);
                }
                feedParams.LastExceptionToken = feedExceptionData.ToVersion;
                foreach (ExceptionEvent exceptionEvent in feedExceptionData.Data)
                {
                    exceptionEvent.Device = await GetDeviceAsync(exceptionEvent.Device);
                    exceptionEvent.Driver = await GetDriver(exceptionEvent.Driver);
                    exceptionEvent.Diagnostic = await GetDiagnosticAsync(exceptionEvent.Diagnostic);
                    exceptionEvent.Rule = await GetRuleAsync(exceptionEvent.Rule);
                    feedResults.ExceptionEvents.Add(exceptionEvent);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (e is HttpRequestException)
                {
                    await Task.Delay(5000);
                }
                if (e is DbUnavailableException)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            }
            return feedResults;
        }

        async Task<IDictionary<Id, T>> GetCacheAsync<T>()
            where T : Entity
        {
            IList<T> items = await api.CallAsync<IList<T>>("Get", typeof(T));
            if (items != null)
            {
                Dictionary<Id, T> cache = new Dictionary<Id, T>(items.Count);
                foreach (T item in items)
                {
                    cache.Add(item.Id, item);
                }
                return cache;
            }
            return new Dictionary<Id, T>(0);
        }

        /// <summary>
        /// Returns a fully populated <see cref="Controller"/> from the cache if it exists, otherwise gets it from the server and adds it to the cache before returning it.
        /// </summary>
        /// <param name="controller">The <see cref="Controller"/> to populate.</param>
        /// <returns>populated controller</returns>
        async Task<Controller> GetControllerAsync(Controller controller)
        {
            if (controller == null || controller is NoController)
            {
                return NoController.Value;
            }
            Id id = controller.Id;
            if (controllerCache.TryGetValue(id, out controller))
            {
                return controller;
            }
            IList<Controller> returnedController = await api.CallAsync<IList<Controller>>("Get", typeof(Controller), new { search = new ControllerSearch(id) });
            if (returnedController.Count == 0)
            {
                return null;
            }
            controller = returnedController[0];
            controllerCache.Add(id, controller);
            return controller;
        }

        /// <summary>
        /// Returns a fully populated <see cref="Device"/> from the cache if it exists, otherwise gets it from the server and adds it to the cache before returning it.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> to populate.</param>
        /// <returns>populated device</returns>
        async Task<Device> GetDeviceAsync(Device device)
        {
            if (device == null || device is NoDevice)
            {
                return NoDevice.Value;
            }
            Id id = device.Id;
            if (deviceCache.TryGetValue(id, out device))
            {
                return device;
            }
            IList<Device> returnedDevices = await api.CallAsync<IList<Device>>("Get", typeof(Device), new { search = new DeviceSearch(id) });
            if (returnedDevices.Count == 0)
            {
                return null;
            }
            device = returnedDevices[0];
            deviceCache.Add(id, device);
            return device;
        }

        /// <summary>
        /// Returns a fully populated <see cref="Diagnostic" /> from the cache if it exists, otherwise gets it from the server and adds it to the cache before returning it.
        /// </summary>
        /// <param name="diagnostic">The <see cref="Diagnostic" /> to populate.</param>
        /// <returns>
        /// Populated diagnostic.
        /// </returns>
        async Task<Diagnostic> GetDiagnosticAsync(Diagnostic diagnostic)
        {
            if (diagnostic == null || diagnostic is NoDiagnostic)
            {
                return NoDiagnostic.Value;
            }
            Id id = diagnostic.Id;
            if (diagnosticCache.TryGetValue(id, out diagnostic))
            {
                return diagnostic;
            }
            IList<Diagnostic> returnedDiagnostic = await api.CallAsync<IList<Diagnostic>>("Get", typeof(Diagnostic), new { search = new DiagnosticSearch(id) });
            if (returnedDiagnostic.Count == 0)
            {
                return null;
            }
            diagnostic = returnedDiagnostic[0];
            diagnosticCache.Add(id, diagnostic);
            return diagnostic;
        }

        async Task<IDictionary<Id, Diagnostic>> GetDiagnosticCacheAsync()
        {
            IDictionary<Id, Diagnostic> cache = await GetCacheAsync<Diagnostic>();
            if (cache.Keys.Count > 0)
            {
                foreach (Diagnostic diagnostic in cache.Values)
                {
                    Controller controller = diagnostic.Controller;
                    if (controller == null)
                    {
                        diagnostic.Controller = NoController.Value;
                    }
                    else if (!controller.Equals(NoController.Value))
                    {
                        diagnostic.Controller = controllerCache[controller.Id];
                    }
                    UnitOfMeasure unitOfMeasure = diagnostic.UnitOfMeasure;
                    if (unitOfMeasure != null)
                    {
                        diagnostic.UnitOfMeasure = unitOfMeasureCache[unitOfMeasure.Id];
                    }
                }
            }
            return cache;
        }

        /// <summary>
        /// Gets the driver.
        /// </summary>
        /// <param name="driver">The driver.</param>
        /// <returns>a populated driver</returns>
        async Task<Driver> GetDriver(Driver driver)
        {
            if (driver == null || driver is NoDriver)
            {
                return NoDriver.Value;
            }
            if (driver is UnknownDriver)
            {
                return UnknownDriver.Value;
            }
            Id id = driver.Id;
            if (driverCache.TryGetValue(id, out driver))
            {
                return driver;
            }
            UserSearch userSearch = new UserSearch(id)
            {
                UserSearchType = UserSearchType.Driver
            };
            IList<User> returnedDriver = await api.CallAsync<List<User>>("Get", typeof(User), new { search = userSearch });
            if (returnedDriver.Count == 0)
            {
                return null;
            }
            driver = (Driver)returnedDriver[0];
            driverCache.Add(id, driver);
            return driver;
        }

        async Task<IDictionary<Id, Driver>> GetDriverCacheAsync()
        {
            UserSearch userSearch =
            new UserSearch
            {
                UserSearchType = UserSearchType.Driver
            };
            IList<User> drivers = await api.CallAsync<List<User>>("Get", typeof(User), new { search = userSearch });
            if (drivers != null)
            {
                Dictionary<Id, Driver> cache = new Dictionary<Id, Driver>(drivers.Count);
                for (int i = 0; i < drivers.Count; i++)
                {
                    Driver driver = (Driver)drivers[i];
                    cache.Add(driver.Id, driver);
                }
                return cache;
            }
            return new Dictionary<Id, Driver>(0);
        }

        /// <summary>
        /// Returns a fully populated <see cref="FailureMode"/> from the cache if it exists, otherwise gets it from the server and adds it to the cache before returning it.
        /// </summary>
        /// <param name="failureMode">The <see cref="FailureMode"/> to populate.</param>
        /// <returns>populated failure mode</returns>
        async Task<FailureMode> GetFailureModeAsync(FailureMode failureMode)
        {
            if (failureMode == null || failureMode is NoFailureMode)
            {
                return NoFailureMode.Value;
            }
            Id id = failureMode.Id;
            if (failureModeCache.TryGetValue(id, out failureMode))
            {
                return failureMode;
            }
            IList<FailureMode> returnedFailureMode = await api.CallAsync<IList<FailureMode>>("Get", typeof(FailureMode), new { search = new FailureModeSearch(id) });
            if (returnedFailureMode.Count == 0)
            {
                return null;
            }
            failureMode = returnedFailureMode[0];
            failureModeCache.Add(id, failureMode);
            return failureMode;
        }

        async Task<Rule> GetRuleAsync(Rule rule)
        {
            if (rule == null)
            {
                return null;
            }
            Id id = rule.Id;
            if (ruleCache.TryGetValue(id, out rule))
            {
                return rule;
            }
            IList<Rule> returnedRule = await api.CallAsync<IList<Rule>>("Get", typeof(Rule), new { search = new RuleSearch(id) });
            if (returnedRule.Count == 0)
            {
                return null;
            }
            rule = returnedRule[0];
            ruleCache.Add(id, rule);
            return rule;
        }

        async Task<FeedResult<T>> MakeFeedCallAsync<T>(long? fromVersion)
            where T : Entity => await api.CallAsync<FeedResult<T>>("GetFeed", typeof(T), new { fromVersion = fromVersion });

        async Task PopulateCachesAsync()
        {
            controllerCache = await GetCacheAsync<Controller>();
            unitOfMeasureCache = await GetCacheAsync<UnitOfMeasure>();
            diagnosticCache = await GetDiagnosticCacheAsync();
            failureModeCache = await GetCacheAsync<FailureMode>();
            deviceCache = await GetCacheAsync<Device>();
            driverCache = await GetDriverCacheAsync();
            ruleCache = await GetCacheAsync<Rule>();
        }
    }
}
