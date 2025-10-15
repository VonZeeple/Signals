namespace signals.src.signalNetwork
{
    public interface IBESignalReceptor
    {
        void OnValueChanged(NodePos pos, byte value);
    }
}