namespace TodoList.Infra.Data.Services;

public interface IDatabaseSwapService
{
    void DoSwap(string sourceFilename, string targetFilename);
}