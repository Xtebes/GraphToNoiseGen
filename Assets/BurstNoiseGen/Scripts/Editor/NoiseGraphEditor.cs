namespace BurstNoiseGen
{
    [CustomNodeGraphEditor(typeof(NoiseGraph))]
    public class NoiseGraphEditor : XNodeEditor.NodeGraphEditor
    {
        NoiseGraph graph;
        public override void OnOpen()
        {
            graph = target as NoiseGraph;
            window.titleContent.text = "Noise Graph";
            graph.GetOrSpawnRootNode();
            graph.UpdateNodeTextures();
            AddUpdateTexturesOnValidate();
        }
        public override void OnWindowFocus()
        {
            AddUpdateTexturesOnValidate();
        }
        public override void OnWindowFocusLost()
        {
            RemoveUpdateTexturesOnValidate();
        }
        private void AddUpdateTexturesOnValidate()
        {
            foreach (var node in graph.nodes)
            {
                (node as NoiseNode).onValidate += graph.UpdateNodeTextures;
            }
        }
        private void RemoveUpdateTexturesOnValidate()
        {
            foreach (var node in graph.nodes)
            {
                (node as NoiseNode).onValidate -= graph.UpdateNodeTextures;
            }
        }
    }
}