using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    internal static class DepthOfFieldFocusApplier
    {
        public static bool TryApply(Component targetComponent, float focusDistanceMeters)
        {
            return targetComponent != null && TryApplyToObject(targetComponent, focusDistanceMeters);
        }

        public static bool TryApply(GameObject targetObject, float focusDistanceMeters)
        {
            if (targetObject == null)
            {
                return false;
            }

            var components = targetObject.GetComponents<Component>();
            for (var i = 0; i < components.Length; i++)
            {
                if (TryApplyToObject(components[i], focusDistanceMeters))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryApplyToObject(object target, float focusDistanceMeters)
        {
            if (target == null)
            {
                return false;
            }

            if (TryApplyDirectFocusDistance(target, focusDistanceMeters))
            {
                return true;
            }

            if (TryApplyViaNamedMember(target, "profile", focusDistanceMeters))
            {
                return true;
            }

            if (TryApplyViaNamedMember(target, "sharedProfile", focusDistanceMeters))
            {
                return true;
            }

            if (TryApplyViaNamedMember(target, "settings", focusDistanceMeters))
            {
                return true;
            }

            if (TryApplyViaNamedMember(target, "components", focusDistanceMeters))
            {
                return true;
            }

            return false;
        }

        private static bool TryApplyDirectFocusDistance(object target, float focusDistanceMeters)
        {
            var type = target.GetType();
            if (!type.Name.Contains("DepthOfField", StringComparison.OrdinalIgnoreCase) && type.GetMember("focusDistance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length == 0)
            {
                return false;
            }

            var property = type.GetProperty("focusDistance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                var currentValue = property.CanRead ? property.GetValue(target, null) : null;
                if (TryAssignFocusDistance(property.PropertyType, currentValue, assignedValue => property.SetValue(target, assignedValue, null), focusDistanceMeters))
                {
                    return true;
                }
            }

            var field = type.GetField("focusDistance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                var currentValue = field.GetValue(target);
                if (TryAssignFocusDistance(field.FieldType, currentValue, assignedValue => field.SetValue(target, assignedValue), focusDistanceMeters))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryApplyViaNamedMember(object target, string memberName, float focusDistanceMeters)
        {
            var type = target.GetType();
            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanRead && TryApplyToNestedValue(property.GetValue(target, null), focusDistanceMeters))
            {
                return true;
            }

            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null && TryApplyToNestedValue(field.GetValue(target), focusDistanceMeters);
        }

        private static bool TryApplyToNestedValue(object nestedValue, float focusDistanceMeters)
        {
            if (nestedValue == null)
            {
                return false;
            }

            if (TryApplyToObject(nestedValue, focusDistanceMeters))
            {
                return true;
            }

            if (nestedValue is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (TryApplyToObject(item, focusDistanceMeters))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryAssignFocusDistance(Type valueType, object currentValue, Action<object> assignValue, float focusDistanceMeters)
        {
            if (valueType == typeof(float))
            {
                assignValue(focusDistanceMeters);
                return true;
            }

            if (valueType == typeof(double))
            {
                assignValue((double)focusDistanceMeters);
                return true;
            }

            if (currentValue == null)
            {
                try
                {
                    currentValue = Activator.CreateInstance(valueType);
                }
                catch
                {
                    return false;
                }
            }

            if (!TryAssignParameterValue(currentValue, focusDistanceMeters))
            {
                return false;
            }

            assignValue(currentValue);
            return true;
        }

        private static bool TryAssignParameterValue(object parameterObject, float focusDistanceMeters)
        {
            var parameterType = parameterObject.GetType();
            var assigned = false;
            var valueProperty = parameterType.GetProperty("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (valueProperty != null && valueProperty.CanWrite)
            {
                if (valueProperty.PropertyType == typeof(float))
                {
                    valueProperty.SetValue(parameterObject, focusDistanceMeters, null);
                    assigned = true;
                }
                else if (valueProperty.PropertyType == typeof(double))
                {
                    valueProperty.SetValue(parameterObject, (double)focusDistanceMeters, null);
                    assigned = true;
                }
            }

            var valueField = parameterType.GetField("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (!assigned && valueField != null)
            {
                if (valueField.FieldType == typeof(float))
                {
                    valueField.SetValue(parameterObject, focusDistanceMeters);
                    assigned = true;
                }
                else if (valueField.FieldType == typeof(double))
                {
                    valueField.SetValue(parameterObject, (double)focusDistanceMeters);
                    assigned = true;
                }
            }

            var overrideStateProperty = parameterType.GetProperty("overrideState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (overrideStateProperty != null && overrideStateProperty.CanWrite && overrideStateProperty.PropertyType == typeof(bool))
            {
                overrideStateProperty.SetValue(parameterObject, true, null);
            }

            var overrideStateField = parameterType.GetField("overrideState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (overrideStateField != null && overrideStateField.FieldType == typeof(bool))
            {
                overrideStateField.SetValue(parameterObject, true);
            }

            var activeProperty = parameterType.GetProperty("active", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (activeProperty != null && activeProperty.CanWrite && activeProperty.PropertyType == typeof(bool))
            {
                activeProperty.SetValue(parameterObject, true, null);
            }

            var activeField = parameterType.GetField("active", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (activeField != null && activeField.FieldType == typeof(bool))
            {
                activeField.SetValue(parameterObject, true);
            }

            return assigned;
        }
    }
}
