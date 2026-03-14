#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazeHouseGenerator))]
public class MazeHouseGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var gen = (MazeHouseGenerator)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Maze House"))
        {
            if (!Application.isPlaying)
            {
                Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Generate Maze House");
            }

            gen.GenerateMazeHouse();
        }

        EditorGUILayout.HelpBox(
            "Assign HouseBoundary (with BoxCollider or Renderer), then click 'Generate Maze House' to build the maze inside the boundary.",
            MessageType.Info
        );
    }
}
#endif
