namespace TheWar.Module
{
    using System;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;
    using UnityObject = UnityEngine.Object;

    /// <summary>
    /// The asset reference is act as a weak reference for other asset witch has
    /// asset bundle and asset name.
    /// </summary>
    [Serializable]
    public struct AssetRef : IEquatable<AssetRef>
    {
        /// <summary>
        /// The empty AssetID instance.
        /// </summary>
        public static readonly AssetRef Empty = new AssetRef(string.Empty, string.Empty);

        [SerializeField]
        private string assetGUID;

        [SerializeField]
        private string assetPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetRef"/> struct.
        /// </summary>
        public AssetRef(string guid, string assetPath)
        {
            this.assetGUID = guid;
            this.assetPath = assetPath;
        }

        /// <summary>
        /// Gets the asset GUID.
        /// </summary>
        public string AssetGUID => this.assetGUID;

        public string AssetPath => this.assetPath;

        /// <summary>
        /// Gets a value indicating whether this AssetID is empty.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(this.assetGUID);

#if UNITY_EDITOR
        /// <summary>
        /// Load a object from this asset ID.
        /// </summary>
        public T LoadObject<T>()
            where T : UnityObject
        {
            if (this.IsEmpty)
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(this.assetGUID);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
#endif

        /// <summary>
        /// The text represent of this object.
        /// </summary>
        public override string ToString()
        {
            //var assetID = AssetManager.GetAssetID(this);
            //return assetID.IsEmpty ? this.assetGUID : assetID.ToString();
            return this.assetGUID;
        }

        /// <inheritdoc/>
        public bool Equals(AssetRef other)
        {
            return this.assetGUID == other.assetGUID;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.assetGUID.GetHashCode();
        }
    }
}