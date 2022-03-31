namespace BurstNoiseGen
{
    using UnityEngine;
    using UnityEditor;
    using System.Linq;
    using Unity.Collections;
    using Unity.Jobs;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using XNodeEditor;
    [CustomEditor(typeof(NoiseGraph))]
    public class NoiseGraphAssetEditor : Editor
    {
        NoiseGraph graph;
        Texture2D texture;
        List<NoiseNode> orderedNodes;
        string guid;
        long localId;
        private string classStart
        {
            get 
            {
                return 
                    "namespace BurstNoiseGen\n" +
                    "{\n" +
                    "    using Unity.Jobs;\n" +
                    "    using Unity.Burst;\n" +
                    "    using Unity.Collections;\n" +
                    "    using Unity.Mathematics;\n" +
                    "    [BurstCompile]\n" +
                    "    public struct NoiseJob" + guid + " : IJobParallelFor\n" +
                    "    {\n";
            }
        }
        private string classEnd
        {
            get
            {
                return
                    "    }\n" +
                    "}";
            }
        }
        private string VarDeclaration
        {
            get
            {
                string nodesDeclaration = "";
                int[] indexes = new int[orderedNodes.Count];
                for (int i = 0; i < orderedNodes.Count - 1; i++)
                {
                    indexes[i] = graph.nodes.IndexOf(orderedNodes[i]);
                    string nodeDec = orderedNodes[i].GetCodeForVariableDeclaration(graph, indexes[i]);
                    if (nodeDec != null) nodesDeclaration += nodeDec;
                }
                nodesDeclaration += "        [WriteOnly]\n";
                nodesDeclaration += "        public NativeArray<float>\n";
                nodesDeclaration += "        ";
                for (int i = 0; i < orderedNodes.Count - 1; i++)
                {
                    
                    nodesDeclaration +=
                        $"outputArray{indexes[i]}, ";
                }
                nodesDeclaration = nodesDeclaration.Remove(nodesDeclaration.Length - 2);
                nodesDeclaration += ";\n";
                return
                    "        public int3 chunkPosition;\n" +
                    "        public int width, depth, height;\n" +
                    nodesDeclaration;
            }
        }
        private string GetNoiseMethodDefinition
        {
            get
            {
                string nodesInitialization = "";
                int[] indexes = new int[orderedNodes.Count];
                for (int i = 0; i < orderedNodes.Count - 1; i++)
                {
                    indexes[i] = graph.nodes.IndexOf(orderedNodes[i]);
                    string nodeInit = orderedNodes[i].GetCodeForVariableInitialization(graph, indexes[i]);
                    if (nodeInit != null) nodesInitialization += nodeInit;   
                }

                for (int i = 0; i < orderedNodes.Count - 1; i++)
                    nodesInitialization += $"            outputArray{indexes[i]} = new NativeArray<float>(size, Allocator.TempJob);\n";

                return
                    "        public JobHandle GetNoise(NoiseGraph graph, int3 position, int width, int height, int depth)\n" +
                    "        {\n" +
                    "            int size = width * height * depth;\n" +
                    "            this.width = width;\n" +
                    "            this.height = height;\n" +
                    "            this.depth = depth;\n" +
                    "            chunkPosition = position;\n" + 
                    nodesInitialization + 
                    "            return this.Schedule(size, 32);\n" +
                    "        }\n";
            }
        }

        private string ExecuteMethodDefinition
        {
            get
            {
                string nodesExecution = "";
                int[] indexes = new int[orderedNodes.Count];
                for (int i = 0; i < orderedNodes.Count - 1; i++)
                {
                    indexes[i] = graph.nodes.IndexOf(orderedNodes[i]);
                    nodesExecution += orderedNodes[i].GetCodeForJobExecution(graph, indexes[i]);
                }

                for (int i = 0; i < orderedNodes.Count - 1; i++)
                {
                    nodesExecution += $"            outputArray{indexes[i]}[index] = {NoiseNode.GetOutputRefFromNode(graph, orderedNodes[i])};\n";
                }

                return
                    "        public void Execute(int index)\n" +
                    "        {\n" +
                    "            int localX = index % width;\n" +
                    "            int localY = (index / width) % height;\n" +
                    "            int localZ = index / (width * height);\n" +
                    "            int3 position = chunkPosition + new int3(localX, localY, localZ);\n" +
                    nodesExecution +
                    "        }\n";
            }
        }
        public void OnEnable()
        {
            graph = target as NoiseGraph;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(graph, out guid, out localId);
        }
        private string GetFilePath()
        {
            string filePath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            filePath += AssetDatabase.GetAssetPath(graph);
            filePath = filePath.Substring(0, filePath.LastIndexOf(Path.AltDirectorySeparatorChar) + 1);
            filePath += $"NoiseJob{guid}.cs";
            return filePath;
        }
        public void CompileGraph()
        {
            string filePath = GetFilePath();
            List<string> errors;
            SetOrderedNodes();
            if (!graph.GraphIsAcceptable(out errors))
            {
                foreach (var error in errors)
                    Debug.LogError(error);
                return;
            }
            try
            {
                using (StreamWriter fs = new StreamWriter(filePath))
                {
                    fs.Write(classStart);
                    fs.Write(VarDeclaration);
                    fs.Write(ExecuteMethodDefinition);
                    fs.Write(GetNoiseMethodDefinition);
                    fs.Write(classEnd);
                }
                AssetDatabase.Refresh();  
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        private void SetOrderedNodes()
        {
            orderedNodes = graph.GetChildNodes<NoiseNode>(graph.rootNode);
        }
        public override void OnInspectorGUI()
        {
            float textureDim = Mathf.Clamp(EditorGUIUtility.currentViewWidth / 2, 0, 300);
            string filePath = GetFilePath();
            EditorGUILayout.LabelField(filePath);

            List<string> errors;
            bool canGenerate = graph.GraphIsAcceptable(out errors);
            if(GUILayout.Button("Open Graph"))
            {
                NodeEditorWindow.Open(graph);
            }
            if (canGenerate)
            {
                EditorGUILayout.HelpBox("This graph is ready to generate the file", MessageType.Info);
            }
            else
            {
                string errorMessage = "This graph has the following errors that must be fixed:\n";
                foreach (var error in errors)
                {
                    errorMessage += error + "\n";
                }
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }

            GUI.enabled = canGenerate;
            if (GUILayout.Button("Gen File"))
            {
                CompileGraph();
            }
            GUI.enabled = true;
            if (File.Exists(filePath) && canGenerate)
            {
                EditorGUILayout.HelpBox("This file already exists and will be overwritten.", MessageType.Warning);
            }
            
            string[] fieldsToRemove = new string[]
            {
                "texture",
                "onValidate",
                "onCreateConnection",
                "onRemoveConnection",
                "graph",
                "inputA",
                "inputB",
                "output",
                "position",
            };
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            if (orderedNodes == null) SetOrderedNodes();
            for (int i = 0; i < orderedNodes.Count - 1; i++)
            {
                Rect rect = EditorGUILayout.GetControlRect(false, textureDim);
                var rectTex = new Texture2D(1, 1);
                rectTex.SetPixel(1, 1, new Color(0.1f, 0.1f, 0.1f));
                rectTex.Apply();
                GUI.DrawTexture(rect, rectTex);
                var textureRect = new Rect(rect.x + rect.width - textureDim, rect.y, textureDim - verticalSpacing, rect.height);

                var nodeFields = orderedNodes[i].GetType().GetFields().Where(x => !fieldsToRemove.Contains(x.Name)).ToArray();
                var nodeNameRect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
                GUI.Label(nodeNameRect, orderedNodes[i].name);
                for (int x = 0; x < nodeFields.Length; x++)
                {
                    var fieldRect = new Rect(nodeNameRect.x, nodeNameRect.y + (x + 1) * EditorGUIUtility.singleLineHeight, nodeNameRect.width, nodeNameRect.height);

                    var fieldValue = nodeFields[x].GetValue(orderedNodes[i]).ToString();
                    var fieldName = nodeFields[x].Name;
                    if (fieldValue != null)
                        GUI.Label(fieldRect, fieldName + " : " + fieldValue);           
                }
                if (GUI.Button(new Rect(nodeNameRect.x, rect.y + textureDim - EditorGUIUtility.singleLineHeight, rect.width - textureRect.width - verticalSpacing, nodeNameRect.height), "ExportToPNG"))
                {
                    byte[] texBytes = orderedNodes[i].texture.EncodeToPNG();
                    File.WriteAllBytes(Application.dataPath + Path.DirectorySeparatorChar + "tex" + i + ".png", texBytes);
                    AssetDatabase.Refresh();
                }
                if (orderedNodes[i].texture == null) graph.UpdateNodeTextures();
                GUI.DrawTexture(textureRect, orderedNodes[i].texture);
            }
        }
    }
    struct NoiseToColorJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> input;
        [WriteOnly]
        public NativeArray<Color> output;
        public void Execute(int index)
        {
            float n = input[index];
            output[index] = new Color(n, n, n);
        }
    }
}