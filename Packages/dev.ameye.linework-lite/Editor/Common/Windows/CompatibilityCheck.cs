using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace LineworkLite.Editor.Common.Windows
{
    public class CompatibilityCheck : EditorWindow
    {
        private List<Check> checks;

        private enum CheckStatus
        {
            Untested,
            InProgress,
            Completed
        }

        private enum ResultEnum
        {
            Untested,
            Pass,
            Fail,
            Warning,
            Info
        }

        private class CheckResult
        {
            public ResultEnum Result { get; }
            public string Message { get; }

            public string Description { get; }

            public CheckResult(ResultEnum result, string message, string description)
            {
                Result = result;
                Message = message;
                Description = description;
            }

            public override string ToString()
            {
                return Message;
            }
        }

        private static SearchRequest _urpSearchRequest;
        private static AddRequest _addRequest;

        private const string NoActiveRendererFoundDescription = "Not found. See Edit > Project Settings > Graphics if there is a Default Render Pipeline assigned.";

        [MenuItem("Window/Linework Lite/Compatibility")]
        public static void ShowWindow()
        {
            var window = GetWindow<CompatibilityCheck>();
            window.titleContent = new GUIContent("Compatibility Check", EditorGUIUtility.IconContent("Settings").image);
            window.minSize = new Vector2(400, 500);
            window.maxSize = new Vector2(400, 500);
            window.Show();
        }

        private void CreateGUI()
        {
            checks = new List<Check>
            {
                new("Unity Version", CheckUnityVersion),
                new("URP Version", CheckURPVersion),
                new("Pipeline", CheckActivePipeline),
                new("Renderer", CheckActiveRenderer),
                new("Render Graph", CheckRenderGraph),
                new("Rendering Path", CheckRenderingPath),
                new("DOTS Hybrid Renderer", CheckHybridRenderer),
                new("Graphics API", CheckGraphicsAPI),
                new("Platform", CheckTargetPlatform),
                new("Active Pipeline Asset", CheckActiveRenderPipelineAsset),
                new("STP", CheckSTP),
                new("Version", CheckLiteVersion)
            };

            var container = new VisualElement
            {
                style =
                {
                    flexGrow = 1
                }
            };

            var detailsHeader = new VisualElement
            {
                style =
                {
                    paddingTop = 10,
                    paddingLeft = 5
                }
            };

            // Title.
            var titleContainer = new VisualElement
            {
                style =
                {
                    marginLeft = 4,
                    marginRight = 4,
                    paddingLeft = 2
                }
            };
            var titleHeader = new Label("Linework Lite")
            {
                style =
                {
                    fontSize = 19,
                    minWidth = 100,
                    marginTop = 0,
                    paddingBottom = 2,
                    paddingLeft = 0,
                    paddingRight = 2,
                    paddingTop = 1,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            titleContainer.Add(titleHeader);
            detailsHeader.Add(titleContainer);

            // Version.
            var versionContainer = new VisualElement
            {
                style =
                {
                    marginLeft = 2,
                    marginTop = 4,
                    paddingLeft = 2
                }
            };
            var versionLabel = new Label("1.1.0 • September 2025")
            {
                style =
                {
                    fontSize = 12,
                    height = 18,
                    paddingBottom = 2,
                    paddingLeft = 2,
                    paddingRight = 2,
                    paddingTop = 1,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            versionContainer.Add(versionLabel);
            detailsHeader.Add(versionContainer);

            // Author.
            var authorLabel = new Label("By Alexander Ameye")
            {
                style =
                {
                    fontSize = 12,
                    marginBottom = 2,
                    marginLeft = 4,
                    marginRight = 4,
                    marginTop = 2,
                    paddingBottom = 2,
                    paddingLeft = 2,
                    paddingRight = 2,
                    paddingTop = 1
                }
            };
            detailsHeader.Add(authorLabel);

            // Links.
            var linksContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingBottom = 5,
                    alignItems = Align.FlexStart
                }
            };
            var linksContainerHorizontal = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                }
            };
            var separator1 = new Label("|")
            {
                style =
                {
                    height = 20,
                    marginLeft = 4,
                    fontSize = 15,
                    color = new Color(0.1686275f, 0.1607843f, 0.1686275f),
                }
            };
            var separator2 = new Label("|")
            {
                style =
                {
                    height = 20,
                    marginLeft = 4,
                    fontSize = 15,
                    color = new Color(0.1686275f, 0.1607843f, 0.1686275f),
                }
            };
            var separator3 = new Label("|")
            {
                style =
                {
                    height = 20,
                    marginLeft = 4,
                    fontSize = 15,
                    color = new Color(0.1686275f, 0.1607843f, 0.1686275f),
                }
            };
            var fullVersionLink = new Label
            {
                text = "Full Version",
                style =
                {
                    color = new StyleColor(new Color(0.2980392f, 0.4941176f, 1.0f, 1.0f)),
                    marginLeft = 6,
                    paddingLeft = 0,
                    paddingRight = 0
                }
            };
            var documentationLink = new Label
            {
                text = "Documentation",
                style =
                {
                    color = new StyleColor(new Color(0.2980392f, 0.4941176f, 1.0f, 1.0f)),
                    marginLeft = 6,
                    paddingLeft = 0,
                    paddingRight = 0
                }
            };
            var supportLink = new Label
            {
                text = "Support",
                style =
                {
                    color = new StyleColor(new Color(0.2980392f, 0.4941176f, 1.0f, 1.0f)),
                    marginLeft = 6,
                    paddingLeft = 0,
                    paddingRight = 0
                }
            };
            var reviewLink = new Label
            {
                text = "Review",
                style =
                {
                    color = new StyleColor(new Color(0.2980392f, 0.4941176f, 1.0f, 1.0f)),
                    marginLeft = 6,
                    paddingLeft = 0,
                    paddingRight = 0
                }
            };

            fullVersionLink.AddManipulator(new Clickable(() => Application.OpenURL("https://assetstore.unity.com/packages/vfx/shaders/linework-outlines-and-edge-detection-294140")));
            documentationLink.AddManipulator(new Clickable(() => Application.OpenURL("https://linework.ameye.dev")));
            supportLink.AddManipulator(new Clickable(() => Application.OpenURL("https://discord.gg/cFfQGzQdPn")));
            reviewLink.AddManipulator(new Clickable(() => Application.OpenURL("https://assetstore.unity.com/packages/vfx/shaders/free-outline-326925#reviews")));
            linksContainerHorizontal.Add(fullVersionLink);
            linksContainerHorizontal.Add(separator1);
            linksContainerHorizontal.Add(documentationLink);
            linksContainerHorizontal.Add(separator2);
            linksContainerHorizontal.Add(supportLink);
            linksContainerHorizontal.Add(separator3);
            linksContainerHorizontal.Add(reviewLink);
            linksContainer.Add(linksContainerHorizontal);
            detailsHeader.Add(linksContainer);

            // List view.
            var compatibilityContainer = new VisualElement
            {
                style =
                {
                    backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.1686275f, 0.1607843f, 0.1686275f) : new Color(0.8352941f, 0.8352941f, 0.8352941f),
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1,
                }
            };
            var child = new VisualElement();

            var descriptionLabel = new Label
            {
                text = "Select a check to see its description.",
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Italic,
                    marginTop = 10,
                    textOverflow = TextOverflow.Ellipsis,
                    whiteSpace = WhiteSpace.Normal,
                    paddingLeft = 5
                }
            };

            var listView = new ListView(checks, 20, MakeItem, BindItem)
            {
                style =
                {
                    flexGrow = 1.0f
                },
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                selectionType = SelectionType.Single
            };

            listView.selectionChanged += objects =>
            {
                if (objects.ToArray()[0] is Check selectedCheck)
                {
                    descriptionLabel.text = selectedCheck.Result.Description;
                }
            };

            var buttons = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    justifyContent = Justify.Center,
                    marginTop = 10
                }
            };
            var detectButton = new Button(() =>
            {
                foreach (var checkItem in checks)
                {
                    RunCheck(checkItem, listView);
                }
            })
            {
                text = "Check Compatibility",
                style =
                {

                    marginTop = 20,
                    marginLeft = 0,
                    marginRight = 0,
                    flexGrow = 1
                }

            };
            buttons.Add(detectButton);

            var copyInfoButton = new Button(() => { CopyCheckInfoToClipboard(); })
            {
                text = "Copy Result",
                style =
                {
                    marginLeft = 0,
                    marginRight = 0,
                    flexGrow = 1
                }
            };
            buttons.Add(copyInfoButton);

            container.Add(detailsHeader);

            child.Add(listView);
            child.Add(descriptionLabel);
            child.Add(buttons);
            compatibilityContainer.Add(child);
            container.Add(compatibilityContainer);

            rootVisualElement.Add(container);
            return;

            void BindItem(VisualElement e, int i)
            {
                var labels = e.Query<Label>().ToList();
                var image = e.Q<Image>();

                labels[0].text = checks[i].Name;
                labels[1].text = checks[i].Status switch
                {
                    CheckStatus.Untested => "Not tested",
                    CheckStatus.InProgress => "Testing...",
                    CheckStatus.Completed when checks[i].Result != null => checks[i].Result.Message,
                    _ => labels[1].text
                };

                image.image = checks[i].Status == CheckStatus.Completed && checks[i].Result != null
                    ? LoadIconForResult(checks[i].Result.Result).image
                    : LoadIconForResult(ResultEnum.Untested).image;
            }

            VisualElement MakeItem()
            {
                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        paddingLeft = 4,
                        paddingRight = 4
                    }
                };

                var checkLabel = new Label
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        flexGrow = 1,
                        minWidth = 140
                    }
                };


                var statusLabel = new Label
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        flexGrow = 1,
                        minWidth = 110
                    }
                };


                var resultIcon = new Image()
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleRight,
                        alignSelf = Align.Center,
                        justifyContent = Justify.FlexEnd,
                        minWidth = 20
                    }
                };

                row.Add(checkLabel);
                row.Add(statusLabel);
                row.Add(resultIcon);
                row.AddToClassList("list-view-row");

                return row;
            }
        }

        private void RunCheck(Check check, ListView listView)
        {
            check.Status = CheckStatus.InProgress;
            listView.RefreshItems();

            EditorApplication.delayCall += () =>
            {
                var result = check.RunCheck();
                check.Status = CheckStatus.Completed;
                check.Result = result;
                listView.RefreshItems();
            };
        }

        private void CopyCheckInfoToClipboard()
        {
            var stringBuilder = new StringBuilder();

            foreach (var check in checks)
            {
                if (check.Status == CheckStatus.Completed && check.Result != null)
                {
                    switch (check.Result.Result)
                    {
                        case ResultEnum.Pass:
                            stringBuilder.AppendLine($"PASS\t{check.Name}: {check.Result.Message}");
                            break;
                        case ResultEnum.Warning:
                            stringBuilder.AppendLine($"WARN\t{check.Name}: {check.Result.Message}");
                            break;
                        case ResultEnum.Fail:
                            stringBuilder.AppendLine($"FAIL\t{check.Name}: {check.Result.Message}");
                            break;
                    }
                    stringBuilder.AppendLine($"\t\t{check.Result.Description}");
                }
                else
                {
                    stringBuilder.AppendLine($"UNKNOWN\t{check.Name}");
                    stringBuilder.AppendLine("\t\tPlease run the compatibility check first.");
                }
                stringBuilder.AppendLine();
            }

            EditorGUIUtility.systemCopyBuffer = stringBuilder.ToString();
            Debug.Log("Compatibility info copied to clipboard!");
        }

        private CheckResult CheckUnityVersion()
        {
            var unityVersion = Application.unityVersion;
            var alpha = unityVersion.Contains("a");
            var beta = unityVersion.Contains("b");
            if (unityVersion.StartsWith("6000"))
            {
                if (alpha || beta)
                {
                    return new CheckResult(ResultEnum.Info, unityVersion, "Linework Lite is compatible with Unity 6+. However, alpha/beta versions of Unity may be unstable.");
                }
                return new CheckResult(ResultEnum.Pass, unityVersion, "Linework Lite is compatible with Unity 6+.");
            }
            if (unityVersion.StartsWith("2022.3"))
            {
                if (alpha || beta)
                {
                    return new CheckResult(ResultEnum.Info, unityVersion, "Linework Lite is compatible with Unity 2022.3. However, alpha/beta versions of Unity may not be stable.");
                }
                return new CheckResult(ResultEnum.Info, unityVersion,
                    "Linework Lite is compatible with Unity 2022.3. However, Unity no longer develops or improves the rendering path that does not use Render Graph API. Upgrade to Unity 6 to make use of the Render Graph API.");
            }
            if (unityVersion.StartsWith("2023"))
            {
                return new CheckResult(ResultEnum.Fail, unityVersion, $"Linework Lite is not compatible with Unity {unityVersion}. Please upgrade to Unity 6 which is the LTS version for the 2023 cycle.");
            }
            return new CheckResult(ResultEnum.Fail, unityVersion, $"Linework Lite is not compatible with Unity {unityVersion}.");
        }

        private CheckResult CheckURPVersion()
        {
            _urpSearchRequest = Client.Search("com.unity.render-pipelines.universal", Application.internetReachability == NetworkReachability.NotReachable);
            while (!_urpSearchRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }

            if (_urpSearchRequest.Status != StatusCode.Success)
            {
                return new CheckResult(ResultEnum.Fail, $"Error: {_urpSearchRequest.Error.message}", "An error occurred.");
            }

            var package = _urpSearchRequest.Result[0];
            var version = package.version;
            return new CheckResult(version.StartsWith("14") || version.StartsWith("17") ? ResultEnum.Pass : ResultEnum.Fail, version, "Linework Lite is compatible with URP 14 and URP 17.");
        }

        private CheckResult CheckTargetPlatform()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            return target switch
            {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => new CheckResult(ResultEnum.Pass, "Windows", "Linework Lite is compatible with Windows."),
                BuildTarget.StandaloneOSX => new CheckResult(ResultEnum.Pass, "macOS", "Linework Lite is compatible with macOS."),
                BuildTarget.iOS => new CheckResult(ResultEnum.Warning, "iOS",
                    "Linework Lite is compatible with iOS. However, some bugs may still be present. Please reach out if you encounter any issues."),
                BuildTarget.WebGL => new CheckResult(ResultEnum.Warning, "WebGL",
                    "Linework Lite is compatible with WebGL. However, some bugs may still be present. Please reach out if you encounter any issues."),
                _ => new CheckResult(ResultEnum.Fail, target.ToString(),
                    $"Compatibility with {target.ToString()} has not yet been tested. This does not mean that Linework Lite will not work. Please reach out if you encounter any issues.")
            };
        }

        private CheckResult CheckActivePipeline()
        {
            if (GraphicsSettings.currentRenderPipeline == null) return new CheckResult(ResultEnum.Fail, "Not found", NoActiveRendererFoundDescription);
            var type = GraphicsSettings.currentRenderPipeline.GetType().ToString();
            if (type.Contains("HDRenderPipelineAsset"))
            {
                return new CheckResult(ResultEnum.Fail, "High Definition", "Linework Lite is not compatible with the High Definition Render Pipeline.");
            }

            if (type.Contains("UniversalRenderPipelineAsset"))
            {
                return new CheckResult(ResultEnum.Pass, "Universal", "Linework Lite is compatible with the Universal Render Pipeline.");
            }
            if (type.Contains("LightweightRenderPipelineAsset"))
            {
                return new CheckResult(ResultEnum.Fail, "Light Weight", "Linework Lite is not compatible with the Light Weight Render Pipeline");
            }
            return new CheckResult(ResultEnum.Fail, "Custom", "Linework Lite is not compatible with any Custom Render Pipeline.");
        }

        private CheckResult CheckActiveRenderer()
        {
            if (GraphicsSettings.currentRenderPipeline == null) return new CheckResult(ResultEnum.Fail, "Not found", NoActiveRendererFoundDescription);
            var type = GraphicsSettings.currentRenderPipeline.GetType().ToString();
            if (!type.Contains("UniversalRenderPipelineAsset")) return new CheckResult(ResultEnum.Fail, "Universal pipeline not found", "Universal pipeline not found.");
            var pipeline = (UniversalRenderPipelineAsset) GraphicsSettings.currentRenderPipeline;
            var propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            var rendererData = ((ScriptableRendererData[]) propertyInfo?.GetValue(pipeline))?[0];
            return rendererData switch
            {
                Renderer2DData => new CheckResult(result: ResultEnum.Fail, message: "2D",
                    description: "Linework Lite is not compatible with the 2D Renderer. Support will be added in the future."),
                UniversalRendererData => new CheckResult(result: ResultEnum.Pass, message: "Universal", description: "Linework Lite compatible with the Universal Renderer."),
                _ => new CheckResult(ResultEnum.Fail, "Unknown", "An unknown renderer was used.")
            };
        }

        private CheckResult CheckRenderGraph()
        {
#if UNITY_6000_0_OR_NEWER
            var renderGraphSettings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
            var usingRenderGraph = !renderGraphSettings.enableRenderCompatibilityMode;
            return usingRenderGraph
                ? new CheckResult(ResultEnum.Pass, "Enabled", "Render Graph is enabled.")
                : new CheckResult(ResultEnum.Info, "Compatibility Mode",
                    "Render Graph is available but not in use because Compatibility Mode is enabled. Unity no longer develops or improves the rendering path that does not use Render Graph API. See Edit > Project Settings > Graphics > Render Graph to disable Compatibility Mode.");
#else
            return new CheckResult(ResultEnum.Info, "Not Available", "Unity no longer develops or improves the rendering path that does not use Render Graph API. Upgrade to Unity 6 to make use of the Render Graph API.");
#endif
        }

        private CheckResult CheckRenderingPath()
        {
            if (GraphicsSettings.currentRenderPipeline == null) return new CheckResult(ResultEnum.Fail, "Not found", NoActiveRendererFoundDescription);
            var type = GraphicsSettings.currentRenderPipeline.GetType().ToString();
            if (!type.Contains("UniversalRenderPipelineAsset")) return new CheckResult(ResultEnum.Fail, "URP renderer not found", "URP renderer not found.");
            var pipeline = (UniversalRenderPipelineAsset) GraphicsSettings.currentRenderPipeline;
            var propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            var rendererData = ((ScriptableRendererData[]) propertyInfo?.GetValue(pipeline))?[0];
            switch (rendererData)
            {
                case Renderer2DData:
                    return new CheckResult(result: ResultEnum.Fail, message: "Unknown",
                        description: "Linework Lite is not compatible with the 2D Renderer. Support will be added in the future.");
                case UniversalRendererData data:
                {
                    var renderingMode = data.renderingMode;
                    return renderingMode switch
                    {
                        RenderingMode.Forward => new CheckResult(ResultEnum.Pass, "Forward", "Linework Lite is compatible with the Forward rendering path."),
                        RenderingMode.ForwardPlus => new CheckResult(ResultEnum.Pass, "Forward+", "Linework Lite is compatible with the Forward+ rendering path."),
                        RenderingMode.Deferred => new CheckResult(ResultEnum.Fail, "Deferred",
                            "Linework Lite is not compatible with the Deferred rendering path. You can change the active rendering path on your active Render Pipeline asset."),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }
                default:
                    return new CheckResult(ResultEnum.Fail, "Unknown", "An unknown renderer was used.");
            }
        }

        private CheckResult CheckHybridRenderer()
        {
#if ENABLE_HYBRID_RENDERER_V2
            return new CheckResult(ResultEnum.Warning, "Enabled", "The DOTS Hybrid Renderer is enabled. Support for the DOTS Hybrid Renderer is experimental. Please reach out if you encounter any issues.");
#else
            return new CheckResult(ResultEnum.Pass, "Disabled", "The DOTS Hybrid Renderer is disabled.");
#endif
        }

        private CheckResult CheckGraphicsAPI()
        {
            return SystemInfo.graphicsDeviceType switch
            {
                GraphicsDeviceType.Metal => new CheckResult(ResultEnum.Pass, "Metal", "Linework Lite is compatible with Metal."),
                GraphicsDeviceType.Vulkan => new CheckResult(ResultEnum.Pass, "Vulkan", "Linework Lite is compatible with Vulkan."),
                GraphicsDeviceType.Direct3D11 => new CheckResult(ResultEnum.Pass, "Direct3D11", "Linework Lite is compatible with Direct3D11."),
                GraphicsDeviceType.OpenGLCore => new CheckResult(ResultEnum.Fail, "OpenGLCore", "Linework Lite is not compatible with OpenGLCore."),
                _ => new CheckResult(ResultEnum.Warning, SystemInfo.graphicsDeviceType.ToString(),
                    $"Compatibility with {SystemInfo.graphicsDeviceType.ToString()} has not been tested.")
            };
        }

        private CheckResult CheckActiveRenderPipelineAsset()
        {
            var activeRenderPipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (activeRenderPipelineAsset == null) return new CheckResult(ResultEnum.Fail, "Not found", NoActiveRendererFoundDescription);
            return new CheckResult(ResultEnum.Info, activeRenderPipelineAsset.name, $"You currently active pipeline asset is {activeRenderPipelineAsset.name}. Make sure that you are adding outlines to the renderer of that pipeline asset.");
        }

        private CheckResult CheckSTP()
        {
#if UNITY_6000_0_OR_NEWER
           if (GraphicsSettings.currentRenderPipeline == null) return new CheckResult(ResultEnum.Fail, "Not found", NoActiveRendererFoundDescription);
            var type = GraphicsSettings.currentRenderPipeline.GetType().ToString();
            if (!type.Contains("UniversalRenderPipelineAsset")) return new CheckResult(ResultEnum.Fail, "Universal pipeline not found", "Universal pipeline not found.");
            var pipeline = (UniversalRenderPipelineAsset) GraphicsSettings.currentRenderPipeline;
            if (pipeline.upscalingFilter == UpscalingFilterSelection.STP) return new CheckResult(ResultEnum.Warning, "Enabled", "Spatial-Temporal Post-processing is enabled and might interfere with outline rendering. If you encounter issues, try setting the outline stage to 'Before Post Processing'.");
            return new CheckResult(ResultEnum.Pass, "Disabled", "Spatial-Temporal Post-processing is disabled.");
#else
            return new CheckResult(ResultEnum.Info, "Not Available", "Spatial-Temporal Post-processing is a Unity 6 feature.");
#endif
        }
        
        private static CheckResult CheckLiteVersion()
        {
            return new CheckResult(ResultEnum.Info, "Lite version", "Your are using the free version of Linework. Upgrade to access all outline types.");
        }

        private class Check
        {
            public string Name { get; }
            public CheckStatus Status { get; set; }
            public CheckResult Result { get; set; }
            private Func<CheckResult> CheckCallback { get; }

            public Check(string name, Func<CheckResult> checkCallback)
            {
                Name = name;
                Status = CheckStatus.Untested;
                Result = null;
                CheckCallback = checkCallback;
            }

            public CheckResult RunCheck()
            {
                return CheckCallback.Invoke();
            }
        }

        private static GUIContent LoadIconForResult(ResultEnum result)
        {
            var iconName = result switch
            {
                ResultEnum.Untested => "TestIgnored",
                ResultEnum.Pass => EditorGUIUtility.isProSkin ? "d_GreenCheckmark" : "GreenCheckmark",
                ResultEnum.Fail => EditorGUIUtility.isProSkin ? "d_console.erroricon.sml" : "console.erroricon.sml",
                ResultEnum.Warning => EditorGUIUtility.isProSkin ? "d_console.warnicon.sml" : "console.warnicon.sml",
                ResultEnum.Info => EditorGUIUtility.isProSkin ? "d_console.infoicon.sml" : "console.infoicon.sml",
                _ => null
            };

            return !string.IsNullOrEmpty(iconName) ? EditorGUIUtility.IconContent(iconName) : null;
        }
    }
}
