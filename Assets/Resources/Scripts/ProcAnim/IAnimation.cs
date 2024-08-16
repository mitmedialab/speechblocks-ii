public interface IAnimation {
    void Init(params object[] args);
    void Start();
    void Stop();
    bool IsGoing();
}