using Files.Backend.Helpers;
using System.Collections.Generic;

namespace Files.Backend.Models
{
    public class GroupedCollectionBase<T> : BulkConcurrentObservableCollection<T>, IGroupedCollectionHeader, IGroupedCollection<T>
    {
        public IGroupedHeader Model { get; set; }

        public GroupedCollectionBase(IEnumerable<T> items) : base(items)
        {
        }

        public GroupedCollectionBase() : base()
        {
        }

        public void InitializeExtendedGroupHeaderInfoAsync()
        {
            if (GetExtendedGroupHeaderInfo is null)
            {
                return;
            }

            Model.ResumePropertyChangedNotifications(false);

            GetExtendedGroupHeaderInfo.Invoke(this);
            Model.Initialized = true;
            if (isBulkOperationStarted)
            {
                Model.PausePropertyChangedNotifications();
            }
        }

        public override void BeginBulkOperation()
        {
            base.BeginBulkOperation();
            Model.PausePropertyChangedNotifications();
        }

        public override void EndBulkOperation()
        {
            base.EndBulkOperation();
            Model.ResumePropertyChangedNotifications();
        }
    }
}
