using System;
using System.Collections.Generic;
using System.Text;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;

namespace Geotab.SDK.DataFeed
{
    /// <summary>
    /// An object that creates a feed to the console
    /// </summary>
    class FeedToConsole
    {
        const string FaultDataHeader = "Vehicle Serial Number, Date, Diagnostic Name, Failure Mode Name, Failure Mode Source, Controller Name";
        const string GpsDataHeader = "Vehicle Serial Number, Date, Longitude, Latitude, Speed";
        const string StatusDataHeader = "Vehicle Serial Number, Date, Diagnostic Name, Source Name, Value, Units";
        const string TripHeader = "VehicleName, VehicleSerialNumber, Vin, Driver Name, Driver Keys, Trip Start Time, Trip End Time, Trip Distance";

        const string ExceptionEventHeader = "Id, Vehicle Name, Vehicle Serial Number, VIN, Diagnostic Name, Diagnostic Code, Source Name, Driver Name, Driver Keys, Rule Name,sActive From, Active To";

        static readonly char[] trimChars = { ' ', ',' };
        readonly IDictionary<Id, Device> deviceLookup = new Dictionary<Id, Device>();
        readonly IList<FaultData> faultRecords;
        readonly IList<LogRecord> gpsRecords;
        readonly IList<StatusData> statusRecords;
        readonly IList<ExceptionEvent> exceptionEvents;
        readonly IList<Trip> trips;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedToConsole"/> class.
        /// </summary>
        /// <param name="gpsRecords">The GPS records.</param>
        /// <param name="statusRecords">The status records.</param>
        /// <param name="faultRecords">The fault records.</param>
        public FeedToConsole(IList<LogRecord> gpsRecords = null, IList<StatusData> statusRecords = null, IList<FaultData> faultRecords = null, IList<Trip> trips = null, IList<ExceptionEvent> exceptionEvents = null)
        {
            this.gpsRecords = gpsRecords ?? new List<LogRecord>();
            this.statusRecords = statusRecords ?? new List<StatusData>();
            this.faultRecords = faultRecords ?? new List<FaultData>();
            this.trips = trips ?? new List<Trip>();
            this.exceptionEvents = exceptionEvents ?? new List<ExceptionEvent>();
            List<Device> devices = new List<Device>(SeparateByDevice(this.gpsRecords).Keys);
            devices.AddRange(SeparateByDevice(this.statusRecords).Keys);
            devices.AddRange(SeparateByDevice(this.faultRecords).Keys);
            foreach (Device device in devices)
            {
                deviceLookup[device.Id] = device;
            }
        }

        /// <summary>
        /// Separates collection of <see cref="IDeviceProvider" /> interface objects into individual collections by
        /// <see cref="Device" />
        /// objects.
        /// </summary>
        /// <typeparam name="T">The type of the device</typeparam>
        /// <param name="collection">The collection.</param>
        /// <returns>
        /// dictionary with devices
        /// </returns>
        public static IDictionary<Device, IList<T>> SeparateByDevice<T>(ICollection<T> collection)
            where T : IDeviceProvider
        {
            Dictionary<Device, IList<T>> dictionary = new Dictionary<Device, IList<T>>(1);
            if (collection.Count > 0)
            {
                Device thisDevice = null;
                IList<T> list = collection as IList<T>;
                if (list == null)
                {
                    SeparateByDeviceImpl(dictionary, collection);
                }
                else
                {
                    bool useOriginalCollection = true;
                    foreach (T item in collection)
                    {
                        Device device = item.Device;
                        if (thisDevice == null)
                        {
                            thisDevice = device;
                            continue;
                        }
                        if (device.Equals(thisDevice))
                        {
                            continue;
                        }
                        useOriginalCollection = false;
                        break;
                    }
                    if (useOriginalCollection)
                    {
                        dictionary.Add(thisDevice, list);
                    }
                    else
                    {
                        SeparateByDeviceImpl(dictionary, collection);
                    }
                }
            }
            return dictionary;
        }

        /// <summary>
        /// Runs the feed.
        /// </summary>
        public void Run()
        {
            
            if (gpsRecords.Count > 0)
            {
                WriteData(gpsRecords);
            }
            if (statusRecords.Count > 0)
            {
                WriteData(statusRecords);
            }
            if (faultRecords.Count > 0)
            {
                WriteData(faultRecords);
            }
            if (trips.Count > 0)
            {
                WriteData(trips);
            }
            if (exceptionEvents.Count > 0)
            {
                WriteData(exceptionEvents);
            }
            if ((faultRecords.Count == 0)&& (statusRecords.Count == 0) && (gpsRecords.Count == 0) && (trips.Count == 0) && (exceptionEvents.Count == 0))
            {
                NoDataError();
            }
        }

        static void AppendName(StringBuilder sb, NameEntity entity)
        {
            AppendValues(sb, entity.IsSystemEntity() ? entity.GetType().ToString().Replace("Geotab.Checkmate.ObjectModel.Engine.", "").Replace(",", " ") : entity.Name.Replace(",", " "));
        }

        static void AppendValues(StringBuilder sb, object o)
        {
            sb.Append(o);
            sb.Append(", ");
        }

        static void SeparateByDeviceImpl<T>(IDictionary<Device, IList<T>> dictionary, ICollection<T> collection)
                    where T : IDeviceProvider
        {
            IList<T> records = null;
            Device thisDevice = null;
            foreach (T item in collection)
            {
                Device device = item.Device;
                bool addToDictionary = records == null || !device.Equals(thisDevice) && !dictionary.TryGetValue(device, out records);
                thisDevice = device;
                if (addToDictionary)
                {
                    records = new List<T>(100);
                    dictionary.Add(thisDevice, records);
                }
                records.Add(item);
            }
        }

        void AppendDeviceValues(StringBuilder sb, Id id)
        {
            if (deviceLookup.TryGetValue(id, out Device device))
            {
                AppendValues(sb, device.SerialNumber);
            }
            else
            {
                AppendValues(sb, "");
                AppendValues(sb, "");
            }
        }

        static void AppendDeviceValues(StringBuilder sb, Device device)
        {
            AppendValues(sb, device.Name.Replace(",", " "));
            AppendValues(sb, device.SerialNumber);
            GoDevice goDevice = device as GoDevice;
            AppendValues(sb, (goDevice == null ? "" : goDevice.VehicleIdentificationNumber ?? "").Replace(",", " "));
        }
        
        static void AppendDriverValues(StringBuilder sb, Driver driver)
        {
            AppendName(sb, driver);
            List<Key> keys = driver.Keys;
            if (keys != null)
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append('~');
                    }
                    sb.Append(keys[i].SerialNumber);
                }
            }
            sb.Append(',');
        }

        static void AppendDiagnosticValues(StringBuilder sb, Diagnostic diagnostic)
        {
            AppendName(sb, diagnostic);
            AppendValues(sb, diagnostic.Code);
            Source source = diagnostic.Source;
            if (source != null)
            {
                AppendName(sb, source);
            }
            else
            {
                AppendValues(sb, "");
            }
        }

        void Write(LogRecord logRecord)
        {
            StringBuilder sb = new StringBuilder();
            AppendDeviceValues(sb, logRecord.Device.Id);
            AppendValues(sb, logRecord.DateTime);
            AppendValues(sb, Math.Round(logRecord.Longitude, 3));
            AppendValues(sb, Math.Round(logRecord.Latitude, 3));
            AppendValues(sb, logRecord.Speed);
            Console.WriteLine(sb.ToString().TrimEnd(trimChars));
        }

        void Write(StatusData statusData)
        {
            StringBuilder sb = new StringBuilder();
            AppendDeviceValues(sb, statusData.Device.Id);
            AppendValues(sb, statusData.DateTime);
            Diagnostic diagnostic = statusData.Diagnostic;
            AppendName(sb, diagnostic);
            AppendName(sb, diagnostic.Source);
            AppendValues(sb, statusData.Data);
            if (diagnostic is DataDiagnostic dataDiagnostic)
            {
                AppendName(sb, dataDiagnostic.UnitOfMeasure);
            }
            Console.WriteLine(sb.ToString().TrimEnd(trimChars));
        }

        void Write(FaultData faultData)
        {
            StringBuilder sb = new StringBuilder();
            AppendDeviceValues(sb, faultData.Device.Id);
            AppendValues(sb, faultData.DateTime);
            AppendName(sb, faultData.Diagnostic);
            FailureMode failureMode = faultData.FailureMode;
            AppendName(sb, failureMode);
            if (failureMode is NoFailureMode)
            {
                AppendValues(sb, "None");
            }
            else
            {
                AppendName(sb, failureMode.Source);
            }
            AppendName(sb, faultData.Controller);
            Console.WriteLine(sb.ToString().TrimEnd(trimChars));
        }

        void Write(Trip trip)
        {
            StringBuilder sb = new StringBuilder();
            AppendDeviceValues(sb, trip.Device);
            AppendDriverValues(sb, trip.Driver);
            AppendValues(sb, trip.Start);
            AppendValues(sb, trip.Stop);
            AppendValues(sb, trip.Distance);
            Console.WriteLine(sb.ToString().TrimEnd(trimChars));
        }


        void Write(ExceptionEvent exceptionEvent)
        {
            StringBuilder sb = new StringBuilder();
            AppendValues(sb, exceptionEvent.Id);
            AppendDeviceValues(sb, exceptionEvent.Device);
            AppendDiagnosticValues(sb, exceptionEvent.Diagnostic);
            AppendDriverValues(sb, exceptionEvent.Driver);
            AppendName(sb, exceptionEvent.Rule);
            AppendValues(sb, exceptionEvent.ActiveFrom);
            AppendValues(sb, exceptionEvent.ActiveTo);
            Console.WriteLine(sb.ToString().TrimEnd(trimChars));
        }


        void NoDataError(){
            Console.WriteLine("Unable to Write to Console: No data found");
        }
        void WriteData<T>(IList<T> entities)
                                            where T : class
        {
            Type type = typeof(T);
            if (type == typeof(LogRecord))
            {
                IList<LogRecord> logs = (IList<LogRecord>)entities;
                Console.WriteLine(GpsDataHeader);
                for (int i = 0; i < logs.Count; i++)
                {
                    Write(logs[i]);
                }
                Console.WriteLine();
            }
            else if (type == typeof(StatusData))
            {
                IList<StatusData> statusData = (IList<StatusData>)entities;
                Console.WriteLine(StatusDataHeader);
                for (int i = 0; i < statusData.Count; i++)
                {
                    Write(statusData[i]);
                }
                Console.WriteLine();
            }
            else if (type == typeof(FaultData))
            {
                IList<FaultData> faults = (IList<FaultData>)entities;
                Console.WriteLine(FaultDataHeader);
                for (int i = 0; i < faults.Count; i++)
                {
                    Write(faults[i]);
                }
                Console.WriteLine();
            }
            else if (type == typeof(Trip))
            {
                IList<Trip> trips = (IList<Trip>)entities;
                Console.WriteLine(TripHeader);
                for (int i = 0; i < trips.Count; i++)
                {
                    Write(trips[i]);
                }
                Console.WriteLine();
            }
            else if (type == typeof(ExceptionEvent))
            {
                IList<ExceptionEvent> exceptionEvents = (IList<ExceptionEvent>)entities;
                Console.WriteLine(ExceptionEventHeader);
                for (int i = 0; i < exceptionEvents.Count; i++)
                {
                    Write(exceptionEvents[i]);
                }
                Console.WriteLine();
            }

            else
            {
                throw new NotSupportedException(type.ToString());
            }
        }
    }
}