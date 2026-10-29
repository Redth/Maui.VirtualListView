using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Microsoft.Maui;

class IrSource : IReadOnlyList<IrDataWrapper>, INotifyCollectionChanged
{
	public IrSource(IMauiContext context, PositionalViewSelector positionalViewSelector, IVirtualListView virtualListView)
	{
		Context = context;
		PositionalViewSelector = positionalViewSelector;
		VirtualListView = virtualListView;
	}

	public readonly IMauiContext Context;

	readonly PositionalViewSelector PositionalViewSelector;
	readonly IVirtualListView VirtualListView;
	KeyedRow[] rows;
	KeyedRows acceptedRows;

	public event NotifyCollectionChangedEventHandler CollectionChanged;

	public void Reset()
	{
		rows = null;
		acceptedRows = null;
		CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	IrDataWrapper Wrap(KeyedRow row) => new(Context, row.Position, row.Data, PositionalViewSelector, VirtualListView, row.Key);
	KeyedRow CurrentRow(KeyedRow row) => acceptedRows is not null && acceptedRows.Indices.TryGetValue(row.Key, out var index)
		? acceptedRows.Rows[index] : row;

	internal void Apply(KeyedUpdatePlan plan)
	{
		if (plan.RefreshedItems is not null)
		{
			foreach (var edit in plan.Edits)
			{
				var item = plan.RefreshedItems[edit.Index];
				var old = this[edit.Index];
				var replacement = new IrDataWrapper(Context, item.Position, item.Data, PositionalViewSelector, VirtualListView);
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Replace, replacement, old, edit.Index));
			}
			return;
		}
		acceptedRows = plan.After;
		if (plan.Reset)
		{
			rows = plan.After.Rows;
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			return;
		}
		rows = plan.Before.Rows;
		foreach (var edit in plan.Edits)
		{
			var next = rows.ToList();
			NotifyCollectionChangedEventArgs notification;
			switch (edit.Kind)
			{
				case RowEditKind.Insert:
					var inserted = plan.After.Rows[edit.Index];
					next.Insert(edit.Index, inserted);
					notification = new(NotifyCollectionChangedAction.Add, Wrap(inserted), edit.Index);
					break;
				case RowEditKind.Remove:
					var removed = next[edit.Index];
					next.RemoveAt(edit.Index);
					notification = new(NotifyCollectionChangedAction.Remove, Wrap(removed), edit.Index);
					break;
				case RowEditKind.Move:
					var moved = next[edit.From];
					next.RemoveAt(edit.From);
					next.Insert(edit.Index, moved);
					notification = new(NotifyCollectionChangedAction.Move, Wrap(CurrentRow(moved)), edit.Index, edit.From);
					break;
				case RowEditKind.Replace:
					var old = next[edit.Index];
					var replacement = plan.After.Rows[edit.Index];
					next[edit.Index] = replacement;
					notification = new(NotifyCollectionChangedAction.Replace, Wrap(replacement), Wrap(old), edit.Index);
					break;
				default:
					throw new InvalidOperationException("Unknown row edit.");
			}
			rows = next.ToArray();
			CollectionChanged?.Invoke(this, notification);
		}
		rows = plan.After.Rows;
	}

	public int Count
		=> rows?.Length ?? PositionalViewSelector?.TotalCount ?? 0;

	public bool IsReadOnly
		=> true;

	public IrDataWrapper this[int index]
	{
		get
		{
			if (rows is not null)
				return Wrap(CurrentRow(rows[index]));
			var info = PositionalViewSelector?.GetInfo(index);

			if (info == null)
				return default;

			var data = PositionalViewSelector?.Adapter?.DataFor(info.Kind, info.SectionIndex, info.ItemIndex);

			return new(Context, info, data, PositionalViewSelector, VirtualListView);
		}
		set => throw new NotImplementedException();
	}

	public int IndexOf(IrDataWrapper item)
	{
		throw new NotImplementedException();
	}

	public void Insert(int index, IrDataWrapper item)
	{
		throw new NotImplementedException();
	}

	public void RemoveAt(int index)
	{
		throw new NotImplementedException();
	}

	public void Add(IrDataWrapper item)
	{
		throw new NotImplementedException();
	}

	public void Clear()
	{
		throw new NotImplementedException();
	}

	public bool Contains(IrDataWrapper item)
	{
		throw new NotImplementedException();
	}

	public void CopyTo(IrDataWrapper[] array, int arrayIndex)
	{
		throw new NotImplementedException();
	}

	public bool Remove(IrDataWrapper item)
	{
		throw new NotImplementedException();
	}

	public IEnumerator<IrDataWrapper> GetEnumerator()
	{
		for (var index = 0; index < Count; index++)
			yield return this[index];
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
