using Geotab.Checkmate.ObjectModel;

namespace Geotab.SDK.ImportDevices
{
    /// <summary>
    /// Models a row from a file with <see cref="Device"/> data.
    /// </summary>
    class DeviceRow
    {
        /// <summary>
        /// The description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The group names
        /// </summary>
        public string GroupNames { get; set; }

        /// <summary>
        /// The asset type name
        /// </summary>
        public string AssetType { get; set; }

        /// <summary>
        /// Gets or sets the serial number.
        /// </summary>
        /// <value>
        /// The serial number.
        /// </value>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the vin.
        /// </summary>
        /// <value>
        /// The vin.
        /// </value>
        public string Vin { get; set; }
    }
}
