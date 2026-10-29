#nullable enable
namespace Microsoft.Maui;

internal sealed class ReusableViewPool<T> : IDisposable where T : class
{
	readonly Dictionary<string, Queue<T>> entries = new();
	readonly Action<T> release;
	int count;

	public ReusableViewPool(Action<T> release) => this.release = release;

	public T? Take(string key)
	{
		if (!entries.TryGetValue(key, out var queue))
			return null;
		var item = queue.Dequeue();
		count--;
		if (queue.Count == 0)
			entries.Remove(key);
		return item;
	}

	public void Return(string key, T item, int activeCount)
	{
		var capacity = Math.Max(4, activeCount * 2);
		while (count >= capacity)
		{
			var oldest = entries.First();
			release(Take(oldest.Key)!);
		}
		if (!entries.TryGetValue(key, out var queue))
			entries.Add(key, queue = new Queue<T>());
		if (queue.Count >= 2)
			release(item);
		else
		{
			queue.Enqueue(item);
			count++;
		}
	}

	public void Clear()
	{
		var items = entries.Values.SelectMany(queue => queue).ToArray();
		entries.Clear();
		count = 0;
		foreach (var item in items)
			release(item);
	}

	public void Dispose() => Clear();
}
