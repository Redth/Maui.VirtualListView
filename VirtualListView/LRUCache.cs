using System.Collections.Specialized;

namespace Microsoft.Maui;

/// <summary>
/// LRU cache that internally uses tuples.  T1 is the type of the key, and T2 is the type of the value.
/// </summary>
internal class LRUCache<TKey, TValue>
{
	private readonly int _capacity;
	private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _cache;
	private readonly LinkedList<KeyValuePair<TKey, TValue>> _list;

	public int Capacity { get; set; }

	public LRUCache() : this(1000)
	{}
	
	public LRUCache(int capacity)
	{
		_capacity = capacity;
		_cache = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
		_list = new LinkedList<KeyValuePair<TKey, TValue>>();
	}

	public void AddReplace(TKey key, TValue value)
	{
		if (_cache.TryGetValue(key, out var node))
		{
			_list.Remove(node);
			_cache.Remove(key);
		}

		if (_cache.Count >= _capacity)
		{
			var last = _list.Last;
			_list.RemoveLast();
			_cache.Remove(last.Value.Key);
		}

		node = _list.AddFirst(new KeyValuePair<TKey, TValue>(key, value));
		_cache.Add(key, node);
	}

	public TValue Get(TKey key)
	{
		if (_cache.TryGetValue(key, out var node))
		{
			_list.Remove(node);
			_list.AddFirst(node);
			return node.Value.Value;
		}

		return default;
	}

	public bool TryGet(TKey key, out TValue value)
	{
		if (_cache.TryGetValue(key, out var node))
		{
			_list.Remove(node);
			_list.AddFirst(node);
			value = node.Value.Value;
			return true;
		}

		value = default;
		return false;
	}

	public void Clear()
	{
		_cache.Clear();
		_list.Clear();
	}
}
