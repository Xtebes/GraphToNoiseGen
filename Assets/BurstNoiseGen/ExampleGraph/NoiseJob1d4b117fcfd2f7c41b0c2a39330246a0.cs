namespace BurstNoiseGen
{
    using Unity.Jobs;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Mathematics;
    [BurstCompile]
    public struct NoiseJob1d4b117fcfd2f7c41b0c2a39330246a0 : IJobParallelFor
    {
        public int3 chunkPosition;
        public int width, depth, height;
        [DeallocateOnJobCompletion][NativeDisableParallelForRestriction]
        public NativeArray<float3> octavePositions1;
        public float scale1, persistence1, lacunarity1;
        [DeallocateOnJobCompletion][NativeDisableParallelForRestriction]
        public NativeArray<float3> octavePositions2;
        public float scale2, persistence2, lacunarity2;
        [WriteOnly]
        public NativeArray<float>
        outputArray1, outputArray2, outputArray3, outputArray4, outputArray5;
        public void Execute(int index)
        {
            int localX = index % width;
            int localY = (index / width) % height;
            int localZ = index / (width * height);
            int3 position = chunkPosition + new int3(localX, localY, localZ);
            float gain1 = 1;
            float det1 = 1;
            float val1 = 0;
            float maxVal1 = 0;
            float3 position1 = (float3)position * scale1;
            for (int i = 0; i < octavePositions1.Length; i++)
            {
                 val1 += noise.snoise(octavePositions1[i] + position1 * det1);
                 maxVal1 += gain1;
                 gain1 *= persistence1;
                 det1 *= lacunarity1;
            }
            float output1 = val1 / maxVal1;
            float gain2 = 1;
            float det2 = 1;
            float val2 = 0;
            float maxVal2 = 0;
            float3 position2 = (float3)position * scale2;
            for (int i = 0; i < octavePositions2.Length; i++)
            {
                 val2 += noise.snoise(octavePositions2[i] + position2 * det2);
                 maxVal2 += gain2;
                 gain2 *= persistence2;
                 det2 *= lacunarity2;
            }
            float output2 = val2 / maxVal2;
            float output3 = noise.snoise(new float2(output1, output2));
            float output4 = noise.snoise(new float2(output3, output2));
            float output5 = math.abs(output4);
            outputArray1[index] = output1;
            outputArray2[index] = output2;
            outputArray3[index] = output3;
            outputArray4[index] = output4;
            outputArray5[index] = output5;
        }
        public JobHandle GetNoise(NoiseGraph graph, int3 position, int width, int height, int depth)
        {
            int size = width * height * depth;
            this.width = width;
            this.height = height;
            this.depth = depth;
            chunkPosition = position;
            var node1 = (SimplexNoiseNode)graph.nodes[1];
            octavePositions1 = new NativeArray<float3>(node1.octaves, Allocator.TempJob);
            System.Random rand1 = new System.Random(node1.seed);
            scale1 = node1.scale;
            lacunarity1 = node1.lacunarity;
            persistence1 = node1.persistence;
            for (int i = 0; i < node1.octaves; i++)
            {
                octavePositions1[i] = new float3(rand1.Next(-10000, 10000), rand1.Next(-10000, 10000), rand1.Next(-10000, 10000));
            }
            var node2 = (SimplexNoiseNode)graph.nodes[2];
            octavePositions2 = new NativeArray<float3>(node2.octaves, Allocator.TempJob);
            System.Random rand2 = new System.Random(node2.seed);
            scale2 = node2.scale;
            lacunarity2 = node2.lacunarity;
            persistence2 = node2.persistence;
            for (int i = 0; i < node2.octaves; i++)
            {
                octavePositions2[i] = new float3(rand2.Next(-10000, 10000), rand2.Next(-10000, 10000), rand2.Next(-10000, 10000));
            }
            outputArray1 = new NativeArray<float>(size, Allocator.TempJob);
            outputArray2 = new NativeArray<float>(size, Allocator.TempJob);
            outputArray3 = new NativeArray<float>(size, Allocator.TempJob);
            outputArray4 = new NativeArray<float>(size, Allocator.TempJob);
            outputArray5 = new NativeArray<float>(size, Allocator.TempJob);
            return this.Schedule(size, 32);
        }
    }
}