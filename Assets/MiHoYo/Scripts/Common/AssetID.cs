namespace TheWar.Module
{
    using System;
    using UnityEngine;

    /// <summary>
    /// This structure represent the location identify of an asset bundled in AssetBundle.
    /// </summary>
    public struct AssetID : IEquatable<AssetID>
    {
        public static readonly AssetID Empty = new AssetID(string.Empty, string.Empty);

        [SerializeField]
        private string assetBundle;

        [SerializeField]
        private string assetName;

        /// <summary>
        /// Initializes a new instance of the <see cref = "AssetID" /> struct.
        /// </summary>
        public AssetID(string assetBundle, string assetName)
        {
            this.assetBundle = assetBundle;
            this.assetName = assetName;
        }

        /// <summary>
        /// Gets the name of AssetBundle.
        /// </summary>
        public string AssetBundle => this.assetBundle;

        /// <summary>
        /// Gets the asset name.
        /// </summary>
        public string AssetName => this.assetName;

        /// <summary>
        /// Gets a value indicating whether this AssetID is empty.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(this.assetName);

        /// <inheritdoc/>
        public bool Equals(AssetID other)
        {
            return this.assetBundle == other.assetBundle && this.assetName == other.assetName;
        }

        /// <summary>
        /// The text represent of this object.
        /// </summary>
        public override string ToString()
        {
            return this.assetBundle + ": " + this.assetName;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = this.assetBundle.GetHashCode();
            hash = (397 * hash) ^ this.assetName.GetHashCode();
            return hash;
        }
    }
}