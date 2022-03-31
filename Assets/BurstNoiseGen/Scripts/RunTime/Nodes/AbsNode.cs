namespace BurstNoiseGen
{
    public class AbsNode : OutputNoiseNode
    {
        [Input(typeConstraint = TypeConstraint.Strict, connectionType = ConnectionType.Override)]
        public float inputA;
        public string varNameA
        {
            get => (GetPort("inputA").Connection.node as OutputNoiseNode).OutputRef;
        }
        public override string GetCodeForJobExecution(NoiseGraph graph, int index)
        {
            return
                 $"            float {OutputRef} = math.abs({varNameA});\n";
        }
    }
}