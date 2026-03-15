namespace TourGuideAPP.Services;

public class NarrationService
{
    private CancellationTokenSource? _cts;
    private bool _isSpeaking = false;

    public async Task SpeakAsync(string text)
    {
        if (_isSpeaking || string.IsNullOrEmpty(text)) return;

        try
        {
            _cts = new CancellationTokenSource();
            _isSpeaking = true;
            await TextToSpeech.SpeakAsync(text, _cts.Token);
        }
        finally
        {
            _isSpeaking = false;
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _isSpeaking = false;
    }

    public bool IsSpeaking => _isSpeaking;
}