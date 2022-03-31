namespace BurstNoiseGen
{
    public class AddNode : OutputNoiseNode
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
        public override string GetCodeForJobExecution(NoiseGraph graph, int index)
        {
            return
                 $" float {OutputRef} = {varNameA} + {varNameB};\n";
        }
    }
}