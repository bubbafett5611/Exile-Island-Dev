using Oxide.Core;
using UnityEngine;
using System.Collections;
using System.Reflection;
using UnityEngine.Networking;

namespace Oxide.Plugins
{
    [Info("ChaosExtensionDownloader", "k1lly0u", "0.1.0")]
    class ChaosExtensionDownloader : RustPlugin
    {
        private const string DLL_URI = "https://chaoscode.io/oxide/Oxide.Ext.Chaos.dll";

        private void OnServerInitialized()
        {
            if (Interface.Oxide.GetExtension("Chaos") == null)
                ServerMgr.Instance.StartCoroutine(DownloadAndSave(true));
        }
        
        private IEnumerator DownloadAndSave(bool loadInstantly)
        {
            Debug.Log($"[Chaos] - Downloading latest version of Chaos...");

            UnityWebRequest www = UnityWebRequest.Get(DLL_URI);

            string downloadPath = UnityEngine.Application.platform == RuntimePlatform.WindowsPlayer ? $@"{Interface.Oxide.ExtensionDirectory}\Oxide.Ext.Chaos.dll" :
                $@"{Interface.Oxide.ExtensionDirectory}/Oxide.Ext.Chaos.dll";

            www.downloadHandler = new DownloadHandlerFile(downloadPath);

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError($"[Chaos] - Failed to connect to chaoscode.io : {www.error}");
                www.Dispose();
                yield break;
            }

            yield return new WaitUntil(()=> (www.downloadHandler as DownloadHandlerFile).isDone);

            if (loadInstantly)
            {
                Debug.Log($"[Chaos] - Download completed! Loading extension...");
                Interface.Oxide.LoadExtension("Oxide.Ext.Chaos");
                
                Oxide.Core.Extensions.Extension extension = Interface.Oxide.GetExtension("Chaos");
                MethodInfo methodInfo = extension.GetType().GetMethod("LoadCorePlugins", BindingFlags.Public | BindingFlags.Instance);
                methodInfo.Invoke(extension, null);
                
                timer.In(1, ()=> Interface.Oxide.LoadAllPlugins());
            }
            else Debug.Log($"[Chaos] - Download completed! Restart your server for file to load");
        }
    }
}
