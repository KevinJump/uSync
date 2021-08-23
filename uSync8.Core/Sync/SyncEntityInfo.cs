namespace uSync8.Core.Sync
{
    /// <summary>
    ///  things that let exporter know whats happening. 
    /// </summary>
    public class SyncEntityInfo
    {
        public string SectionAlias { get; set; }
        public string TreeAlias { get; set; }

        public string PickerView { get; set; }

        public bool DoNotPickContainers { get; set; }

    }

}
