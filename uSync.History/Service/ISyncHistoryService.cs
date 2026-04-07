namespace uSync.History.Service;

public interface ISyncHistoryService
{
    void ClearHistory();
    Task<IEnumerable<HistoryInfo>> GetHistoryAsync();
    bool IsEnabled();
}