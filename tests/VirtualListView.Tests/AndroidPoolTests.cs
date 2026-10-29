namespace VirtualListView.Tests;

public class AndroidPoolTests
{
	[Fact]
	public void RareTypesCannotRetainAnEntireTraversalAndClearReleasesEveryRemainingTree()
	{
		var pool = new RvRecycledViewPool(() => 0);
		var holders = Enumerable.Range(0, 10_000).Select(i => new RvItemHolder(i)).ToArray();
		foreach (var holder in holders)
			pool.PutRecycledView(holder);
		Assert.Equal(9_996, holders.Sum(holder => holder.Releases));
		Assert.All(holders, holder => Assert.Equal(1, holder.NativeReturns));
		pool.Clear();
		pool.Clear();
		Assert.All(holders, holder => Assert.Equal(1, holder.Releases));
	}

	[Fact]
	public void PerTypeOverflowIsReleasedBeforeNativeDiscardAndTakenTreesAreNotReleased()
	{
		var pool = new RvRecycledViewPool(() => 20);
		var holders = Enumerable.Range(0, 3).Select(_ => new RvItemHolder(1)).ToArray();
		foreach (var holder in holders)
			pool.PutRecycledView(holder);
		Assert.Equal(2, pool.GetRecycledViewCount(1));
		Assert.Equal(1, holders[2].Releases);
		Assert.Equal(1, holders[2].NativeReturns);
		var taken = pool.GetRecycledView(1);
		pool.Clear();
		Assert.Equal(0, ((RvItemHolder)taken).Releases);
		Assert.Equal(2, holders.Sum(holder => holder.Releases));
	}

	[Fact]
	public void TotalSpareBudgetTracksTheViewportRatherThanDatasetSize()
	{
		var visible = 10;
		var pool = new RvRecycledViewPool(() => visible);
		var holders = Enumerable.Range(0, 20).Select(i => new RvItemHolder(i)).ToList();
		foreach (var holder in holders)
			pool.PutRecycledView(holder);
		Assert.Equal(0, holders.Sum(holder => holder.Releases));
		visible = 1;
		holders.Add(new RvItemHolder(21));
		pool.PutRecycledView(holders.Last());
		Assert.Equal(17, holders.Sum(holder => holder.Releases));
		pool.Clear();
		Assert.Equal(21, holders.Sum(holder => holder.Releases));
	}
}
