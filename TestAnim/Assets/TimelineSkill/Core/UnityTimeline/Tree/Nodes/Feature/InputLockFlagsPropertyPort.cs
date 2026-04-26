using System;
using TreeDesigner;

namespace UnityTimeline
{
    [Serializable]
    public class InputLockFlagsPropertyPort : PropertyPort<InputLockFlags>
    {
        public InputLockFlagsPropertyPort() { Value = InputLockFlags.All; }
        public InputLockFlagsPropertyPort(InputLockFlags value) { Value = value; }
    }
}
