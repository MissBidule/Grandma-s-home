using System;

namespace PurrNet.Voice
{
    public abstract class FilterInstance
    {
        public virtual void Process(ref float inputSample, int frequency, float strength) {}
        
        public virtual void Process(ArraySegment<float> inputSample, int frequency, float strength) {}
    }
}
