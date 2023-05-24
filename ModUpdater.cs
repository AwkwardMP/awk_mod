using System;
using System.IO;
using System.Diagnostics;
using System.Collections;

using UnityEngine;

using Newtonsoft.Json.Linq;
using Version = SemanticVersioning.Version;

using BepInEx;
using AWK;
using TMPro;
using UnityEngine.Networking;

namespace AwkwardMP
{
    public class ModUpdater : MonoBehaviour
    {
        public static bool showUpdatePopUp = true;
        public static bool updateInProgress = false;

        public static ModUpdater Instance { get; private set; }
        public ModUpdater() : base() { }


        public class UpdateData
        {
            public String Content;
            public string Tag;
            public string TimeString;
            public JObject Request;
            public Version Version => Version.Parse(Tag);

            public UpdateData(JObject data)
            {
                Tag = data["tag_name"]?.ToString().TrimStart('v');
                Content = data["body"]?.ToString();
                TimeString = DateTime.FromBinary(((DateTime)data["published_at"]).ToBinary()).ToString();
                Request = data;
            }

            public bool IsNewer(Version version)
            {
                if (!Version.TryParse(Tag, out var myVersion)) return false;
                return myVersion.BaseVersion() > version.BaseVersion();
            }
        }

        public UpdateData AwkwardUpdate;
        public UpdateData RequiredUpdateData => AwkwardUpdate;

        public void Awake()
        {
            if (Instance) Destroy(this);
            Instance = this;


            AwkwardMP.Log.LogInfo("Checking for Updates...");
            this.StartCoroutine(CoCheckForUpdates());

            foreach (var file in Directory.GetFiles(Paths.PluginPath, "*.old"))
            {
                File.Delete(file);
            }
        }

        private static Stopwatch _waitTimer = new Stopwatch();
        public IEnumerator CoCheckForUpdates()
        {
            _waitTimer.Start();
            while (_waitTimer.ElapsedMilliseconds < 3000) yield return null;
            _waitTimer.Stop(); _waitTimer.Reset();

            using (UnityWebRequest unityWebRequest = new UnityWebRequest())
            {
                unityWebRequest.url = $"https://api.github.com/repos/AwkwardMP/awk_mod/releases/latest";
                unityWebRequest.SetRequestHeader("Content-Type", "application/json");
                unityWebRequest.SetRequestHeader("User-Agent", "AwkwardMP Updater");
                unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
                unityWebRequest.method = "GET";

                yield return unityWebRequest.Send();


                if (unityWebRequest.isError)
                {
                    AwkwardMP.Log.LogError("isHttpError: " + unityWebRequest.isError);
                    AwkwardMP.Log.LogError(unityWebRequest.error);
                }

                JObject data = JObject.Parse(unityWebRequest.downloadHandler.text);
                UpdateData _update = new UpdateData(data);

                if (_update.IsNewer(Version.Parse(AwkwardMP.VersionString)))
                {
                    Instance.AwkwardUpdate = _update;
                    bool bContinue = false;

                    Helper.ShowPopupYesNo($"AwkwardMP\nUpdate available!\nv{_update.Tag}\n\nPress OK to download!", "OK", "Cancel", new InputManager.InputDelegate(delegate
                    {
                        Helper.HidePopup();
                        bContinue = true;
                        this.StartCoroutine(CoUpdate());
                    }), new InputManager.InputDelegate(delegate
                    {
                        Helper.HidePopup();
                        bContinue = true;
                    }), out TextMeshProUGUI _updateAvailableText, out AWK.MenuButton _updateBtnConfirm, out AWK.MenuButton _updateBtnCancel);

                    while (!bContinue) yield return null;
                }
            }
        }

        public IEnumerator CoUpdate()
        {
            updateInProgress = true;

            Helper.ShowPopupOk("Updating AwkwardMP\n...", "OK", new InputManager.InputDelegate(delegate
            {
                Helper.HidePopup();
            }), out TextMeshProUGUI _updateText, out MenuButton _updateBtnDone);
            _updateBtnDone.EnableInput(false);


            JToken assets = AwkwardUpdate.Request["assets"];
            string downloadURI = "";
            for (JToken current = assets.First; current != null; current = current.Next)
            {
                string browser_download_url = current["browser_download_url"]?.ToString();
                if (browser_download_url != null && current["content_type"] != null)
                {
                    if (current["content_type"].ToString().Equals("application/x-msdownload") &&
                        browser_download_url.EndsWith(".dll"))
                    {
                        downloadURI = browser_download_url;
                        break;
                    }
                }
            }
            if (downloadURI.Length == 0) yield return false;


            using (UnityWebRequest unityWebRequest = new UnityWebRequest())
            {
                unityWebRequest.method = "GET";
                unityWebRequest.url = downloadURI;
                unityWebRequest.SetRequestHeader("User-Agent", "AwkwardMP Updater");
                unityWebRequest.downloadHandler = new DownloadHandlerBuffer();

                yield return unityWebRequest.Send();

                try
                {
                    string filePath = Path.Combine(Paths.PluginPath, "AwkwardMP.dll");
                    if (File.Exists(filePath + ".old")) File.Delete(filePath + ".old");
                    if (File.Exists(filePath)) File.Move(filePath, filePath + ".old");

                    byte[] results = unityWebRequest.downloadHandler.data;

                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(results, 0, results.Length);
                    }

                    _updateText.text = "Update complete!\n\nPlease restart the Game.";
                    
                } catch
                {
                    _updateText.text = "Update failed!\n\nPlease download and install manually.";
                }
                
            }

            _updateBtnDone.EnableInput(true);
            yield return true;
        }
    }
}