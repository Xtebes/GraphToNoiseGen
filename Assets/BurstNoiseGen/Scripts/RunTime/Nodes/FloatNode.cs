namespace BurstNoiseGen
{
    public class FloatNode : OutputNoiseNode
    {
        public float value;
        public override string GetCodeForVariableDeclaration(NoiseGraph graph, int index)
        {
            return
                $"        float {OutputRef};\n";
        }
        public override string GetCodeForVariableInitialization(NoiseGraph graph, int index)
        {
            return 
                $"            {OutputRef} = (graph.nodes[{index}] as FloatNode).value;\n";
        }
    }
}