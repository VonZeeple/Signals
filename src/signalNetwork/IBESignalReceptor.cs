namespace signals.src.signalNetwork
{
    internal interface IBESignalReceptor
    {
        void OnValueChanged(NodePos pos, byte value);
    }
}