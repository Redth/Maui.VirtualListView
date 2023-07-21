using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Maui.Adapters
{
    public interface IVirtualListViewAdapter
    {
        int Sections { get; }

        object Section(int sectionIndex);

        int ItemsForSection(int sectionIndex);

        object Item(int sectionIndex, int itemIndex);

        event EventHandler OnDataInvalidated;

        void InvalidateData();
    }
}
