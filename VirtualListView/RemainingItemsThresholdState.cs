namespace Microsoft.Maui;

internal sealed class RemainingItemsThresholdState
{
	int itemCount;
	int threshold = -1;
	bool notified;

	public void Reset()
	{
		itemCount = 0;
		threshold = -1;
		notified = false;
	}

	public void UpdateItemCount(int count)
	{
		if (count > itemCount)
			notified = false;
		itemCount = count;
	}

	public bool ShouldNotify(int remainingItemsThreshold, int? remainingItems)
	{
		if (threshold != remainingItemsThreshold)
		{
			threshold = remainingItemsThreshold;
			notified = false;
		}
		if (threshold < 0 || remainingItems is null)
			return false;
		if (remainingItems > threshold)
		{
			notified = false;
			return false;
		}
		if (notified)
			return false;
		notified = true;
		return true;
	}
}
