using FrooxEngine;
using HarmonyLib;
using System;
using System.IO;
using MimeDetective;
using System.Drawing;
using ResoniteModLoader;
using Elements.Assets;
using System.Threading.Tasks;

namespace SaveExif
{
    public class SaveExif : ResoniteMod
    {
        public override string Name => "SaveExif";
        public override string Author => "kka429";
        public override string Version => "2.1.0";
        public override string Link => "https://github.com/rassi0429/SaveExif";

        private static bool _keepOriginalScreenshotFormat = false;

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("dev.kokoa.saveexif");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(WindowsPlatformConnector))]
        class WindowsPlatformConnector_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(WindowsPlatformConnector.NotifyOfScreenshot))]
            static bool NotifyOfScreenshot_Prefix()
            {
                // NotifyOfScreenshot_Postfix で代替しているのでこっちは無効化
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(WindowsPlatformConnector.Initialize))]
            static void Initialize_Postfix(WindowsPlatformConnector __instance)
            {
                var updateAction = () =>
                {
                    Task.Run(async () =>
                    {
                        var result = await __instance.Engine.LocalDB.TryReadVariableAsync<bool>(WindowsPlatformConnector.SCREENSHOT_FORMAT_SETTING);
                        if (result.hasValue)
                        {
                            _keepOriginalScreenshotFormat = result.value;
                        }
                    });
                };
                updateAction();
                __instance.Engine.LocalDB.RegisterVariableListener(WindowsPlatformConnector.SCREENSHOT_FORMAT_SETTING, updateAction);
            }
        }

        [HarmonyPatch(typeof(PhotoMetadata))]
        class PhotoMetadata_Patch
        {
            // srcPath != dstPath 上書きはできない
            static void WriteExif(PhotoMetadata photoMetadata, string srcPath, string dstPath)
            {
                using (var img = Image.FromFile(srcPath))
                {
                    var metadata = new SavedMetadata(photoMetadata);
                    var ew = new ExifWriter(img);
                    ew.SetModel("ResoniteCamera");
                    ew.SetMake("FrooxEngine");
                    ew.SetDateTimeOriginal(photoMetadata.TimeTaken.Value.ToLocalTime().ToString("yyyy:MM:dd HH:mm:ss"));
                    ew.SetDescription("Resonite Photo");
                    ew.SetArtist(metadata.TakeUserName); // Unicodeなユーザ名もいるので本当はダメそう
                    ew.SetSoftware("Resonite");

                    ew.SetUserComment(metadata.ToJson());

                    img.Save(dstPath);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(PhotoMetadata.NotifyOfScreenshot))]
            static void NotifyOfScreenshot_Postfix(PhotoMetadata __instance)
            {
                // PhotoMetadata を WindowsPlatformConnector.NotifyOfScreenshot に確実に渡すのが面倒なのでここで代替する
                __instance.StartGlobalTask(async () =>
                {
                    var tex = __instance.Slot.GetComponent<StaticTexture2D>();
                    var url = tex?.URL.Value;
                    if (url is null) return;

                    await new ToBackground();
                    // キャッシュが効いてるはずなので重複して実行しても大してコストはかからない認識
                    var tmpPath = await __instance.Engine.AssetManager.GatherAssetFile(url, 100f);
                    if (tmpPath is null) return;

                    string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    pictures = Path.Combine(pictures, __instance.Engine.Cloud.Platform.Name);
                    Directory.CreateDirectory(pictures);

                    string filename = __instance.TimeTaken.Value.ToLocalTime().ToString("yyyy-MM-dd HH.mm.ss"); //FIX LOCALTIME
                    string extension = _keepOriginalScreenshotFormat ? Path.GetExtension(tmpPath) : ".jpg";
                    if (string.IsNullOrWhiteSpace(extension))
                    {
                        FileType fileType = new FileInfo(tmpPath).GetFileType();
                        if (fileType != null)
                            extension = "." + fileType.Extension;
                    }
                    await WindowsPlatformConnector.ScreenshotSemaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        int num = 1;
                        string str1;
                        do
                        {
                            string str2 = filename;
                            if (num > 1)
                                str2 += string.Format(" ({0})", num);
                            str1 = Path.Combine(pictures, str2 + extension);
                            num++;
                        }
                        while (File.Exists(str1));

                        if (_keepOriginalScreenshotFormat)
                        {
                            File.Copy(tmpPath, str1);
                            File.SetAttributes(str1, FileAttributes.Normal);
                        }
                        else
                        {
                            var convertedPath = tmpPath + ".tmp";
                            TextureEncoder.ConvertToJPG(tmpPath, convertedPath);
                            WriteExif(__instance, convertedPath, str1);
                        }
                    }
                    catch (Exception ex)
                    {
                        Error("Exception saving screenshot to Windows:\n" + ex);
                    }
                    finally
                    {
                        WindowsPlatformConnector.ScreenshotSemaphore.Release();
                    }
                });
            }
        }
    }
}