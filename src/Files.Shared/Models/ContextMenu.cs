using Files.Shared.Enums;
using System.Collections.Generic;

namespace Files.Shared.Models
{
    public class Win32ContextMenu
    {
        public List<Win32ContextMenuItem> Items { get; set; }
    }

    public class Win32ContextMenuItem
    {
        public string IconBase64 { get; set; }
        public int ID { get; set; } // Valid only in current menu to invoke item
        public string Label { get; set; }
        public string CommandString { get; set; }
        public MenuItemType Type { get; set; }
        public List<Win32ContextMenuItem> SubItems { get; set; }
    }
}