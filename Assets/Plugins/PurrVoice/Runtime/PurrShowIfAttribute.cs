using UnityEngine;

namespace PurrNet.Voice
{
    public class PurrShowIfAttribute : PropertyAttribute
    {
        public string boolFieldName;
        public PurrShowIfAttribute(string boolFieldName) => this.boolFieldName = boolFieldName;
    }
}
