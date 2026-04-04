using UnityEngine;
using System.Collections.Generic;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class SampleVisualReferenceRigBehaviour : MonoBehaviour
    {
        [SerializeField] private bool showLegend = true;
        [SerializeField] private Rect legendRect = new Rect(16f, 320f, 220f, 150f);
        private readonly List<Transform> labelTransforms = new List<Transform>();

        private void OnEnable()
        {
            EnsureVisuals();
        }

        public void EnsureVisuals()
        {
            if (transform.childCount > 0)
            {
                return;
            }

            CreatePrimitive(PrimitiveType.Plane, "Ground", new Vector3(0f, -1.25f, 12f), new Vector3(3f, 1f, 3f), new Color(0.26f, 0.3f, 0.26f, 1f), false);
            CreatePrimitive(PrimitiveType.Cube, "Center Tower", new Vector3(0f, 0f, 12f), new Vector3(1.5f, 2.5f, 1.5f), new Color(0.95f, 0.72f, 0.22f, 1f), true);
            CreatePrimitive(PrimitiveType.Cube, "Left Marker", new Vector3(-4f, -0.25f, 8f), new Vector3(1f, 1.5f, 1f), new Color(0.17f, 0.63f, 0.95f, 1f), true);
            CreatePrimitive(PrimitiveType.Cube, "Right Marker", new Vector3(4f, 0.75f, 16f), new Vector3(1f, 3.5f, 1f), new Color(0.91f, 0.36f, 0.35f, 1f), true);
            CreatePrimitive(PrimitiveType.Sphere, "Near Target", new Vector3(-1.5f, 1f, 6f), new Vector3(1.25f, 1.25f, 1.25f), new Color(0.3f, 0.83f, 0.43f, 1f), true);
            CreatePrimitive(PrimitiveType.Sphere, "Far Target", new Vector3(2.5f, 1.5f, 20f), new Vector3(2f, 2f, 2f), new Color(0.73f, 0.47f, 0.93f, 1f), true);
            CreatePrimitive(PrimitiveType.Capsule, "Depth Pole", new Vector3(0f, 2f, 24f), new Vector3(1.5f, 4f, 1.5f), new Color(0.96f, 0.9f, 0.48f, 1f), true);
            CreatePrimitive(PrimitiveType.Cube, "Center Line", new Vector3(0f, -0.9f, 12f), new Vector3(0.2f, 0.08f, 26f), new Color(0.9f, 0.9f, 0.9f, 1f), false);
            CreatePrimitive(PrimitiveType.Cube, "Cross Line", new Vector3(0f, -0.9f, 12f), new Vector3(10f, 0.08f, 0.2f), new Color(0.72f, 0.72f, 0.72f, 1f), false);
        }

        private void LateUpdate()
        {
            var targetCamera = Camera.main;
            if (targetCamera == null)
            {
                return;
            }

            for (var index = 0; index < labelTransforms.Count; index++)
            {
                var labelTransform = labelTransforms[index];
                if (labelTransform == null)
                {
                    continue;
                }

                var cameraTransform = targetCamera.transform;
                var direction = labelTransform.position - cameraTransform.position;
                if (direction.sqrMagnitude <= 0f)
                {
                    continue;
                }

                labelTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private void CreatePrimitive(PrimitiveType primitiveType, string objectName, Vector3 localPosition, Vector3 localScale, Color color, bool createLabel)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(transform, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = Quaternion.identity;
            primitive.transform.localScale = localScale;
            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Standard");
                if (shader != null)
                {
                    var material = new Material(shader);
                    material.color = color;
                    renderer.sharedMaterial = material;
                }
            }

            if (!createLabel)
            {
                return;
            }

            CreateLabel(primitive.transform, objectName);
        }

        private void CreateLabel(Transform parent, string labelText)
        {
            var labelObject = new GameObject($"{labelText} Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = Vector3.up * GetLabelHeight(parent.localScale.y);
            labelObject.transform.localRotation = Quaternion.identity;
            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = labelText;
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.1f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            labelTransforms.Add(labelObject.transform);
        }

        private static float GetLabelHeight(float objectHeight)
        {
            return Mathf.Max(1.2f, objectHeight * 0.65f + 0.35f);
        }

        private void OnGUI()
        {
            if (!showLegend)
            {
                return;
            }

            GUILayout.BeginArea(legendRect, GUI.skin.box);
            GUILayout.Label("Sample Visual Rig");
            DrawLegendLine("Yellow", "Center Tower / Depth Pole");
            DrawLegendLine("Blue", "Left Marker");
            DrawLegendLine("Red", "Right Marker");
            DrawLegendLine("Green", "Near Target");
            DrawLegendLine("Purple", "Far Target");
            DrawLegendLine("White", "Center / Cross Line");
            GUILayout.EndArea();
        }

        private static void DrawLegendLine(string colorName, string label)
        {
            GUILayout.Label($"{colorName}: {label}");
        }
    }
}
