using UnityEngine;

public interface IMicrophone
{
    string name { get; }

    int minFrequency { get; }

    int maxFrequency { get; }

    AudioClip StartRecording(int frequency);

    int position { get; }

    void StopRecording();
}
