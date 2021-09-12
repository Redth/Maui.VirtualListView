using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Maui
{
    /// <summary>
    /// LRU cache that internally uses tuples.  T1 is the type of the key, and T2 is the type of the value.
    /// </summary>
    internal class LRUCache<T1, T2> : IDisposable
    {
        #region Private-Members

        private int _Capacity = 0;
        private int _EvictCount = 0;
        private readonly object _CacheLock = new object();
        private Dictionary<T1, DataNode<T2>> _Cache;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initialize the cache.
        /// </summary>
        /// <param name="capacity">Maximum number of entries.</param>
        /// <param name="evictCount">Number to evict when capacity is reached.</param>
        /// <param name="debug">Deprecated.</param>
        public LRUCache(int capacity, int evictCount, bool debug = false)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (evictCount < 1) throw new ArgumentOutOfRangeException(nameof(evictCount));
            if (evictCount > capacity) throw new ArgumentOutOfRangeException(nameof(evictCount));

            _Capacity = capacity;
            _EvictCount = evictCount;
            _Cache = new Dictionary<T1, DataNode<T2>>();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose of the object.  Do not use after disposal.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Retrieve the current number of entries in the cache.
        /// </summary>
        /// <returns>An integer containing the number of entries.</returns>
        public int Count()
        {
            lock (_CacheLock)
            {
                return _Cache.Count;
            }
        }

        /// <summary>
        /// Retrieve the key of the oldest entry in the cache.
        /// </summary>
        /// <returns>String containing the key.</returns>
        public T1 Oldest()
        {
            if (_Cache == null || _Cache.Count < 1) throw new KeyNotFoundException();

            lock (_CacheLock)
            {
                KeyValuePair<T1, DataNode<T2>> oldest = _Cache.Where(x => x.Value.Added != null).OrderBy(x => x.Value.Added).First();
                return oldest.Key;
            }
        }

        /// <summary>
        /// Retrieve the key of the newest entry in the cache.
        /// </summary>
        /// <returns>String containing the key.</returns>
        public T1 Newest()
        {
            if (_Cache == null || _Cache.Count < 1) throw new KeyNotFoundException();

            lock (_CacheLock)
            {
                KeyValuePair<T1, DataNode<T2>> newest = _Cache.Where(x => x.Value.Added != null).OrderBy(x => x.Value.Added).Last();
                return newest.Key;
            }
        }

        /// <summary>
        /// Retrieve all entries from the cache.
        /// </summary>
        /// <returns>Dictionary.</returns>
        public Dictionary<T1, T2> All()
        {
            Dictionary<T1, T2> ret = new Dictionary<T1, T2>();
            Dictionary<T1, DataNode<T2>> dump = null;

            lock (_CacheLock)
            {
                dump = new Dictionary<T1, DataNode<T2>>(_Cache);
            }

            foreach (KeyValuePair<T1, DataNode<T2>> cached in dump)
            {
                ret.Add(cached.Key, cached.Value.Data);
            }

            return ret;
        }

        /// <summary>
        /// Retrieve the key of the last used entry in the cache.
        /// </summary>
        /// <returns>String containing the key.</returns>
        public T1 LastUsed()
        {
            if (_Cache == null || _Cache.Count < 1) throw new KeyNotFoundException();

            lock (_CacheLock)
            {
                KeyValuePair<T1, DataNode<T2>> lastUsed = _Cache.Where(x => x.Value.LastUsed != null).OrderBy(x => x.Value.LastUsed).Last();
                return lastUsed.Key;
            }
        }

        /// <summary>
        /// Retrieve the key of the first used entry in the cache.
        /// </summary>
        /// <returns>String containing the key.</returns>
        public T1 FirstUsed()
        {
            if (_Cache == null || _Cache.Count < 1) throw new KeyNotFoundException();

            lock (_CacheLock)
            {
                KeyValuePair<T1, DataNode<T2>> firstUsed = _Cache.Where(x => x.Value.LastUsed != null).OrderBy(x => x.Value.LastUsed).First();
                return firstUsed.Key;
            }
        }

        /// <summary>
        /// Clear the cache.
        /// </summary>
        public void Clear()
        {
            lock (_CacheLock)
            {
                _Cache = new Dictionary<T1, DataNode<T2>>();
                return;
            }
        }

        /// <summary>
        /// Retrieve a key's value from the cache.
        /// </summary>
        /// <param name="key">The key associated with the data you wish to retrieve.</param>
        /// <returns>The object data associated with the key.</returns>
        public T2 Get(T1 key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            lock (_CacheLock)
            {
                if (_Cache.ContainsKey(key))
                {
                    KeyValuePair<T1, DataNode<T2>> curr = _Cache.Where(x => x.Key.Equals(key)).First();

                    // update LastUsed
                    _Cache.Remove(key);
                    curr.Value.LastUsed = DateTime.Now;
                    _Cache.Add(key, curr.Value);

                    // return data
                    return curr.Value.Data;
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
        }

        /// <summary>
        /// Retrieve a key's value from the cache.
        /// </summary>
        /// <param name="key">The key associated with the data you wish to retrieve.</param>
        /// <param name="val">The value associated with the key.</param>
        /// <returns>True if key is found.</returns>
        public bool TryGet(T1 key, out T2 val)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            lock (_CacheLock)
            {
                if (_Cache.ContainsKey(key))
                {
                    KeyValuePair<T1, DataNode<T2>> curr = _Cache.Where(x => x.Key.Equals(key)).First();

                    // update LastUsed
                    _Cache.Remove(key);
                    curr.Value.LastUsed = DateTime.Now;
                    _Cache.Add(key, curr.Value);

                    // return data
                    val = curr.Value.Data;
                    return true;
                }
                else
                {
                    val = default(T2);
                    return false;
                }
            }
        }

        /// <summary>
        /// See if a key exists in the cache.
        /// </summary>
        /// <param name="key">The key of the cached items.</param>
        /// <returns>True if cached.</returns>
        public bool Contains(T1 key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            lock (_CacheLock)
            {
                if (_Cache.ContainsKey(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Add or replace a key's value in the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="val">The value associated with the key.</param>
        /// <returns>Boolean indicating success.</returns>
        public void AddReplace(T1 key, T2 val)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            lock (_CacheLock)
            {
                if (_Cache.ContainsKey(key))
                {
                    _Cache.Remove(key);
                }

                if (_Cache.Count >= _Capacity)
                {
                    _Cache = _Cache.OrderBy(x => x.Value.LastUsed).Skip(_EvictCount).ToDictionary(x => x.Key, x => x.Value);
                }

                DataNode<T2> curr = new DataNode<T2>(val);
                _Cache.Add(key, curr);
                return;
            }
        }

        /// <summary>
        /// Remove a key from the cache.
        /// </summary>
        /// <param name="key">The key.</param> 
        public void Remove(T1 key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            lock (_CacheLock)
            {
                if (_Cache.ContainsKey(key))
                {
                    _Cache.Remove(key);
                }

                return;
            }
        }

        /// <summary>
        /// Retrieve all keys in the cache.
        /// </summary>
        /// <returns>List of string.</returns>
        public List<T1> GetKeys()
        {
            lock (_CacheLock)
            {
                List<T1> keys = new List<T1>(_Cache.Keys);
                return keys;
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Dispose of the object.  Do not use after disposal.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_CacheLock)
                {
                    _Cache = null;
                }

                _Capacity = 0;
                _EvictCount = 0;
            }
        }

        #endregion
    }

    internal class DataNode<T>
    {
        public T Data { get; set; }
        public DateTime Added { get; set; }
        public DateTime LastUsed { get; set; }

        public DataNode()
        {
            DateTime ts = DateTime.Now;
            Added = ts;
            LastUsed = ts;
        }

        public DataNode(T val)
        {
            DateTime ts = DateTime.Now;
            Added = ts;
            LastUsed = ts;
            Data = val;
        }
    }
}