// ArchNet · API Service — API recipe (ArchNet Hub > Services > API) and its tool
// Creates, under Assets/ArchNet/Api/: Scripts/GameApi.cs (your endpoints), Data/ApiSettings.asset
// (base URL per environment, timeout, retries) and Prefabs/Api.prefab under the ArchNet Bootstrap.
// The tool edits the settings and sends a test GET to see what the server answers.
// This assembly only compiles when the ArchNet Hub (com.archnet.hub 2.0.0+) is installed.
// Docs: https://archnet-plateform.fr/#ServiceApi

using System;
using ArchNet.Core.Editor;
using ArchNet.Installer.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace ArchNet.Service.Editor
{
    public sealed class ApiRecipe : ServiceRecipe
    {
        public const string RecipeId = "api";
        const string TemplatePath = "Packages/com.archnet.service.api/Editor/Recipe/Templates/GameApi.cs.txt";
        const string ServiceScript = "Scripts/GameApi.cs";
        const string SettingsFile = "Data/ApiSettings.asset";
        const string PrefabFile = "Prefabs/Api.prefab";

        public override string Id => RecipeId;
        public override string DisplayName => "API";
        public override string Description => "Talk to your web API in one line: await Services.Get<GameApi>().Status(); one base URL per environment, timeout, retries, clear errors.";
        public override string Category => "Online";
        public override string FolderName => "Api";
        public override string[] RequiredServices => new[] { BootstrapRecipe.RecipeId };
        public override bool HasTool => true;

        public static string SettingsPath => $"{RecipeRunner.RootFolder}/Api/{SettingsFile}";

        public override void Create(RecipeContext context)
        {
            context.WriteTemplate(ServiceScript, RecipeContext.LoadTemplate(TemplatePath));
            context.CreateAsset(SettingsFile, ScriptableObject.CreateInstance<ApiSettings>());
        }

        public override void AfterScriptsCompiled(RecipeContext context)
        {
            Type serviceType = RecipeTools.GeneratedType<ApiService>(context, "GameApi", ServiceScript);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(context.PathOf(PrefabFile));
            if (prefab == null)
            {
                var root = new GameObject("Api", serviceType);
                RecipeTools.Assign(root.GetComponent(serviceType), "_settings", AssetDatabase.LoadAssetAtPath<ApiSettings>(context.PathOf(SettingsFile)));
                prefab = context.SavePrefab(PrefabFile, root);
            }
            BootstrapRecipe.AddUnderBootstrap(prefab);
        }

        public override void OpenTool() => ApiWindow.Open();
    }

    public class ApiWindow : EditorWindow
    {
        const string StylePath = "Packages/com.archnet.hub/Editor/UI/Hub.uss";
        const int MaxShownCharacters = 3000;

        public static void Open()
        {
            var window = GetWindow<ApiWindow>();
            window.titleContent = new GUIContent("API");
            window.minSize = new Vector2(460, 420);
        }

        void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.AddToClassList("archnet-hub");
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (style != null) root.styleSheets.Add(style);
            root.style.paddingLeft = root.style.paddingRight = root.style.paddingTop = 8;

            var settings = AssetDatabase.LoadAssetAtPath<ApiSettings>(ApiRecipe.SettingsPath);
            if (settings == null)
            {
                root.Add(new HelpBox($"{ApiRecipe.SettingsPath} was not found. Add the API service from ArchNet > Hub > Services.", HelpBoxMessageType.Warning));
                return;
            }
            root.Add(new HelpBox("Never put a password or a secret key here: this asset ships in the game. Give tokens to GameApi.AuthToken at runtime.", HelpBoxMessageType.Info));
            root.Add(new InspectorElement(settings));

            var row = new VisualElement();
            row.AddToClassList("row");
            row.style.marginTop = 10;
            var url = new TextField("Test GET") { value = settings.Active.baseUrl.TrimEnd('/') + "/status" };
            url.AddToClassList("grow");
            row.Add(url);
            var result = new Label();
            result.AddToClassList("muted");
            result.style.whiteSpace = WhiteSpace.Normal;
            var send = new Button(() => SendTest(url.value, result)) { text = "Send" };
            send.AddToClassList("primary");
            row.Add(send);
            root.Add(row);
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.Add(result);
            root.Add(scroll);
        }

        static void SendTest(string url, Label result)
        {
            result.text = $"GET {url}…";
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = 10;
            request.SendWebRequest().completed += _ =>
            {
                string body = request.downloadHandler?.text ?? "";
                if (body.Length > MaxShownCharacters) body = body.Substring(0, MaxShownCharacters) + "…";
                result.text = request.result == UnityWebRequest.Result.Success
                    ? $"HTTP {request.responseCode}\n{body}"
                    : $"{request.error} (HTTP {request.responseCode})\n{body}";
                request.Dispose();
            };
        }
    }
}
