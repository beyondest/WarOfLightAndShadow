using UnityEngine;

using UnityEditor;

namespace Editor
{

    [CustomEditor(typeof(LineMeasurement))]
    public class LineMeasurementEditor : UnityEditor.Editor
    {
        private LineMeasurement lineMeasurement;
        private const float LabelOffset = 0.2f;

        private void OnEnable()
        {
            lineMeasurement = (LineMeasurement)target;
        }

        public void OnSceneGUI()
        {
            // Allow the user to move the start and end points
            lineMeasurement.startPoint = Handles.PositionHandle(lineMeasurement.startPoint, Quaternion.identity);
            lineMeasurement.endPoint = Handles.PositionHandle(lineMeasurement.endPoint, Quaternion.identity);

            // Draw the line between the two points
            Handles.color = Color.cyan;
            Handles.DrawLine(lineMeasurement.startPoint, lineMeasurement.endPoint);

            // Calculate the length of the line
            float lineLength = Vector3.Distance(lineMeasurement.startPoint, lineMeasurement.endPoint);
        
            // Display the length in the scene view
            Vector3 labelPosition = (lineMeasurement.startPoint + lineMeasurement.endPoint) / 2f;
        
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
        
            Handles.Label(labelPosition + Vector3.up * LabelOffset, $"Length: {lineLength:F2}", style);

            // Repaint the scene view to ensure constant updates
            if (GUI.changed)
            {
                EditorUtility.SetDirty(lineMeasurement);
            }
        }
    }

}