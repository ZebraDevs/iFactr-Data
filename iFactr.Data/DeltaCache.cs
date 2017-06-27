using MonoCross.Utilities;
using MonoCross.Utilities.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iFactr.Data
{

    /// <summary>
    /// Represents a list of cached objects that have been changed.
    /// </summary>
    /// <remarks>
    /// the <c>DeltaCache&lt;T&gt;</c> class is used by the <c>Provider&lt;T&gt;</c> class to maintain a list of cached objects that have been changed via queued transaction processing. When an add, change, or delete transaction is successfully completed on a provider the the object, its transaction metadata is placed in the delta cache to assist in future caching operations involving that item. the delta cache is maintained automatically by the provider through normal usage.
    /// </remarks>
    /// <typeparam name="T">The generic object type of the collection</typeparam>
#if (DROID)
    [Android.Runtime.Preserve(AllMembers = true)]
#elif (TOUCH)
    [MonoTouch.Foundation.Preserve (AllMembers = true)]
#endif
    public class DeltaCache<T> : List<DeltaCacheItem>
    {
        object syncLock = new object();

        /// <summary>
        /// Adds a unique DeltaCacheItem to the list.
        /// </summary>
        /// <param name="deltaCacheItem">The item to add.</param>
        /// <param name="serialize">Indicates whether to serialize the delta cache after the change.</param>
        public void Add(DeltaCacheItem deltaCacheItem, bool serialize)
        {
            lock (syncLock)
            {
                // prevent duplicates

                var items = this.Where(item => item.Uri.Equals(deltaCacheItem.Uri, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var item in items)
                    base.Remove(item);

                // can't use RemoveAll thanks to SL.
                //base.RemoveAll( item => item.Uri.Equals( deltaCacheItem.Uri, StringComparison.InvariantCultureIgnoreCase ) );

                base.Add(deltaCacheItem);
                if (serialize)
                    this.Serialize();
            }
        }
        /// <summary>
        /// Adds a unique DeltaCacheItem to the list.
        /// </summary>
        /// <param name="deltaCacheItem">The item to add.</param>
        public new void Add(DeltaCacheItem deltaCacheItem)
        {
            this.Add(deltaCacheItem, true);
        }

        /// <summary>
        /// Updates a unique DeltaCacheItem in the list.
        /// </summary>
        /// <param name="deltaCacheItem">The item to update.</param>
        /// <param name="serialize">Indicates whether to serialize the delta cache after the change.</param>
        public void Update(DeltaCacheItem deltaCacheItem, bool serialize)
        {
            lock (syncLock)
            {
                // prevent duplicates
                var items = this.Where(item => item.Uri.Equals(deltaCacheItem.Uri, StringComparison.OrdinalIgnoreCase));
                foreach (var item in items)
                    base.Remove(item);

                base.Add(deltaCacheItem);
                if (serialize)
                    this.Serialize();
            }
        }
        /// <summary>
        /// Updates a unique DeltaCacheItem to the list.
        /// </summary>
        /// <param name="deltaCacheItem">The item to update.</param>
        public void Update(DeltaCacheItem deltaCacheItem)
        {
            this.Update(deltaCacheItem, true);
        }

        /// <summary>
        /// Removes a DeltaCacheItem from the list.
        /// </summary>
        /// <param name="deltaCacheItem">The item to remove.</param>
        /// <param name="serialize">indicates whether to serialize the delta cache after the change.</param>
        public void Remove(DeltaCacheItem deltaCacheItem, bool serialize)
        {
            lock (syncLock)
            {
                // prevent duplicates
                //base.RemoveAll( item => item.Uri.Equals( deltaCacheItem.Uri, StringComparison.InvariantCultureIgnoreCase ) );  // doesn't work in SL

                var items = this.Where(item => item.Uri.Equals(deltaCacheItem.Uri, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var item in items)
                    base.Remove(item);

                if (serialize)
                    this.Serialize();
            }
        }
        /// <summary>
        /// Removes a DeltaCacheItem from the list.
        /// </summary>
        /// <param name="deltaCacheItem">The item to remove.</param>
        public new void Remove(DeltaCacheItem deltaCacheItem)
        {
            this.Remove(deltaCacheItem, true);
        }

        #region Delta Cache Serialization Methods

        /// <summary>
        /// Deserializes DeltaCache from file.
        /// </summary>
        public void Deserialize()
        {
            lock (syncLock)
            {
                ISerializer<DeltaCacheItem> iSerializer = SerializerFactory.Create<DeltaCacheItem>(SerializationFormat.XML);
                List<DeltaCacheItem> list = null;

                try
                {
                    list = iSerializer.DeserializeListFromFile(DeltaCacheFileName);
                }
                catch (Exception cexc)
                {
                    if (cexc.Message.Contains("Bad PKCS7 padding") || cexc.Message.Contains("Padding is invalid and cannot be removed"))
                    {
                        // attempt to deserialize file with no encryption.
                        list = iSerializer.DeserializeListFromFile(DeltaCacheFileName, EncryptionMode.NoEncryption);
                    }
                    else
                        throw;
                }

                if (list == null)
                    return;

                foreach (DeltaCacheItem item in list)
                    this.Add(item, false);
            }
        }

        /// <summary>
        /// Serializes DeltaCache to file.
        /// </summary>
        public void Serialize()
        {
            lock (syncLock)
            {
                ISerializer<DeltaCacheItem> iSerializer = SerializerFactory.Create<DeltaCacheItem>(SerializationFormat.XML);
                iSerializer.SerializeListToFile(this.ToList(), DeltaCacheFileName);
            }
        }

        string _cacheFileName;
        string DeltaCacheFileName
        {
            get
            {
                if (string.IsNullOrEmpty(_cacheFileName))
                {
                    _cacheFileName = Device.SessionDataPath.AppendPath("Queue").AppendPath(typeof(T).Name + "_delta.xml");
                    // ensure file's directory exists
                    Device.File.EnsureDirectoryExists(_cacheFileName);
                }

                return _cacheFileName;
            }
            set
            {
                _cacheFileName = value;
            }
        }


        /// <summary>
        /// Removes all delta cache items, and removes the serialized delta cache file from storage.
        /// </summary>
        public new void Clear()
        {
            lock (syncLock)
            {
                Device.File.Delete(DeltaCacheFileName);
                this.Clear();
            }
        }

        /// <summary>
        /// Removes delta cache contents posted on or before the date provided, and removes the serialized delta cache file from storage.
        /// </summary>
        /// <param name="clearDate">The post date on or before which the delta cache items are to be removed.</param>
        public void Clear(DateTime clearDate)
        {
            // ensure date is in universal time.
            clearDate = clearDate.ToUniversalTime();

            lock (syncLock)
            {
                //base.RemoveAll( item => item.PostDate <= clearDate );
                var items = this.Where(item => item.PostDate <= clearDate).ToList();
                foreach (var item in items)
                    base.Remove(item);

                if (this.Count > 0)
                    this.Serialize();
                else
                    Device.File.Delete(DeltaCacheFileName);
            }
        }
        #endregion
    }
    /// <summary>
    /// Represents an individual delta cache item.
    /// </summary>
    /// <remarks>
    /// The <c>DeltaCacheItem </c>class contains the metadata for a cached object that has been modified via queue processing.
    /// </remarks>
#if (DROID)
    [Android.Runtime.Preserve(AllMembers = true)]
#elif (TOUCH)
    [MonoTouch.Foundation.Preserve (AllMembers = true)]
#endif
    public class DeltaCacheItem
    {
        /// <summary>
        /// Gets or sets the URI of the item.
        /// </summary>
        public string Uri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the HTTP Verb that describes the action taken on the item.
        /// </summary>
        public string Verb
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the transaction date of the item.
        /// </summary>
        public DateTime PostDate
        {
            get
            {
                return _postDate;
            }
            set
            {
                _postDate = value.ToUniversalTime();
            }
        }
        private DateTime _postDate = DateTime.MinValue.ToUniversalTime();
    }
}
