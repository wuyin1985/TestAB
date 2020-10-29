namespace TheWar.Module
{
    using System;

    /// <summary>
    /// The attribute for a property to export this property to inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AssetTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetTypeAttribute"/>
        /// class.
        /// </summary>
        public AssetTypeAttribute(Type assetType)
        {
            this.AssetType = assetType;
        }

        /// <summary>
        /// Gets the asset type.
        /// </summary>
        public Type AssetType { get; private set; }
    }
}
