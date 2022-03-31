namespace BurstNoiseGen
{
    using UnityEngine;
    public class NoiseRootNode : NoiseNode
    {
        [Input(connectionType = ConnectionType.Override, backingValue = ShowBackingValue.Never)]
        public float input;
    }
}