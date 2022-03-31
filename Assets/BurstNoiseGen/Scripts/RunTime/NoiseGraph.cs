namespace BurstNoiseGen
{
    using System.Collections;
    using System.Collections.Generic;
    using XNode;
    using UnityEngine;
    using Unity.Collections;
    using Unity.Mathematics;
    using System.Linq;
    using System;
    using Unity.Jobs;
    using UnityEditor;
    public class NoiseGraph : NodeGraph
    {   
        [SerializeField]
        public NoiseRootNode rootNode;
        public Action<int3, int, int, int, float[]> onNoiseGot;
        public static readonly string fn = "NoiseJob";
        public int width, height;
        public string noiseClassName
        {
            get
            {
                string guid;
                long localId;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out guid, out localId);
                return GetType().Namespace + "." + fn + guid;
            }
        }
        private void OnValidate()
        {
            GetOrSpawnRootNode();
        }
        public float[] GetNoiseImmediate(int3 position, int width, int height, int depth)
        {
            int size = width * height * depth;
            var jobType = Type.GetType(noiseClassName);
            var jobInstance = Activator.CreateInstance(jobType);
           
            var getNoiseMethod = jobType.GetMethod("GetNoise");
            jobType.GetField("chunkPosition").SetValue(jobInstance, position);
            jobType.GetField("width").SetValue(jobInstance, width);
            jobType.GetField("height").SetValue(jobInstance, height);
            jobType.GetField("depth").SetValue(jobInstance, depth);
            var jobHandle = (JobHandle)getNoiseMethod.Invoke(jobInstance, new object[] { this, position, width, height, depth });
            jobHandle.Complete();
            List<NoiseNode> orderedNodes = GetChildNodes<NoiseNode>(rootNode);
            var noise = ((NativeArray<float>)jobType.GetField($"outputArray{nodes.IndexOf(orderedNodes[orderedNodes.Count - 2])}").GetValue(jobInstance)).ToArray();
            for (int i = 0; i < orderedNodes.Count - 1; i++)
            {
                ((NativeArray<float>)jobType.GetField($"outputArray{nodes.IndexOf(orderedNodes[i])}").GetValue(jobInstance)).Dispose();
            }
            return noise;
        }
        public Dictionary<int, float[]> GetAllNoiseImmediate(int3 position, int width, int height, int depth)
        {
            var jobType = Type.GetType(noiseClassName);
            var jobInstance = Activator.CreateInstance(jobType);
            Dictionary<int, float[]> result = new Dictionary<int, float[]>();
            var getNoiseMethod = jobType.GetMethod("GetNoise");
            jobType.GetField("chunkPosition").SetValue(jobInstance, position);
            jobType.GetField("width").SetValue(jobInstance, width);
            jobType.GetField("height").SetValue(jobInstance, height);
            jobType.GetField("depth").SetValue(jobInstance, depth);
            var jobHandle = (JobHandle)getNoiseMethod.Invoke(jobInstance, new object[] { this, position, width, height, depth });
            jobHandle.Complete();
            List<NoiseNode> orderedNodes = GetChildNodes<NoiseNode>(rootNode);
            for (int i = 0; i < orderedNodes.Count - 1; i++)
            {
                int nodeIndex = nodes.IndexOf(orderedNodes[i]);
                NativeArray<float> array = (NativeArray<float>)jobType.GetField($"outputArray{nodeIndex}").GetValue(jobInstance);
                result.Add(nodeIndex, array.ToArray());
                array.Dispose();
            }
            return result;
        }
#if UNITY_EDITOR
        public void UpdateNodeTextures()
        {
            Dictionary<int, float[]> noiseAtNodes = GetAllNoiseImmediate(int3.zero, 256, 256, 1);
            Color[] colors = new Color[256 * 256];
            foreach (var pain in noiseAtNodes)
            {
                var node = nodes[pain.Key] as NoiseNode;
                if (!node) continue;
                if (node.texture == null) node.texture = new Texture2D(256, 256);
                for (int i = 0; i < colors.Length; i++)
                {
                    var noiseVal = noiseAtNodes[pain.Key][i];
                    colors[i] = new Color(noiseVal, noiseVal, noiseVal);
                }
                node.texture = new Texture2D(256, 256);
                node.texture.SetPixels(colors);
                node.texture.Apply();
               
            }
        }
#endif
        /// <summary>
        /// When the noise is ready, onNoiseGot is called with the specified dimensions and with the result.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public IEnumerator GetNoise(int3 position, int width, int height, int depth)
        {
            int length = width * height * depth;
            var jobType = Type.GetType(noiseClassName);
            NativeArray<float> output = new NativeArray<float>(length, Allocator.TempJob);
            var jobInstance = Activator.CreateInstance(jobType);
            var scheduleMethodInfo = typeof(IJobParallelForExtensions).GetMethod("Schedule").MakeGenericMethod(jobType);
            jobType.GetField("chunkPosition").SetValue(jobInstance, position);
            jobType.GetField("width").SetValue(jobInstance, width);
            jobType.GetField("height").SetValue(jobInstance, height);
            jobType.GetField("depth").SetValue(jobInstance, depth);
            jobType.GetField("output").SetValue(jobInstance, output);
            var jobHandle = (JobHandle)scheduleMethodInfo.Invoke(null, new object[] { jobInstance, length, 16, default });
            while (!jobHandle.IsCompleted) yield return null;
            jobHandle.Complete();
            var noise = output.ToArray();
            output.Dispose();
            onNoiseGot?.Invoke(position, width, height, depth, noise);
        }


        /// <summary>
        /// Gets all of the nodes, depth first, on every connection made to any of the input ports of the node that's being searched.
        /// </summary>
        /// <param name="node"> The root node to search, not included on the list </param>
        /// <returns></returns>
        public List<T> GetChildNodes<T>(T node, Dictionary<T, bool> nodesGot = null) where T : Node
        {
            List<T> childNodes = new List<T>();
            if (nodesGot == null) nodesGot = new Dictionary<T, bool>();
            if (!nodesGot.ContainsKey(node)) nodesGot.Add(node, false);
            if (nodesGot[node]) return childNodes;
            nodesGot[node] = true;
            foreach (var inputPort in node.Inputs.ToArray())
            {
                foreach (var connection in inputPort.GetConnections())
                {
                    childNodes.AddRange(GetChildNodes((T)connection.node, nodesGot));
                }
            }
            childNodes.Add(node);
            return childNodes;
        }



        public bool CheckForCycleInGraph(List<Node> nodesVisited, List<Node> nodesExplored, Node rootNode)
        {
            if (nodesVisited.Contains(rootNode)) return false;
            if (nodesExplored.Contains(rootNode)) return true;
            nodesExplored.Add(rootNode);
            foreach (var inputPort in rootNode.Inputs.ToArray())
            {
                foreach (var connection in inputPort.GetConnections())
                {
                    if (!nodesVisited.Contains(connection.node))
                        if (CheckForCycleInGraph(nodesVisited, nodesExplored, connection.node)) return true;
                }
            }
            nodesExplored.Remove(rootNode);
            nodesVisited.Add(rootNode);
            return false;
        }


        public NoiseRootNode GetOrSpawnRootNode()
        {
            if (rootNode == null)
            {
                try
                {
                    rootNode = (NoiseRootNode)nodes.First(node => node.name == "Root" && (node as NoiseRootNode));
                }
                catch
                {
                    rootNode = (NoiseRootNode)AddNode(typeof(NoiseRootNode));
                    rootNode.name = "Root";
                }
            }
            return rootNode;
        }


        /// <summary>
        /// Does all the necessary checks to see if the graph is ready for generation.
        /// </summary>
        public bool GraphIsAcceptable()
        {
            GetOrSpawnRootNode();
            if (CheckForCycleInGraph(new List<Node>(), new List<Node>(), rootNode)) return false;
            return true;
        }
        public bool GraphIsAcceptable(out List<string> errors)
        {
            bool isAcceptable = true;
            errors = new List<string>();
            GetOrSpawnRootNode();
            if (CheckForCycleInGraph(new List<Node>(), new List<Node>(), rootNode))
            {
                errors.Add("Graph has one or more cycles. From the root to its children, the graph must be a tree.");
                isAcceptable = false;
            }
            return isAcceptable;
        }
    }
}