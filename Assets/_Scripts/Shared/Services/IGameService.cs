
partial class Services { public static IGameService Game; }

public interface IGameService : IService
{
    public bool IsPaused { get; }

    public void Pause();
    public void Unpause(bool lockCursor = false);
    public void TogglePause();

    public IEvent OnGamePaused { get; }
    public IEvent OnGameUnpaused { get; }
}
