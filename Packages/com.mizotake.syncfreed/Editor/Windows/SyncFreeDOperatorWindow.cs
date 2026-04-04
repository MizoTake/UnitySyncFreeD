using MizoTake.SyncFreeD.Editor.Support;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;
using UnityEngine;

namespace MizoTake.SyncFreeD.Editor.Windows
{
    public sealed class SyncFreeDOperatorWindow : EditorWindow
    {
        private SyncFreeDBehaviour targetBehaviour;
        private Vector2 scrollPosition;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle badgeStyle;
        private float moveStepMeters = 0.1f;
        private float rotateStepDegrees = 5f;
        private float rollStepDegrees = 5f;
        private float focalLengthStepMm = 5f;
        private float focusDistanceStepMeters = 0.5f;

        [MenuItem("Tools/SyncFreeD/Operator Window")]
        public static void OpenWindow()
        {
            GetWindow<SyncFreeDOperatorWindow>("SyncFreeD Operator");
        }

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject != null)
            {
                targetBehaviour = Selection.activeGameObject.GetComponent<SyncFreeDBehaviour>();
            }

            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeroPanel("SyncFreeD Operator", "色で状態が分かる非エンジニア向けの確認画面です。sample を開いて、現在の rig 状態と次の作業を確認できます。", SyncFreeDStatusTone.Info);
            DrawSampleGuide();
            EditorGUILayout.Space();
            DrawRigStatus();
        }

        private void DrawSampleGuide()
        {
            EditorGUILayout.LabelField("おすすめ sample", EditorStyles.boldLabel);
            DrawSampleButton(SyncFreeDSampleId.BasicVirtualCamera, "Basic");
            DrawSampleButton(SyncFreeDSampleId.OutputInspector, "Inspector");
            DrawSampleButton(SyncFreeDSampleId.FreeDController, "Controller");
        }

        private void DrawSampleButton(SyncFreeDSampleId sampleId, string shortLabel)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawBadge("Sample", SyncFreeDStatusTone.Info);
                    EditorGUILayout.LabelField(shortLabel, EditorStyles.boldLabel, GUILayout.Width(90f));
                    EditorGUILayout.LabelField(SyncFreeDSupportSummary.GetSampleDescription(sampleId), bodyStyle);
                }

                if (GUILayout.Button($"Open {shortLabel}"))
                {
                    SyncFreeDSupportSummary.TryOpenSampleScene(sampleId);
                }
            }
        }

        private void DrawRigStatus()
        {
            EditorGUILayout.LabelField("選択中 rig の状態", EditorStyles.boldLabel);
            targetBehaviour = (SyncFreeDBehaviour)EditorGUILayout.ObjectField("SyncFreeD Behaviour", targetBehaviour, typeof(SyncFreeDBehaviour), true);
            if (targetBehaviour == null && GUILayout.Button("Scene 内から自動で探す"))
            {
                targetBehaviour = FindObjectOfType<SyncFreeDBehaviour>();
            }

            var snapshot = SyncFreeDSupportSummary.Build(targetBehaviour);
            DrawHeroPanel(snapshot.StatusTitle, snapshot.GuidanceMessage, snapshot.Tone);
            DrawRecommendedSamplePanel();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawOperationPanel();
            DrawStatusRow(snapshot.HasSource, "入力元 Behaviour がある");
            DrawStatusRow(snapshot.HasOutput, "FreeD 出力 Behaviour がある");
            DrawStatusRow(snapshot.HasOutputPreset, "送信設定 Asset がある");
            DrawStatusRow(snapshot.HasControllerPreset, "controller 操作 preset がある、または不要");
            DrawStatusRow(snapshot.HasLoopbackReceiver, "loopback receiver がある");
            DrawStatusRow(!snapshot.HasConfigurationWarning, "送信設定に warning がない");
            DrawStatusRow(!snapshot.HasLoopbackReceiver || snapshot.IsLoopbackReceiving, "loopback 受信確認済み、または未使用");
            if (snapshot.HasConfigurationWarning)
            {
                DrawHeroPanel("Warning", snapshot.ConfigurationWarning, SyncFreeDStatusTone.Warning);
            }

            DrawQuickActions();
            EditorGUILayout.EndScrollView();
        }

        private void DrawRecommendedSamplePanel()
        {
            var recommendedSample = SyncFreeDSupportSummary.GetRecommendedSampleId(targetBehaviour);
            using (new ColorScope(SyncFreeDSupportSummary.GetBackgroundColor(SyncFreeDStatusTone.Ready)))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawBadge("Recommended Sample", SyncFreeDStatusTone.Ready);
                    EditorGUILayout.LabelField(recommendedSample.ToString(), EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(SyncFreeDSupportSummary.GetRecommendedSampleReason(targetBehaviour), bodyStyle);
                    if (GUILayout.Button("おすすめ sample を開く"))
                    {
                        SyncFreeDSupportSummary.TryOpenSampleScene(recommendedSample);
                    }
                }
            }
        }

        private void DrawOperationPanel()
        {
            if (targetBehaviour == null)
            {
                return;
            }

            var controller = targetBehaviour.GetComponent<FreeDControllerBehaviour>();
            using (new ColorScope(SyncFreeDSupportSummary.GetBackgroundColor(controller != null ? SyncFreeDStatusTone.Ready : SyncFreeDStatusTone.ActionNeeded)))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawBadge("Camera Operation", controller != null ? SyncFreeDStatusTone.Ready : SyncFreeDStatusTone.ActionNeeded);
                    EditorGUILayout.LabelField(controller != null ? "ボタン操作で FreeD カメラを直接動かせます。" : "controller を追加すると、この画面から FreeD カメラを直接操作できます。", bodyStyle);
                    if (controller == null)
                    {
                        if (GUILayout.Button("controller を追加して操作可能にする"))
                        {
                            controller = SyncFreeDSupportSummary.EnsureController(targetBehaviour);
                            if (controller != null)
                            {
                                Selection.activeObject = controller;
                                EditorGUIUtility.PingObject(controller);
                            }
                        }

                        return;
                    }

                    moveStepMeters = Mathf.Max(0.01f, EditorGUILayout.FloatField("移動量 (m)", moveStepMeters));
                    rotateStepDegrees = Mathf.Max(0.1f, EditorGUILayout.FloatField("パン/チルト量 (deg)", rotateStepDegrees));
                    rollStepDegrees = Mathf.Max(0.1f, EditorGUILayout.FloatField("ロール量 (deg)", rollStepDegrees));
                    focalLengthStepMm = Mathf.Max(0.1f, EditorGUILayout.FloatField("ズーム量 (mm)", focalLengthStepMm));
                    focusDistanceStepMeters = Mathf.Max(0.01f, EditorGUILayout.FloatField("フォーカス量 (m)", focusDistanceStepMeters));
                    DrawTranslationControls();
                    DrawRotationControls();
                    DrawLensControls();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("いまの状態を FreeD に反映"))
                        {
                            SyncFreeDOperatorActions.TryManualRefresh(targetBehaviour);
                        }

                        if (GUILayout.Button("位置とレンズを初期値に戻す"))
                        {
                            SyncFreeDOperatorActions.TryReset(targetBehaviour);
                        }
                    }
                }
            }
        }

        private void DrawTranslationControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("位置", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("左"))
                {
                    SyncFreeDOperatorActions.TryTranslate(targetBehaviour, Vector3.left * moveStepMeters);
                }

                if (GUILayout.Button("右"))
                {
                    SyncFreeDOperatorActions.TryTranslate(targetBehaviour, Vector3.right * moveStepMeters);
                }

                if (GUILayout.Button("前"))
                {
                    SyncFreeDOperatorActions.TryTranslate(targetBehaviour, Vector3.forward * moveStepMeters);
                }

                if (GUILayout.Button("後"))
                {
                    SyncFreeDOperatorActions.TryTranslate(targetBehaviour, Vector3.back * moveStepMeters);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("上"))
                {
                    SyncFreeDOperatorActions.TryTranslate(targetBehaviour, Vector3.up * moveStepMeters);
                }

                if (GUILayout.Button("下"))
                {
                    SyncFreeDOperatorActions.TryTranslate(targetBehaviour, Vector3.down * moveStepMeters);
                }
            }
        }

        private void DrawRotationControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("向き", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("パン 左"))
                {
                    SyncFreeDOperatorActions.TryRotate(targetBehaviour, new Vector3(0f, -rotateStepDegrees, 0f));
                }

                if (GUILayout.Button("パン 右"))
                {
                    SyncFreeDOperatorActions.TryRotate(targetBehaviour, new Vector3(0f, rotateStepDegrees, 0f));
                }

                if (GUILayout.Button("チルト 上"))
                {
                    SyncFreeDOperatorActions.TryRotate(targetBehaviour, new Vector3(-rotateStepDegrees, 0f, 0f));
                }

                if (GUILayout.Button("チルト 下"))
                {
                    SyncFreeDOperatorActions.TryRotate(targetBehaviour, new Vector3(rotateStepDegrees, 0f, 0f));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("ロール 左"))
                {
                    SyncFreeDOperatorActions.TryRotate(targetBehaviour, new Vector3(0f, 0f, -rollStepDegrees));
                }

                if (GUILayout.Button("ロール 右"))
                {
                    SyncFreeDOperatorActions.TryRotate(targetBehaviour, new Vector3(0f, 0f, rollStepDegrees));
                }
            }
        }

        private void DrawLensControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("レンズ", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("ズーム +"))
                {
                    SyncFreeDOperatorActions.TryAdjustLens(targetBehaviour, focalLengthStepMm, 0f);
                }

                if (GUILayout.Button("ズーム -"))
                {
                    SyncFreeDOperatorActions.TryAdjustLens(targetBehaviour, -focalLengthStepMm, 0f);
                }

                if (GUILayout.Button("フォーカス +"))
                {
                    SyncFreeDOperatorActions.TryAdjustLens(targetBehaviour, 0f, focusDistanceStepMeters);
                }

                if (GUILayout.Button("フォーカス -"))
                {
                    SyncFreeDOperatorActions.TryAdjustLens(targetBehaviour, 0f, -focusDistanceStepMeters);
                }
            }
        }

        private void DrawQuickActions()
        {
            if (targetBehaviour == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("すぐ使う操作", EditorStyles.boldLabel);
            if (GUILayout.Button("詳しい Debug Window を開く"))
            {
                SyncFreeDDebugWindow.OpenWindow();
            }

            if (GUILayout.Button("Setup Wizard を開く"))
            {
                Setup.SyncFreeDSetupWizard.OpenWindow();
            }

            var output = targetBehaviour.GetComponent<FreeDUdpOutputBehaviour>();
            if (output != null && GUILayout.Button("sample の送信 preset を適用する"))
            {
                if (SyncFreeDSupportSummary.ApplySharedOutputPreset(output) && output.OutputProfileAsset != null)
                {
                    Selection.activeObject = output.OutputProfileAsset;
                    EditorGUIUtility.PingObject(output.OutputProfileAsset);
                }
            }

            if (output != null && output.OutputProfileAsset == null && GUILayout.Button("今の送信設定から preset Asset を作る"))
            {
                var createdAsset = SyncFreeDSupportSummary.CreateOutputPresetFromBehaviour(output, $"{targetBehaviour.gameObject.name}_OutputPreset");
                if (createdAsset != null)
                {
                    Selection.activeObject = createdAsset;
                    EditorGUIUtility.PingObject(createdAsset);
                }
            }

            if (output != null && output.OutputProfileAsset != null && GUILayout.Button("送信設定 Asset を選択する"))
            {
                Selection.activeObject = output.OutputProfileAsset;
                EditorGUIUtility.PingObject(output.OutputProfileAsset);
            }

            var controller = targetBehaviour.GetComponent<FreeDControllerBehaviour>();
            if (controller == null && GUILayout.Button("controller を追加して操作可能にする"))
            {
                controller = SyncFreeDSupportSummary.EnsureController(targetBehaviour);
                if (controller != null)
                {
                    Selection.activeObject = controller;
                    EditorGUIUtility.PingObject(controller);
                }
            }

            if (controller != null && GUILayout.Button("sample の操作 preset を適用する"))
            {
                if (SyncFreeDSupportSummary.ApplySharedControllerPreset(controller) && controller.ProfileAsset != null)
                {
                    Selection.activeObject = controller.ProfileAsset;
                    EditorGUIUtility.PingObject(controller.ProfileAsset);
                }
            }

            if (controller != null && controller.ProfileAsset == null && GUILayout.Button("今の操作設定から controller preset を作る"))
            {
                var createdAsset = SyncFreeDSupportSummary.CreateControllerPresetFromBehaviour(controller, $"{targetBehaviour.gameObject.name}_ControllerPreset");
                if (createdAsset != null)
                {
                    Selection.activeObject = createdAsset;
                    EditorGUIUtility.PingObject(createdAsset);
                }
            }

            if (controller != null && controller.ProfileAsset != null && GUILayout.Button("controller preset Asset を選択する"))
            {
                Selection.activeObject = controller.ProfileAsset;
                EditorGUIUtility.PingObject(controller.ProfileAsset);
            }
        }

        private static void DrawStatusRow(bool isOk, string label)
        {
            var tone = isOk ? SyncFreeDStatusTone.Ready : SyncFreeDStatusTone.ActionNeeded;
            var background = SyncFreeDSupportSummary.GetBackgroundColor(tone);
            var accent = SyncFreeDSupportSummary.GetAccentColor(tone);
            using (new ColorScope(background))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var rect = GUILayoutUtility.GetRect(6f, 28f, GUILayout.Width(6f), GUILayout.Height(28f));
                    EditorGUI.DrawRect(rect, accent);
                    GUILayout.Space(4f);
                    EditorGUILayout.LabelField(isOk ? "OK" : "TODO", GUILayout.Width(48f));
                    EditorGUILayout.LabelField(label);
                }
            }
        }

        private void DrawHeroPanel(string title, string body, SyncFreeDStatusTone tone)
        {
            using (new ColorScope(SyncFreeDSupportSummary.GetBackgroundColor(tone)))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawBadge(title, tone);
                    EditorGUILayout.LabelField(body, bodyStyle);
                }
            }
        }

        private void DrawBadge(string text, SyncFreeDStatusTone tone)
        {
            var previousColor = GUI.color;
            GUI.color = SyncFreeDSupportSummary.GetAccentColor(tone);
            GUILayout.Label(text, badgeStyle, GUILayout.Height(22f));
            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = false };
            }

            if (badgeStyle == null)
            {
                badgeStyle = new GUIStyle(EditorStyles.miniButtonMid);
                badgeStyle.alignment = TextAnchor.MiddleCenter;
                badgeStyle.fontStyle = FontStyle.Bold;
                badgeStyle.normal.textColor = Color.white;
            }
        }

        private readonly struct ColorScope : System.IDisposable
        {
            private readonly Color previousColor;

            public ColorScope(Color color)
            {
                previousColor = GUI.backgroundColor;
                GUI.backgroundColor = color;
            }

            public void Dispose()
            {
                GUI.backgroundColor = previousColor;
            }
        }
    }
}
