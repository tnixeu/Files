using System.Collections.Generic;

namespace Files.Backend.Models
{
    public interface IGroupedCollection<T> : ICollection<T>
    {
        IGroupedHeader Model { get; }
        void BeginBulkOperation();
        void EndBulkOperation();
    }

    /// <summary>
    /// This interface is used to allow using x:Bind for the group header template.
    /// <br/>
    /// This is needed because x:Bind does not work with generic types, however it does work with interfaces.
    /// that are implemented by generic types.
    /// </summary>
    public interface IGroupedCollectionHeader
    {
        public IGroupedHeader Model { get; set; }
    }
}
