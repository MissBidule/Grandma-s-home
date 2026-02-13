using System;

namespace PurrNet.Voice
{
    [Serializable]
    public struct DeviceID
    {
        public string id;
        public string displayName;

        public DeviceID(string id, string displayName)
        {
            this.id = id;
            this.displayName = displayName;
        }

        public override string ToString()
        {
            if (displayName == id)
                return displayName;
            return $"{displayName} ({id})";
        }
    }

    [Serializable]
    internal struct DeviceIDArray
    {
        public DeviceID[] items;
    }
}
