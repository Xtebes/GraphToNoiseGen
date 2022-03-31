namespace BurstNoiseGen
{
    using XNodeEditor;
    using UnityEditor;
    [CustomNodeEditor(typeof(NoiseNode))]
    public class NoiseNodeEditor : NodeEditor
    {
       
        public override void OnBodyGUI()
        {
            base.OnBodyGUI();
            var node = (NoiseNode)target;

            if (node.texture != null)
            {
                var textureRect = EditorGUILayout.GetControlRect(false, GetWidth());
                EditorGUI.DrawPreviewTexture(textureRect, node.texture);
            }
        } 
    }
}