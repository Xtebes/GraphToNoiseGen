namespace BurstNoiseGen
{
    public class WarpSimplexNode : OutputNoiseNode
    {
        [Input(typeConstraint = TypeConstraint.Strict, connectionType = ConnectionType.Override)]
        public float inputA, inputB;
        public string varNameA
        {
            get => (GetPort("inputA").Connection.node as OutputNoiseNode).OutputRef;
        }
        public string varNameB
        {
            get => (GetPort("inputB").Connection.node as OutputNoiseNode).OutputRef;
        }
        public override string GetCodeForJobExecution(NoiseGraph graph, int index) => $"            float {OutputRef} = noise.snoise(new float2({varNameA}, {varNameB}));\n";
    }
}
