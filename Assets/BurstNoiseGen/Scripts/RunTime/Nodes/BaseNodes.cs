namespace BurstNoiseGen
{
    using XNode;
    public abstract class NoiseNode : Node
    {
        public System.Action onValidate;
        public System.Action<NodePort, NodePort, NoiseNode> onCreateConnection;
        public System.Action<NodePort, NoiseNode> onRemoveConnection;
#if UNITY_EDITOR
        [UnityEngine.HideInInspector]
        public UnityEngine.Texture2D texture;
#endif

        private void OnValidate()
        {
            onValidate?.Invoke();
        }
        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            onCreateConnection.Invoke(from, to, this);
        }
        public override void OnRemoveConnection(NodePort port)
        {
            onRemoveConnection.Invoke(port, this);
        }
        public virtual string GetCodeForJobExecution(NoiseGraph graph, int index) => null;
        public virtual string GetCodeForVariableInitialization(NoiseGraph graph, int index) => null;
        public virtual string GetCodeForVariableDeclaration(NoiseGraph graph, int index) => null;
        public static string GetOutputRefFromNode(NoiseGraph graph, NoiseNode node)
        {
            return "output" + graph.nodes.IndexOf(node);
        }
        public override object GetValue(XNode.NodePort port) => null;
        protected string FTSWDot(float value) => value.ToString().Replace(',', '.');
    }
    public abstract class OutputNoiseNode : NoiseNode
    {
        [Output] public float output;
        public string OutputRef
        {
            get => GetOutputRefFromNode(graph as NoiseGraph, this);
        }
    }
}