#nullable enable
namespace Microsoft.Maui;

internal sealed class CvDataSourceState
{
	public CvDataSourceState(PositionalViewSelector projection) => Projection = new(projection);

	public PositionalViewSelector Projection { get; private set; }
	public KeyedRows? Rows { get; private set; }
	public int Count => Projection.TotalCount;

	public void Accept(PositionalViewSelector projection, KeyedRows? rows)
	{
		Projection = new(projection);
		Rows = rows;
	}

	public void Reload(PositionalViewSelector projection, KeyedRows? rows, Action reload)
	{
		Accept(projection, rows);
		reload();
	}

	public void Apply(PositionalViewSelector projection, KeyedRows? rows, Action<Action> batch, Action edits)
	{
		var next = new PositionalViewSelector(projection);
		// A pending reload can request old counts and cells before the batch callback.
		batch(() =>
		{
			Projection = next;
			Rows = rows;
			edits();
		});
	}
}
