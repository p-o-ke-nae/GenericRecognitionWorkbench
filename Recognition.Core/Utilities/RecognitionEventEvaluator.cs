namespace Recognition.Core;

public static class RecognitionEventEvaluator
{
    public static bool ShouldTrigger(RecognitionEventMode mode, bool previousDetected, bool currentDetected)
    {
        return mode switch
        {
            RecognitionEventMode.OnDetectedEnter => !previousDetected && currentDetected,
            RecognitionEventMode.OnDetectedExit => previousDetected && !currentDetected,
            RecognitionEventMode.WhileDetected => currentDetected,
            _ => false
        };
    }
}
