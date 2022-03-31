namespace BurstNoiseGen
{
    public class SimplexNoiseNode : OutputNoiseNode
    {
        public float scale;
        public float lacunarity;
        public float persistence;
        public int seed;
        public int octaves;
        public override string GetCodeForVariableInitialization(NoiseGraph graph, int index)
        {
            return
                $"            var node{index} = (SimplexNoiseNode)graph.nodes[{index}];\n" +
                $"            octavePositions{index} = new NativeArray<float3>(node{index}.octaves, Allocator.TempJob);\n" +
                $"            System.Random rand{index} = new System.Random(node{index}.seed);\n" +
                $"            scale{index} = node{index}.scale;\n" +
                $"            lacunarity{index} = node{index}.lacunarity;\n" +
                $"            persistence{index} = node{index}.persistence;\n" +
                $"            for (int i = 0; i < node{index}.octaves; i++)\n" +
                 "            {\n" +
                $"                octavePositions{index}[i] = new float3(rand{index}.Next(-10000, 10000), rand{index}.Next(-10000, 10000), rand{index}.Next(-10000, 10000));\n" +
                 "            }\n";
        } 
        public override string GetCodeForVariableDeclaration(NoiseGraph graph, int index)
        {
            return
                $"        [DeallocateOnJobCompletion][NativeDisableParallelForRestriction]\n" +
                $"        public NativeArray<float3> octavePositions{index};\n" +
                $"        public float scale{index}, persistence{index}, lacunarity{index};\n";
        }
        public override string GetCodeForJobExecution(NoiseGraph graph, int index)
        {
            string gain = "gain" + index;
            string det = "det" + index;
            string val = "val" + index;
            string maxVal = "maxVal" + index;
            string position = "position" + index;
            return
                $"            float {gain} = 1;\n" +
                $"            float {det} = 1;\n" +
                $"            float {val} = 0;\n" +
                $"            float {maxVal} = 0;\n" +
                $"            float3 {position} = (float3)position * scale{index};\n" +
                $"            for (int i = 0; i < octavePositions{index}.Length; i++)\n" +
                 "            {\n" +
                $"                 {val} += noise.snoise(octavePositions{index}[i] + {position} * {det});\n" +
                $"                 {maxVal} += {gain};\n" +
                $"                 {gain} *= persistence{index};\n" +
                $"                 {det} *= lacunarity{index};\n" +
                 "            }\n" +
                $"            float {OutputRef} = {val} / {maxVal};\n";
        }
    }
}
    