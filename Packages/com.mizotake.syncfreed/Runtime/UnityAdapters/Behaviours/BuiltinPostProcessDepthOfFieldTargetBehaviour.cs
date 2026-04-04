using System;
using System.Reflection;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BuiltinPostProcessDepthOfFieldTargetBehaviour : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private MonoBehaviour postProcessLayer;
        [SerializeField] private MonoBehaviour postProcessVolume;
        [SerializeField] private ScriptableObject profile;
        [SerializeField] private ScriptableObject depthOfField;
        [SerializeField] private float volumePriority = 100f;
        [SerializeField] private bool enableDepthOfField = true;
        [SerializeField] private float aperture = 5.6f;
        [SerializeField] private float focalLength = 50f;
        [SerializeField] private int kernelSize = 1;
        [SerializeField] [TextArea] private string setupStatus = string.Empty;

        public bool IsAvailable => GetPostProcessLayerType() != null && GetPostProcessVolumeType() != null && GetPostProcessProfileType() != null && GetDepthOfFieldType() != null;
        public ScriptableObject Profile => profile;
        public ScriptableObject DepthOfField => depthOfField;
        public bool IsConfigured => postProcessLayer != null && postProcessVolume != null && profile != null && depthOfField != null;
        public string SetupStatus => setupStatus;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureSetup();
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsureSetup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            if (Application.isPlaying)
            {
                EnsureSetup();
            }
        }
#endif

        public bool EnsureSetup()
        {
            ResolveReferences();
            if (targetCamera == null || !IsAvailable)
            {
                setupStatus = targetCamera == null ? "Target Camera が未設定です。" : "Builtin Post Processing Stack v2 (`com.unity.postprocessing`) が project に入っていないため、自動設定できません。";
                return false;
            }

            var layerType = GetPostProcessLayerType();
            var volumeType = GetPostProcessVolumeType();
            var profileType = GetPostProcessProfileType();
            var depthOfFieldType = GetDepthOfFieldType();
            if (layerType == null || volumeType == null || profileType == null || depthOfFieldType == null)
            {
                return false;
            }

            postProcessLayer = ResolveComponent(targetCamera.gameObject, layerType, postProcessLayer);
            postProcessVolume = ResolveComponent(gameObject, volumeType, postProcessVolume);
            if (postProcessLayer == null || postProcessVolume == null)
            {
                setupStatus = "既存の PostProcessLayer / PostProcessVolume が見つからないため、自動設定をスキップしました。";
                return false;
            }

            profile = profile != null && profileType.IsInstanceOfType(profile) ? profile : ScriptableObject.CreateInstance(profileType);
            depthOfField = depthOfField != null && depthOfFieldType.IsInstanceOfType(depthOfField) ? depthOfField : ScriptableObject.CreateInstance(depthOfFieldType);
            EnsureDepthOfFieldInProfile(profile, depthOfField);
            ConfigureVolume(postProcessVolume, profile, volumePriority);
            ConfigureLayer(postProcessLayer, gameObject.layer);
            ConfigureDepthOfFieldDefaults(depthOfField, enableDepthOfField, aperture, focalLength, kernelSize);
            setupStatus = $"Configured: Priority={volumePriority}, Aperture={aperture}, FocalLength={focalLength}, KernelSize={kernelSize}";
            return true;
        }

        private void ResolveReferences()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }
        }

        private static MonoBehaviour ResolveComponent(GameObject targetObject, Type componentType, MonoBehaviour currentComponent)
        {
            if (currentComponent != null && componentType.IsInstanceOfType(currentComponent))
            {
                return currentComponent;
            }

            return targetObject.GetComponent(componentType) as MonoBehaviour;
        }

        private static void EnsureDepthOfFieldInProfile(ScriptableObject targetProfile, ScriptableObject targetDepthOfField)
        {
            var settingsField = targetProfile.GetType().GetField("settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (settingsField == null)
            {
                return;
            }

            var settingsList = settingsField.GetValue(targetProfile) as System.Collections.IList;
            if (settingsList == null)
            {
                return;
            }

            for (var i = 0; i < settingsList.Count; i++)
            {
                if (ReferenceEquals(settingsList[i], targetDepthOfField))
                {
                    return;
                }
            }

            settingsList.Add(targetDepthOfField);
        }

        private static void ConfigureVolume(MonoBehaviour volumeComponent, ScriptableObject targetProfile, float priority)
        {
            if (volumeComponent == null)
            {
                return;
            }

            SetMemberValue(volumeComponent, "isGlobal", true);
            SetMemberValue(volumeComponent, "priority", priority);
            if (!SetMemberValue(volumeComponent, "sharedProfile", targetProfile))
            {
                SetMemberValue(volumeComponent, "profile", targetProfile);
            }
        }

        private static void ConfigureLayer(MonoBehaviour layerComponent, int layerIndex)
        {
            if (layerComponent == null)
            {
                return;
            }

            SetMemberValue(layerComponent, "volumeLayer", 1 << layerIndex);
            SetMemberValue(layerComponent, "volumeTrigger", layerComponent.transform);
            SetMemberValue(layerComponent, "enabled", true);
        }

        private static void ConfigureDepthOfFieldDefaults(ScriptableObject targetDepthOfField, bool isEnabled, float targetAperture, float targetFocalLength, int targetKernelSize)
        {
            if (targetDepthOfField == null)
            {
                return;
            }

            SetParameterValue(targetDepthOfField, "enabled", isEnabled);
            SetParameterValue(targetDepthOfField, "aperture", targetAperture);
            SetParameterValue(targetDepthOfField, "focalLength", targetFocalLength);
            SetParameterValue(targetDepthOfField, "kernelSize", targetKernelSize);
        }

        private static bool SetMemberValue(object target, string memberName, object value)
        {
            var type = target.GetType();
            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite && value != null && property.PropertyType.IsAssignableFrom(value.GetType()))
            {
                property.SetValue(target, value, null);
                return true;
            }

            if (property != null && property.CanWrite && property.PropertyType == typeof(int) && value is int intValue)
            {
                property.SetValue(target, intValue, null);
                return true;
            }

            if (property != null && property.CanWrite && property.PropertyType == typeof(float) && value is float floatValue)
            {
                property.SetValue(target, floatValue, null);
                return true;
            }

            if (property != null && property.CanWrite && property.PropertyType == typeof(bool) && value is bool boolValue)
            {
                property.SetValue(target, boolValue, null);
                return true;
            }

            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && value != null && field.FieldType.IsAssignableFrom(value.GetType()))
            {
                field.SetValue(target, value);
                return true;
            }

            if (field != null && field.FieldType == typeof(int) && value is int fieldIntValue)
            {
                field.SetValue(target, fieldIntValue);
                return true;
            }

            if (field != null && field.FieldType == typeof(float) && value is float fieldFloatValue)
            {
                field.SetValue(target, fieldFloatValue);
                return true;
            }

            if (field != null && field.FieldType == typeof(bool) && value is bool fieldBoolValue)
            {
                field.SetValue(target, fieldBoolValue);
                return true;
            }

            return false;
        }

        private static void SetParameterValue(object target, string memberName, object value)
        {
            var type = target.GetType();
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return;
            }

            var parameter = field.GetValue(target);
            if (parameter == null)
            {
                return;
            }

            var parameterType = parameter.GetType();
            var valueProperty = parameterType.GetProperty("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (valueProperty != null && valueProperty.CanWrite)
            {
                if (valueProperty.PropertyType == typeof(bool) && value is bool boolValue)
                {
                    valueProperty.SetValue(parameter, boolValue, null);
                }
                else if (valueProperty.PropertyType == typeof(float) && value is float floatValue)
                {
                    valueProperty.SetValue(parameter, floatValue, null);
                }
                else if (valueProperty.PropertyType.IsEnum && value is int enumIntValue)
                {
                    valueProperty.SetValue(parameter, Enum.ToObject(valueProperty.PropertyType, enumIntValue), null);
                }
            }

            var overrideStateProperty = parameterType.GetProperty("overrideState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (overrideStateProperty != null && overrideStateProperty.CanWrite && overrideStateProperty.PropertyType == typeof(bool))
            {
                overrideStateProperty.SetValue(parameter, true, null);
            }
        }

        private static Type GetPostProcessLayerType()
        {
            return Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessLayer, Unity.Postprocessing.Runtime");
        }

        private static Type GetPostProcessVolumeType()
        {
            return Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessVolume, Unity.Postprocessing.Runtime");
        }

        private static Type GetPostProcessProfileType()
        {
            return Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessProfile, Unity.Postprocessing.Runtime");
        }

        private static Type GetDepthOfFieldType()
        {
            return Type.GetType("UnityEngine.Rendering.PostProcessing.DepthOfField, Unity.Postprocessing.Runtime");
        }
    }
}
